namespace FutbolYaAPI.Entidades;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

// Tabla intermedia N:M: materiales deportivos y cantidad asociados a una reserva.
public class Reserva_Material
{
    [Key]
    public int Cod_Reserva_Mat { get; set; }

    public int Cod_Reserva { get; set; }

    public int Cod_Material { get; set; }

    public int Cantidad { get; set; }

    // Navegación
    [ForeignKey("Cod_Reserva")]
    public Reserva Reserva { get; set; }

    [ForeignKey("Cod_Material")]
    public Material_Deportivo Material_Deportivo { get; set; }
    
}