using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sistema_de_reserva_Futbol_Ya.Models
{
    public class Producto
    {
        [Key]
        public int CodigoProducto { get; set; }
        public string Nombre { get; set; }
        public int Cantidad { get; set; }
        public decimal Precio { get; set; }
        public bool Tipo { get; set; } // True para Comida, False para Bebida

        //este icollection representa que un producto puede estar en varias ventas detalladas.
        public ICollection<Venta_Detallada> VentasDetalladas { get; set; } = new List<Venta_Detallada>();
    }
}
