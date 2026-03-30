using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sistema_de_reserva_Futbol_Ya.Models
{
    public class Reserva
    {
        [Key]
        public int CodigoReserva { get; set; }
        public DateTime FechaReserva { get; set; }
        public TimeSpan HoraDesde { get; set; }
        public TimeSpan HoraHasta { get; set; }
        public string DniCliente { get; set; }
        public string TelefonoCliente { get; set; }
        public int Duracion { get; set; } // Corregir horas
        public int CodigoCancha { get; set; }
        public int CodigoUsuario { get; set; } //corregir id para que cummpla con ET.

        // Relaciones
        public Cancha Cancha { get; set; }
        public Usuario Usuario { get; set; }

        //Esta colección representa que una reserva puede tener varios materiales asociados.
        public ICollection<Reserva_Material> Reserva_Materiales { get; set; } = new List<Reserva_Material>(); 

    }
}
