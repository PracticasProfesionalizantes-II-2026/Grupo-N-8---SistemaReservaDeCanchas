using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sistema_de_reserva_Futbol_Ya.Models
{
    public class Material_Deportivo
    {
        [Key]
        public int CodigoMaterial { get; set; }
        public string Nombre { get; set; }
        public int CantidadDisponible { get; set; }

        //Esta colección representa que un material deportivo puede estar asociado a varias reservas.
        public ICollection<Reserva_Material> Reserva_Materiales { get; set; } = new List<Reserva_Material>(); 

    }
}
