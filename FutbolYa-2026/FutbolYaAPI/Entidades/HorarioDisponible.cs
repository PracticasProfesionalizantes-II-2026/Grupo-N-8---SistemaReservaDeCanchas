namespace FutbolYaAPI.Entidades;
using System.ComponentModel.DataAnnotations;

// Catálogo global fijo de bloques horarios de 1 hora (00-01, 01-02, ..., 23-24),
// compartido por todas las canchas. Cada Cancha elige qué bloques ofrece a través de Cancha_Horario.
public class HorarioDisponible
{
    [Key]
    public int Cod_Horario { get; set; }

    public TimeSpan HoraInicio { get; set; }

    public TimeSpan HoraFin { get; set; }

    public bool Activo { get; set; } = true;

    // Navegación
    public ICollection<Cancha_Horario> CanchaHorarios { get; set; } = new List<Cancha_Horario>();
    public ICollection<Reserva_Horario> ReservaHorarios { get; set; } = new List<Reserva_Horario>();
}
