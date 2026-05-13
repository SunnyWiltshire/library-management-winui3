using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Biblioteca.Models;

namespace Biblioteca.Services
{
    public static class DataService
    {
        public static string BasePath =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.MyDocuments),
                "Biblioteca",
                "Data");

        public static string UsersFile => Path.Combine(BasePath, "users.json");
        public static string BooksFile => Path.Combine(BasePath, "books.json");
        public static string LoansFile => Path.Combine(BasePath, "loans.json");
        public static string SessionFile => Path.Combine(BasePath, "session.json");
        public static string LogsFile => Path.Combine(BasePath, "logs.json");

        private static JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
        };

        public static void Initialize()
        {
            Directory.CreateDirectory(BasePath);

            CreateUsers();
            CreateBooks();
            CreateLoans();
            CreateSession();
            CreateLogs();
        }

        private static void CreateUsers()
        {
            if (File.Exists(UsersFile))
                return;

            var users = new List<User>
            {
                new User
                {
                    Id = 1,
                    Nombre = "Administrador General",
                    Usuario = "admin",
                    Correo = "admin@biblioteca.com",
                    Password = "Admin2026",
                    Rol = "Administrador",
                    Activo = true
                },
                new User
                {
                    Id = 2,
                    Nombre = "María López",
                    Usuario = "maria",
                    Correo = "maria@biblioteca.com",
                    Password = "123456",
                    Rol = "Lector",
                    Activo = true
                },
                new User
                {
                    Id = 3,
                    Nombre = "Juan Pérez",
                    Usuario = "juan",
                    Correo = "juan@mail.com",
                    Password = "123456",
                    Rol = "Lector",
                    Activo = true
                },
                new User
                {
                    Id = 4,
                    Nombre = "Ana Torres",
                    Usuario = "ana",
                    Correo = "ana@mail.com",
                    Password = "123456",
                    Rol = "Lector",
                    Activo = true
                },
                new User
                {
                    Id = 5,
                    Nombre = "Pedro Ruiz",
                    Usuario = "pedro",
                    Correo = "pedro@mail.com",
                    Password = "123456",
                    Rol = "Lector",
                    Activo = true
                }
            };

            Save(UsersFile, users);
        }

        private static void CreateBooks()
        {
            if (File.Exists(BooksFile))
                return;

            var books = new List<Book>
            {
                new Book { Id = 1, Titulo = "Clean Code", Autor = "Robert C. Martin", Categoria = "Programación", ISBN = "9780132350884", Anio = 2008, CantidadTotal = 5, Disponibles = 4, Activo = true },
                new Book { Id = 2, Titulo = "C# Profesional", Autor = "Andrew Troelsen", Categoria = "Programación", ISBN = "9781484278680", Anio = 2023, CantidadTotal = 4, Disponibles = 3, Activo = true },
                new Book { Id = 3, Titulo = "Bases de Datos", Autor = "Elmasri", Categoria = "Datos", ISBN = "9786073222457", Anio = 2019, CantidadTotal = 3, Disponibles = 3, Activo = true },
                new Book { Id = 4, Titulo = "Algoritmos", Autor = "Cormen", Categoria = "Computación", ISBN = "9780262033848", Anio = 2015, CantidadTotal = 2, Disponibles = 1, Activo = true },
                new Book { Id = 5, Titulo = "Redes Informáticas", Autor = "Tanenbaum", Categoria = "Redes", ISBN = "9786071514936", Anio = 2020, CantidadTotal = 6, Disponibles = 6, Activo = true }
            };

            Save(BooksFile, books);
        }

        private static void CreateLoans()
        {
            if (File.Exists(LoansFile))
                return;

            var loans = new List<Loan>
            {
                new Loan
                {
                    Id = 1,
                    BookId = 1,
                    UserId = 3,
                    Libro = "Clean Code",
                    Usuario = "Juan Pérez",
                    FechaPrestamo = DateTime.Today.AddDays(-3).ToString("yyyy-MM-dd"),
                    FechaDevolucion = DateTime.Today.AddDays(7).ToString("yyyy-MM-dd"),
                    Devuelto = false
                },
                new Loan
                {
                    Id = 2,
                    BookId = 2,
                    UserId = 4,
                    Libro = "C# Profesional",
                    Usuario = "Ana Torres",
                    FechaPrestamo = DateTime.Today.AddDays(-20).ToString("yyyy-MM-dd"),
                    FechaDevolucion = DateTime.Today.AddDays(-10).ToString("yyyy-MM-dd"),
                    Devuelto = true
                },
                new Loan
                {
                    Id = 3,
                    BookId = 4,
                    UserId = 5,
                    Libro = "Algoritmos",
                    Usuario = "Pedro Ruiz",
                    FechaPrestamo = DateTime.Today.AddDays(-15).ToString("yyyy-MM-dd"),
                    FechaDevolucion = DateTime.Today.AddDays(-5).ToString("yyyy-MM-dd"),
                    Devuelto = false
                }
            };

            Save(LoansFile, loans);
        }

        private static void CreateSession()
        {
            if (!File.Exists(SessionFile))
                File.WriteAllText(SessionFile, "{}");
        }

        private static void CreateLogs()
        {
            if (File.Exists(LogsFile))
                return;

            var logs = new List<LogEntry>
            {
                new LogEntry
                {
                    Fecha = DateTime.Now.AddMinutes(-40).ToString("yyyy-MM-dd HH:mm:ss"),
                    Usuario = "admin",
                    Accion = "Inicio de sesión"
                },
                new LogEntry
                {
                    Fecha = DateTime.Now.AddMinutes(-35).ToString("yyyy-MM-dd HH:mm:ss"),
                    Usuario = "admin",
                    Accion = "Libro agregado: Clean Code"
                },
                new LogEntry
                {
                    Fecha = DateTime.Now.AddMinutes(-25).ToString("yyyy-MM-dd HH:mm:ss"),
                    Usuario = "admin",
                    Accion = "Préstamo registrado a Juan Pérez"
                },
                new LogEntry
                {
                    Fecha = DateTime.Now.AddMinutes(-10).ToString("yyyy-MM-dd HH:mm:ss"),
                    Usuario = "admin",
                    Accion = "Cierre de sesión"
                }
            };

            Save(LogsFile, logs);
        }

        private static void Save<T>(string file, T data)
        {
            File.WriteAllText(file,
                JsonSerializer.Serialize(data, JsonOptions));
        }

        public static void SaveLog(string usuario, string accion)
        {
            List<LogEntry> logs;

            if (!File.Exists(LogsFile))
            {
                logs = new List<LogEntry>();
            }
            else
            {
                string json = File.ReadAllText(LogsFile);

                logs = string.IsNullOrWhiteSpace(json)
                    ? new List<LogEntry>()
                    : JsonSerializer.Deserialize<List<LogEntry>>(json);
            }

            logs.Add(new LogEntry
            {
                Fecha = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                Usuario = usuario,
                Accion = accion
            });

            File.WriteAllText(
                LogsFile,
                JsonSerializer.Serialize(logs, JsonOptions));
        }
    }
}