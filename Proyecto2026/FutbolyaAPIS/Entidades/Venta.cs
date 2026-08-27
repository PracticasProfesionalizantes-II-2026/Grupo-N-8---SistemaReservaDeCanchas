namespace FutbolyaAPIS.Entidades;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Venta
{
    [Key]
    public int Cod_Venta { get; set; }

    public DateTime Fecha { get; set; }

    public TimeSpan Hora { get; set; }

    public decimal MontoTotal { get; set; }

    // FK hacia Usuario
    public int Cod_Usuario { get; set; }

    // Navegación
    [ForeignKey("Cod_Usuario")]
    public Usuario Usuario { get; set; }

    public ICollection<VentaDetallada> VentasDetalladas { get; set; } = new List<VentaDetallada>();
}