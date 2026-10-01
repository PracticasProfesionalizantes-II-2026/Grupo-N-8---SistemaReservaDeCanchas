namespace FutbolYaAPI.Entidades;
using System.ComponentModel.DataAnnotations;

public enum EstadoCancha
{
    Disponible,
    Mantenimiento,
    Baja
}

public class Cancha
{
    [Key]
    public int Cod_Cancha { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string Descripcion { get; set; } = string.Empty;

    public EstadoCancha Estado { get; set; } = EstadoCancha.Disponible;

    // Navegación
    public ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
    public ICollection<Cancha_Horario> CanchaHorarios { get; set; } = new List<Cancha_Horario>();
}
