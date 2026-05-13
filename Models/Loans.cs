using System;
using System.Globalization;

namespace Biblioteca.Models
{
    public class Loan
    {
        public int Id { get; set; }

        public int BookId { get; set; }

        public int UserId { get; set; }

        public string Libro { get; set; }

        public string Usuario { get; set; }

        public string FechaPrestamo { get; set; }

        public string FechaDevolucion { get; set; }

        public bool Devuelto { get; set; }

        public string EstadoTexto
        {
            get
            {
                if (Devuelto)
                    return "Devuelto";

                DateTime fecha = DateTime.Parse(FechaDevolucion);

                if (fecha.Date < DateTime.Today)
                    return "Atrasado";

                if (fecha.Date == DateTime.Today)
                    return "Vence hoy";

                return "Activo";
            }
        }

        public string EstadoFondo
        {
            get
            {
                switch (EstadoTexto)
                {
                    case "Devuelto":
                        return "#1F2937";

                    case "Atrasado":
                        return "#3B1111";

                    case "Vence hoy":
                        return "#3A2A05";

                    default:
                        return "#10391A";
                }
            }
        }

        public string EstadoBorde
        {
            get
            {
                switch (EstadoTexto)
                {
                    case "Devuelto":
                        return "#9CA3AF";

                    case "Atrasado":
                        return "#EF4444";

                    case "Vence hoy":
                        return "#F59E0B";

                    default:
                        return "#22C55E";
                }
            }
        }

        public string EstadoColorTexto
        {
            get
            {
                switch (EstadoTexto)
                {
                    case "Devuelto":
                        return "#E5E7EB";

                    case "Atrasado":
                        return "#FCA5A5";

                    case "Vence hoy":
                        return "#FCD34D";

                    default:
                        return "#86EFAC";
                }
            }
        }
    }
}