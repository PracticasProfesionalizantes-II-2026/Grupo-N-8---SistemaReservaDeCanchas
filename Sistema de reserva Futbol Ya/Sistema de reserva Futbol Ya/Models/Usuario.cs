using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sistema_de_reserva_Futbol_Ya.Models
{
    public class Usuario
    {
        [Key]
        public int CodigoUsuario { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string Dni { get; set; }
        public string Direccion { get; set; }
        public string CorreoElectronico { get; set; }
        public string Contraseña { get; set; }
        public bool Rol { get; set; } // True para admin, False para usuario operador
        public bool CambiarContraseña { get; set; } // Indica si el usuario debe cambiar su contraseña en el próximo inicio de sesión


        //este icollection representa que un usuario puede tener varias reservas y ventas.
        public ICollection<Reserva> Reservas { get; set; } = new List<Reserva>(); 
        public ICollection<Venta> Ventas { get; set; } = new List<Venta>(); 


    }
}
