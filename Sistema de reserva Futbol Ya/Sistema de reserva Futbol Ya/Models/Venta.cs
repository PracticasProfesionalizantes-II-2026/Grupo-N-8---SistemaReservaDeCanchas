using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sistema_de_reserva_Futbol_Ya.Models
{
    public class Venta
    {
        [Key]
        public int CodigoVenta { get; set; }
        public DateTime FechaVenta { get; set; }
        public TimeSpan HoraVenta { get; set; }
        public decimal MontoTotal { get; set; }

        //codigo usuario
        public int CodigoUsuario { get; set; }


        //relaciones
        public Usuario Usuario { get; set; }

        //esta colección representa que una venta puede tener varios detalles asociados.
        public ICollection<Venta_Detallada> Venta_Detalladas { get; set; } = new List<Venta_Detallada>(); 
    }
}
