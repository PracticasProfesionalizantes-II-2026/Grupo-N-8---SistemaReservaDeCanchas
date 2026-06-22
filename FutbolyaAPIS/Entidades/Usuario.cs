namespace FutbolyaAPIS.Entidades;
using System.ComponentModel.DataAnnotations;

public class Usuario
{
    [Key]  
    public int Cod_Usuario { get; set; }
    public string Nombre { get; set; }
    public string Apellido { get; set; }
    public string Dni { get; set; }
    public string Direccion { get; set; }
    public string Correo { get; set; }
    public string Contraseña { get; set; }
    public bool Rol { get; set; } // Valores posibles: FALSE:"Operador" o TRUE:"Administrador"
    public bool Cambiar_Contraseña { get; set; }

    
    // Navegación
    public ICollection<Venta> Ventas { get; set; } = new List<Venta>();
    public ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
}