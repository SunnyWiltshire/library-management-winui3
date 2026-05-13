using Biblioteca.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Biblioteca.Services
{
    public static class LoanService
    {
        public static List<Loan> GetAll()
        {
            string json = File.ReadAllText(DataService.LoansFile);

            return JsonSerializer.Deserialize<List<Loan>>(json);
        }

        public static void Save(List<Loan> data)
        {
            File.WriteAllText(
                DataService.LoansFile,
                JsonSerializer.Serialize(
                    data,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    }));
        }

        public static int NextId()
        {
            var list = GetAll();

            if (list.Count == 0)
                return 1;

            return list.Max(x => x.Id) + 1;
        }
    }
}