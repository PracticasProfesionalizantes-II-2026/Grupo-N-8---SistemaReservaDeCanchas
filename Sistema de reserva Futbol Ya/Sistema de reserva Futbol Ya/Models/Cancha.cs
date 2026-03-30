using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sistema_de_reserva_Futbol_Ya.Models
{
    public class Cancha
    {
        [Key]
        public int CodigoCancha { get; set; }
        public string Descripcion { get; set; }
        public bool Estado { get; set; } // True para activa, False para inactiva

         
        //este icollection representa que una cancha puede tener varias reservas.
        public ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
    }
}
