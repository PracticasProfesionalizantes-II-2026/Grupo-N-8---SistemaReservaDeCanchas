using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sistema_de_reserva_Futbol_Ya.Models
{
    public class Reserva_Material
    {
        [Key]
        public int CodigoReservaMaterial { get; set; }
        public int CodigoReserva { get; set; }
        public int CodigoMaterial { get; set; }
        public int Cantidad { get; set; }


        // Relaciones
        public Reserva Reserva { get; set; }
        public Material_Deportivo Material_Deportivo { get; set; }

    }
}
