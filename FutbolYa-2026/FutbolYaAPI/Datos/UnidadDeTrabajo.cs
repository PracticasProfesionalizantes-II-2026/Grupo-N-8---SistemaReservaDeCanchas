using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FutbolYaAPI.Datos;

// Las Repositorios comparten el AppDbContext con scope de request y cada método llama
// SaveChangesAsync() por su cuenta, así que una operación con varios pasos (ej.: crear una
// Reserva + descontar stock de materiales) podía quedar a medias si algo fallaba en el medio.
// Esta abstracción le da a la Logica una forma de envolver varios pasos en una sola transacción
// SQL Server sin que la Logica dependa de EF Core directamente (BeginTransactionAsync, etc.).
// El nivel de aislamiento es parametrizable: por defecto ReadCommitted, pero una Logica puede
// pedir Serializable cuando necesita evitar lecturas fantasma entre los pasos que envuelve.
public interface IUnidadDeTrabajo
{
    Task<T> EjecutarEnTransaccion<T>(Func<Task<T>> operacion, IsolationLevel nivelAislamiento = IsolationLevel.ReadCommitted);
}

// Implementación de IUnidadDeTrabajo sobre el AppDbContext scoped: abre la transacción SQL
// Server, ejecuta el delegate de la Logica y hace commit/rollback, con reintento automático
// ante deadlocks y conflictos de serialización.

public class UnidadDeTrabajo : IUnidadDeTrabajo
{
    // Reintentos ante un conflicto de concurrencia (ver EsErrorDeSerializacion). No hace falta
    // una IExecutionStrategy de EF: Program.cs no configura EnableRetryOnFailure, así que no hay
    // una execution strategy registrada para SQL Server con la que este reintento manual choque.
    private const int IntentosMaximos = 5;

    private readonly AppDbContext _db;
    private readonly ILogger<UnidadDeTrabajo> _logger;

    public UnidadDeTrabajo(AppDbContext db, ILogger<UnidadDeTrabajo> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<T> EjecutarEnTransaccion<T>(Func<Task<T>> operacion, IsolationLevel nivelAislamiento = IsolationLevel.ReadCommitted)
    {
        for (int intento = 1; intento <= IntentosMaximos; intento++)
        {
            await using var transaccion = await _db.Database.BeginTransactionAsync(nivelAislamiento);

            try
            {
                var resultado = await operacion();
                await transaccion.CommitAsync();
                return resultado;
            }
            catch (Exception ex) when (EsErrorDeSerializacion(ex) && intento < IntentosMaximos)
            {
                await RollbackSeguro(transaccion);
                // Las entidades del intento fallido quedan trackeadas en el DbContext; si el
                // reintento las guardara de nuevo tal cual, terminaría en un error distinto (500).
                // Se descartan antes de reintentar.
                _db.ChangeTracker.Clear();
                _logger.LogWarning(ex, "Conflicto de concurrencia dentro de una transacción (intento {Intento} de {Max}); reintentando", intento, IntentosMaximos);
                // Espera aleatoria creciente para que las transacciones en conflicto no vuelvan a chocar al mismo tiempo.
                await Task.Delay(Random.Shared.Next(20, 80) * intento);
            }
            catch (Exception ex) when (EsErrorDeSerializacion(ex))
            {
                await RollbackSeguro(transaccion);
                _db.ChangeTracker.Clear();
                throw new ConflictoConcurrenciaException(ex);
            }
            catch
            {
                await RollbackSeguro(transaccion);
                throw;
            }
        }

        // No debería llegarse acá: o se devuelve/comitea, o se relanza antes de agotar los intentos.
        throw new InvalidOperationException("No se pudo completar la operación tras varios reintentos por conflictos de concurrencia");
    }

    // Cuando SQL Server elige esta transacción como víctima de un deadlock ya la revierte del
    // lado del servidor, y RollbackAsync tira "This SqlTransaction has completed", tapando el
    // error original (que sí era reintentable).
    private async Task RollbackSeguro(Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaccion)
    {
        try
        {
            await transaccion.RollbackAsync();
        }
        catch (Exception ex) when (ex is InvalidOperationException or SqlException)
        {
            _logger.LogDebug(ex, "La transacción ya estaba finalizada al intentar el rollback");
        }
    }

    // 1205 = la transacción fue elegida como víctima de un deadlock.
    // 3960 = conflicto de actualización detectado bajo isolation snapshot/serializable.
    // EF Core (sin EnableRetryOnFailure) envuelve el deadlock así:
    // InvalidOperationException ("likely due to a transient failure") → DbUpdateException → SqlException 1205.
    // Por eso se recorre toda la cadena de InnerException y no solo el primer nivel.
    private static bool EsErrorDeSerializacion(Exception ex)
    {
        for (var actual = ex; actual != null; actual = actual.InnerException)
        {
            if (actual is SqlException sql && (sql.Number == 1205 || sql.Number == 3960))
                return true;
        }
        return false;
    }
}

// Se agotaron los reintentos por conflictos de concurrencia: el endpoint responde 409 con un
// mensaje amable en vez de un 500 genérico (ver ManejoErroresExtensions.ProblemaInterno).
public class ConflictoConcurrenciaException : Exception
{
    public ConflictoConcurrenciaException(Exception inner)
        : base("Otro usuario está operando sobre los mismos datos. Intentá nuevamente.", inner) { }
}
