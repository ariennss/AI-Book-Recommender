using BookRecommender.DBObjects;
using System.Collections.Generic;

namespace WebApplication1.ViewModels
{
    public class AISuggestionsModel
    {
        public string Query { get; set; } = "";
        public List<Book> Recommendations { get; set; } = new List<Book>();

        public Dictionary<Book, int> AlreadyRatedBooks { get; set; }
    }
}
