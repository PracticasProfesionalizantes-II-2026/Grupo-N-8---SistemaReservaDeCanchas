namespace FutbolyaAPIS.Entidades;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Reserva
{
    [Key]
    public int Cod_Reserva { get; set; }

    public DateTime Fecha { get; set; }

    public string Dni_Cliente { get; set; }

    public string Telefono_Cliente { get; set; }

    // FK hacia Cancha
    public int Cod_Cancha { get; set; }

    // FK hacia Usuario
    public int Cod_Usuario { get; set; }

    public int Duracion { get; set; }

    // FK hacia HorarioDisponible
    public int Cod_Horario { get; set; }

    // Navegación
    [ForeignKey("Cod_Cancha")]
    public Cancha Cancha { get; set; }

    [ForeignKey("Cod_Usuario")]
    public Usuario Usuario { get; set; }

    [ForeignKey("Cod_Horario")]
    public HorarioDisponible HorarioDisponible { get; set; }

    public ICollection<Reserva_Material> ReservaMateriales { get; set; } = new List<Reserva_Material>();
}