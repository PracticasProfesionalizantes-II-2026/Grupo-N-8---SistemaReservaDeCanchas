namespace FutbolYaAPI.Entidades;
using System.ComponentModel.DataAnnotations;

public class Material_Deportivo
{
    [Key]
    public int Cod_Material { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public int Cant_Material { get; set; }

    public bool Activo { get; set; } = true;

    // Navegación
    public ICollection<Reserva_Material> ReservaMateriales { get; set; } = new List<Reserva_Material>();
}
