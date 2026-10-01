namespace FutbolYaAPI.Entidades;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

// Tabla intermedia N:M: bloques horarios (HorarioDisponible) que integran una reserva.
public class Reserva_Horario
{
    [Key]
    public int Cod_Reserva_Horario { get; set; }

    public int Cod_Reserva { get; set; }

    public int Cod_Horario { get; set; }

    // Navegación
    [ForeignKey("Cod_Reserva")]
    public Reserva Reserva { get; set; } = null!;

    [ForeignKey("Cod_Horario")]
    public HorarioDisponible HorarioDisponible { get; set; } = null!;
}
