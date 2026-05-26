namespace FutbolyaAPIS.Entidades;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class VentaDetallada
{
    [Key]
    public int Cod_Venta_Detallada { get; set; }

    // FK hacia Venta
    public int Cod_Venta { get; set; }

    // FK hacia Producto
    public int Cod_Producto { get; set; }

    public int Cantidad { get; set; }

    public decimal Precio { get; set; }

    public decimal SubTotal { get; set; }

    // Navegación
    [ForeignKey("Cod_Venta")]
    public Venta Venta { get; set; }

    [ForeignKey("Cod_Producto")]
    public Producto Producto { get; set; }
}