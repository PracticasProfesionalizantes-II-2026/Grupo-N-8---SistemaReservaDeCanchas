namespace FutbolyaAPIS.Entidades;
using System.ComponentModel.DataAnnotations;


public class Material_Deportivo
{
    [Key]
    public int Cod_Material { get; set; }

    public string Nombre { get; set; }

    public int Cant_Material { get; set; }


    // Navegación
    public ICollection<Reserva_Material> ReservaMateriales { get; set; }

}