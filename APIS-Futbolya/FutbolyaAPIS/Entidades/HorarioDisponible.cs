namespace FutbolyaAPIS.Entidades;
using System.ComponentModel.DataAnnotations;

public class HorarioDisponible
{
    [Key]
    public int Cod_Horario { get; set; }

    public TimeSpan HoraInicio { get; set; }

    public TimeSpan HoraFin { get; set; }

    public bool Activo { get; set; }

    // Navegación
    public ICollection<Reserva> Reservas { get; set; }
}