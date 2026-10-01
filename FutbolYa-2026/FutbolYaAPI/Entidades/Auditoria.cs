namespace FutbolYaAPI.Entidades;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public enum AccionAuditoria
{
    Alta,
    Modificacion,
    Baja
}

// Registro de auditoría: se escribe una entrada por cada alta, modificación o baja
// sobre una entidad auditada, con el usuario responsable y el estado antes/después.
public class Auditoria
{
    [Key]
    public int Cod_Auditoria { get; set; }

    // FK hacia Usuario (quién hizo el cambio)
    public int Cod_Usuario { get; set; }

    public DateTime Fecha_Hora { get; set; } = DateTime.Now;

    // Nombre del módulo/entidad afectada (ej: "Reserva", "Producto", "Usuario")
    public string Entidad_Afectada { get; set; } = string.Empty;

    public int Cod_Entidad_Afectada { get; set; }

    public AccionAuditoria Accion { get; set; }

    // Snapshot del registro antes/después, serializado como JSON. Nulo en Alta (Valor_Anterior) o Baja (Valor_Nuevo).
    public string? Valor_Anterior { get; set; }
    public string? Valor_Nuevo { get; set; }

    // Navegación
    [ForeignKey("Cod_Usuario")]
    public Usuario Usuario { get; set; } = null!;
}
