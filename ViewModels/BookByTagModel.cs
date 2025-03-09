using BookRecommender.DBObjects;

namespace WebApplication1.ViewModels
{
    public class BookByTagModel
    {
        public string Query { get; set; } = "";
        public List<Book> Recommendations { get; set; } = new List<Book>();

        public Dictionary<Book, int> AlreadyRatedBooks { get; set; }

        public List<Book> SearchedBooks { get; set; } = new List<Book>();

        public Book ChosenOne { get; set; }
    }
}
