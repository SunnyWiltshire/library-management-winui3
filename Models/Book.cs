using Biblioteca.Services;
using Microsoft.UI.Xaml;
using System;

namespace Biblioteca.Models
{
    public class Book
    {
        public int Id { get; set; }

        public string Titulo { get; set; }

        public string Autor { get; set; }

        public string Categoria { get; set; }

        public string ISBN { get; set; }

        public int Anio { get; set; }

        public int CantidadTotal { get; set; }

        public int Disponibles { get; set; }

        public bool Activo { get; set; }

        public string CoverUrl { get; set; }

        // =========================
        // VISIBILIDAD SEGÚN ROL
        // =========================

        public Visibility AccionesVisibility
        {
            get
            {
                var rol = SessionService.GetSession()?.Rol;

                return rol == "Lector" || rol == "Invitado"
                    ? Visibility.Collapsed
                    : Visibility.Visible;
            }
        }

        // =========================
        // COLORES DINÁMICOS
        // =========================

        public string CoverColor
        {
            get
            {
                string[] colores =
                {
                    "#3D1F8C",
                    "#1F4D8C",
                    "#8C1F3D",
                    "#1F8C6B",
                    "#8C5A1F",
                    "#6B1F8C",
                    "#1F6B8C",
                    "#8C3D1F"
                };

                return colores[Math.Abs(Id) % colores.Length];
            }
        }

        public string CoverColor2
        {
            get
            {
                string[] colores =
                {
                    "#7C5CFF",
                    "#22D3EE",
                    "#FF5F7A",
                    "#22EEB8",
                    "#FFB86B",
                    "#C55CFF",
                    "#5CE8FF",
                    "#FF8C5C"
                };

                return colores[Math.Abs(Id) % colores.Length];
            }
        }

        // =========================
        // INICIAL DEL LIBRO
        // =========================

        public string Inicial
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Titulo))
                    return "?";

                return Titulo.Substring(0, 1).ToUpper();
            }
        }

        // =========================
        // ESTADOS
        // =========================

        public string EstadoTexto
        {
            get
            {
                return Disponibles > 0
                    ? "Disponible"
                    : "Sin stock";
            }
        }

        public string EstadoColor
        {
            get
            {
                return Disponibles > 0
                    ? "#22D3EE"
                    : "#FF5F7A";
            }
        }

        public string StockTexto
        {
            get
            {
                return $"{Disponibles}/{CantidadTotal}";
            }
        }

        public string InfoCompleta
        {
            get
            {
                return $"{Autor} • {Anio}";
            }
        }

        public string ISBNTexto
        {
            get
            {
                return string.IsNullOrWhiteSpace(ISBN)
                    ? "Sin ISBN"
                    : ISBN;
            }
        }

        public bool TienePortada
        {
            get
            {
                return !string.IsNullOrWhiteSpace(CoverUrl);
            }
        }

        public bool Disponible
        {
            get
            {
                return Disponibles > 0;
            }
        }
    }
}