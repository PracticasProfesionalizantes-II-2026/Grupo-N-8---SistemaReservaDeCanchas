using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sistema_de_reserva_Futbol_Ya.Models
{
    public class Venta_Detallada
    {
        [Key]
        public int CodigoVentaDetallada { get; set; }
        public int CodigoVenta { get; set; }
        public int CodigoProducto { get; set; }
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Subtotal { get; set; }

        //relaciones
        public Venta Venta { get; set; }
        public Producto Producto { get; set; }
    }
}
