namespace FutbolYaAPI.Entidades;
using System.ComponentModel.DataAnnotations;

public class Producto
{
    [Key]
    public int Cod_Producto { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public int Cantidad { get; set; }

    public decimal Precio { get; set; }

    // Valores posibles: "Bebida" o "Comida"
    public string Tipo { get; set; } = string.Empty;

    public bool Activo { get; set; } = true;

    // Navegación
    public ICollection<VentaDetallada> VentasDetalladas { get; set; } = new List<VentaDetallada>();
}
