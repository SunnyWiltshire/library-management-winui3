using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biblioteca.Models
{
    public class User
    {
        public int Id { get; set; }

        public string Nombre { get; set; }

        public string Usuario { get; set; }

        public string Correo { get; set; }

        public string Password { get; set; }

        public string Rol { get; set; }

        public bool Activo { get; set; }

        public int IntentosFallidos { get; set; }

        public string BloqueadoHasta { get; set; }

        public string EstadoTexto
        {
            get
            {
                return Activo ? "Activo" : "Inactivo";
            }
        }

        public string EstadoFondo
        {
            get
            {
                return Activo ? "#10391A" : "#3B1111";
            }
        }

        public string EstadoBorde
        {
            get
            {
                return Activo ? "#22C55E" : "#EF4444";
            }
        }

        public string EstadoColorTexto
        {
            get
            {
                return Activo ? "#86EFAC" : "#FCA5A5";
            }
        }

        // Agrega en User.cs
        public string Inicial
            => string.IsNullOrWhiteSpace(Nombre) ? "?" : Nombre[0].ToString().ToUpper();

        public string AvatarColor1
        {
            get
            {
                string[] c = { "#3D1F8C", "#1F4D8C", "#8C1F3D", "#1F8C6B", "#8C5A1F", "#6B1F8C" };
                return c[Id % c.Length];
            }
        }

        public string AvatarColor2
        {
            get
            {
                string[] c = { "#7C5CFF", "#22D3EE", "#FF5F7A", "#22EEB8", "#FFB86B", "#C55CFF" };
                return c[Id % c.Length];
            }
        }

        public string RolFondo
        {
            get => Rol switch
            {
                "Administrador" => "#28FF5F7A",
                "Bibliotecario" => "#287C5CFF",
                "Lector" => "#2822D3EE",
                _ => "#18FFFFFF"
            };
        }

        public string RolColor
        {
            get => Rol switch
            {
                "Administrador" => "#FF5F7A",
                "Bibliotecario" => "#C5B8FF",
                "Lector" => "#22D3EE",
                _ => "#8B8FC4"
            };
        }
    }
}
