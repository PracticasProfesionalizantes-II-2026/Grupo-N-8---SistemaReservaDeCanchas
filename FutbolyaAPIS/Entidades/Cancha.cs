namespace FutbolyaAPIS.Entidades;
using System.ComponentModel.DataAnnotations;
public class Cancha
{
    [Key]
    public int Cod_Cancha { get; set; }

    public string Nombre { get; set; }

    public string Descripcion { get; set; }

    // Valores posibles: TRUE "Disponible" o FALSE "En Mantenimiento"
    public bool Estado { get; set; }

    // Navegación
    public ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
}