namespace FutbolYaAPI.Entidades;
using System.ComponentModel.DataAnnotations;

public class Usuario
{
    [Key]
    public int Cod_Usuario { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Dni { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string Contraseña { get; set; } = string.Empty;
    public bool Rol { get; set; } // Valores posibles: FALSE:"Operador" o TRUE:"Administrador"
    public bool Cambiar_Contraseña { get; set; }

    // Recuperación de contraseña por correo
    public string? Token_Recuperacion { get; set; }
    public DateTime? Token_Expira { get; set; }

    // Baja lógica (soft delete)
    public bool Activo { get; set; } = true;

    // Navegación
    public ICollection<Venta> Ventas { get; set; } = new List<Venta>();
    public ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
    public ICollection<Auditoria> RegistrosAuditoria { get; set; } = new List<Auditoria>();
}
