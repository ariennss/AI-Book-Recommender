using BookRecommender.DBObjects;

namespace WebApplication1.ViewModels
{
    public class HomeViewModel
    {
        public List<Book> MostPopularBooks { get; set; }
        public List<Book> SuggestedBooks { get; set; }
        public object BestReviewedBooks { get; set; }
    }
}