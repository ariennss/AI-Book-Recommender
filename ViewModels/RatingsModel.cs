using BookRecommender.DBObjects;

namespace WebApplication1.ViewModels
{
    public class RatingsModel
    {
        public string BookTitle { get; set; }
        public string AuthorName { get; set; }

        public List<Book> MatchingBooks { get; set; } = new List<Book>();
        public Dictionary<Book,int> AlreadyRatedBooks { get; set; } // couple Book - its rating
    }
}
