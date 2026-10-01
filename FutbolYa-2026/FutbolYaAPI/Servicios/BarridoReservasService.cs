using FutbolYaAPI.Logica;

namespace FutbolYaAPI.Servicios;

// BackgroundService que confirma las reservas vencidas (ReservaLogica.ConfirmarVencidas) y
// libera su material periódicamente, en vez de hacerlo dentro de un GET /api/reservas: así una
// lectura no dispara una escritura oculta ni depende de que alguien pida ese endpoint. Corre
// cada N minutos (config "Reservas:MinutosBarrido", default 5) y una vez al arrancar la API.
public class BarridoReservasService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BarridoReservasService> _logger;
    private readonly TimeSpan _intervalo;

    public BarridoReservasService(IServiceScopeFactory scopeFactory, ILogger<BarridoReservasService> logger, IConfiguration configuracion)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;

        var minutos = configuracion.GetValue<int?>("Reservas:MinutosBarrido") ?? 5;
        _intervalo = TimeSpan.FromMinutes(minutos > 0 ? minutos : 5);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Corre una vez al arrancar para confirmar reservas vencidas sin esperar el primer tick
        // del temporizador periódico.
        await EjecutarBarrido(stoppingToken);

        using var temporizador = new PeriodicTimer(_intervalo);
        while (!stoppingToken.IsCancellationRequested && await temporizador.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            await EjecutarBarrido(stoppingToken);
        }
    }

    private async Task EjecutarBarrido(CancellationToken stoppingToken)
    {
        // Cada tick abre su propio scope (y por lo tanto su propio AppDbContext): este servicio
        // vive durante toda la vida de la app, así que no puede compartir el DbContext scoped
        // que usan los demás servicios (Logica/Repositorios) por request.
        using var scope = _scopeFactory.CreateScope();
        var logica = scope.ServiceProvider.GetRequiredService<IReservaLogica>();

        try
        {
            await logica.ConfirmarVencidas();
        }
        catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Error al ejecutar el barrido de reservas vencidas");
        }
    }
}
