using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Biblioteca.Models;

namespace Biblioteca.Services
{
    public static class UserService
    {
        public static List<User> GetAll()
        {
            string json = File.ReadAllText(DataService.UsersFile);

            return JsonSerializer.Deserialize<List<User>>(json);
        }

        public static void Save(List<User> users)
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

        public static int NextId()
        {
            var users = GetAll();

            if (users.Count == 0)
                return 1;

            return users.Max(x => x.Id) + 1;
        }
    }
}