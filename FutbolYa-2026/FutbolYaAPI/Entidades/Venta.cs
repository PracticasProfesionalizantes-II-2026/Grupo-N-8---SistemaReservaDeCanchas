namespace FutbolYaAPI.Entidades;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Venta
{
    [Key]
    public int Cod_Venta { get; set; }

    public DateTime Fecha { get; set; }

    public TimeSpan Hora { get; set; }

    public decimal MontoTotal { get; set; }

    public int Cod_Usuario { get; set; }

    public bool Activo { get; set; } = true;

    // Navegación
    [ForeignKey("Cod_Usuario")]
    public Usuario Usuario { get; set; } = null!;

    public ICollection<VentaDetallada> VentasDetalladas { get; set; } = new List<VentaDetallada>();
}
