using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Biblioteca.Models;

namespace Biblioteca.Services
{
    public static class BookService
    {
        public static List<Book> GetAll()
        {
            string json = File.ReadAllText(DataService.BooksFile);

            return JsonSerializer.Deserialize<List<Book>>(json);
        }

        public static void Save(List<Book> books)
        {
            File.WriteAllText(
                DataService.BooksFile,
                JsonSerializer.Serialize(
                    books,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    }));
        }

        public static int NextId()
        {
            var books = GetAll();

            if (books.Count == 0)
                return 1;

            return books.Max(x => x.Id) + 1;
        }
    }
}
