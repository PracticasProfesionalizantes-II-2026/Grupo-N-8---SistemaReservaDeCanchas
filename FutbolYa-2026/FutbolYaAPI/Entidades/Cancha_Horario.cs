namespace FutbolYaAPI.Entidades;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

// Tabla intermedia N:M: qué bloques horarios del catálogo global ofrece cada cancha.
public class Cancha_Horario
{
    [Key]
    public int Cod_Cancha_Horario { get; set; }

    public int Cod_Cancha { get; set; }

    public int Cod_Horario { get; set; }

    // Navegación
    [ForeignKey("Cod_Cancha")]
    public Cancha Cancha { get; set; } = null!;

    [ForeignKey("Cod_Horario")]
    public HorarioDisponible HorarioDisponible { get; set; } = null!;
}
