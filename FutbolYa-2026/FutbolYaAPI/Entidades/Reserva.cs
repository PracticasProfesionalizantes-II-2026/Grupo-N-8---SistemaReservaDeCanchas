namespace FutbolYaAPI.Entidades;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

// Pendiente = todavía no llegó la fecha/hora (bloquea horario y stock de material).
// Confirmada = ya pasó la fecha/hora (se asume que se usó; el material vuelve al stock).
// Cancelada = se canceló antes de que ocurriera.
public enum EstadoReserva
{
    Pendiente,
    Confirmada,
    Cancelada
}

public class Reserva
{
    [Key]
    public int Cod_Reserva { get; set; }

    public DateTime Fecha { get; set; } // Fecha de creación de la reserva, la asigna el sistema automáticamente al crearla.

    public DateTime FechaReserva { get; set; } // fecha para la que se reserva, la manda el cliente

    public string Dni_Cliente { get; set; } = string.Empty;

    public string Telefono_Cliente { get; set; } = string.Empty;

    public int Cod_Cancha { get; set; }

    public int Cod_Usuario { get; set; }

    public EstadoReserva Estado { get; set; } = EstadoReserva.Pendiente;

    // Navegación
    [ForeignKey("Cod_Cancha")]
    public Cancha Cancha { get; set; } = null!;

    [ForeignKey("Cod_Usuario")]
    public Usuario Usuario { get; set; } = null!;

    public ICollection<Reserva_Horario> ReservaHorarios { get; set; } = new List<Reserva_Horario>();

    public ICollection<Reserva_Material> ReservaMateriales { get; set; } = new List<Reserva_Material>();
}
