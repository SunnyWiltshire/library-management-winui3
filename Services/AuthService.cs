using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Biblioteca.Models;

namespace Biblioteca.Services
{
    public static class AuthService
    {
        public static string Login(string userInput, string password)
        {
            var users = JsonSerializer.Deserialize<List<User>>(
                File.ReadAllText(DataService.UsersFile));

            var user = users.FirstOrDefault(x =>
                x.Usuario.Equals(userInput, StringComparison.OrdinalIgnoreCase)
                || x.Correo.Equals(userInput, StringComparison.OrdinalIgnoreCase));

            // USUARIO NO EXISTE
            if (user == null)
            {
                SaveLog(userInput, "Intento fallido - usuario no encontrado");
                return "Credenciales incorrectas";
            }

            // CUENTA DESHABILITADA
            if (!user.Activo)
            {
                SaveLog(user.Usuario, "Intento fallido - cuenta deshabilitada");
                return "Cuenta deshabilitada";
            }

            // BLOQUEO TEMPORAL
            if (!string.IsNullOrWhiteSpace(user.BloqueadoHasta))
            {
                DateTime bloqueo = DateTime.Parse(user.BloqueadoHasta);

                if (DateTime.Now < bloqueo)
                {
                    SaveLog(user.Usuario, "Intento fallido - cuenta bloqueada");
                    return "Cuenta bloqueada temporalmente";
                }
            }

            // CONTRASEÑA INCORRECTA
            if (user.Password != password)
            {
                user.IntentosFallidos++;

                SaveLog(user.Usuario,
                    $"Contraseña incorrecta ({user.IntentosFallidos}/5)");

                if (user.IntentosFallidos >= 5)
                {
                    user.BloqueadoHasta =
                        DateTime.Now.AddMinutes(1)
                        .ToString("yyyy-MM-dd HH:mm:ss");

                    user.IntentosFallidos = 0;

                    SaveLog(user.Usuario,
                        "Cuenta bloqueada por 5 intentos fallidos");
                }

                SaveUsers(users);

                return "Credenciales incorrectas";
            }

            // LOGIN EXITOSO
            user.IntentosFallidos = 0;
            user.BloqueadoHasta = "";

            SaveUsers(users);

            SaveSession(user);

            SaveLog(user.Usuario, "Inicio de sesión exitoso");

            return "OK";
        }

        public static void Logout()
        {
            var session = SessionService.GetSession();

            if (session != null)
            {
                SaveLog(session.Usuario, "Cierre de sesión");
            }

            File.WriteAllText(DataService.SessionFile, "{}");
        }

        private static void SaveUsers(List<User> users)
        {
            File.WriteAllText(
                DataService.UsersFile,
                JsonSerializer.Serialize(
                    users,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    }));
        }

        private static void SaveSession(User user)
        {
            var session = new Session
            {
                Id = user.Id,
                Usuario = user.Usuario,
                Rol = user.Rol,
                Inicio = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            File.WriteAllText(
                DataService.SessionFile,
                JsonSerializer.Serialize(
                    session,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    }));
        }

        private static void SaveLog(string usuario, string accion)
        {
            var logs = JsonSerializer.Deserialize<List<LogEntry>>(
                File.ReadAllText(DataService.LogsFile));

            logs.Add(new LogEntry
            {
                Fecha = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                Usuario = usuario,
                Accion = accion
            });

            File.WriteAllText(
                DataService.LogsFile,
                JsonSerializer.Serialize(
                    logs,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    }));
        }
    }
}