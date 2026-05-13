using System.IO;
using System.Text.Json;
using Biblioteca.Models;

namespace Biblioteca.Services
{
    public static class SessionService
    {
        public static Session GetSession()
        {
            if (!File.Exists(DataService.SessionFile))
                return null;

            string json = File.ReadAllText(DataService.SessionFile);

            if (string.IsNullOrWhiteSpace(json) || json == "{}")
                return null;

            return JsonSerializer.Deserialize<Session>(json);
        }

        public static void ClearSession()
        {
            File.WriteAllText(DataService.SessionFile, "{}");
        }
    }
}
