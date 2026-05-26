namespace FutbolyaAPIS.Entidades;
using System.ComponentModel.DataAnnotations;

public class Producto
{
    [Key]
    public int Cod_Producto { get; set; }

    public string Nombre { get; set; }

    public int Cantidad { get; set; }

    public decimal Precio { get; set; }

    // Valores posibles: "Bebida" o "Comida"
    public string Tipo { get; set; }


    // Navegación
    public ICollection<VentaDetallada> VentasDetalladas { get; set; }
}