using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biblioteca.Models
{
    public class Session
    {
        public int Id { get; set; }

        public string Usuario { get; set; }

        public string Rol { get; set; }

        public string Inicio { get; set; }
    }
}
