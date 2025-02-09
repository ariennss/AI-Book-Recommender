using BookRecommender.DBObjects;

namespace WebApplication1.ViewModels
{
    public class ProfileModel
    {
        public List<Book> CurrentUserRatings { get; set; }
        public Dictionary <int, int> BookIdRating { get; set; } 
        public string currentUserName { get; set; }
    }
}
