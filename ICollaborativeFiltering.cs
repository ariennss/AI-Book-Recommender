using BookRecommender.DBObjects;

namespace WebApplication1
{
    public interface ICollaborativeFiltering
    {
        List<Book> SuggestionsFor(string username);
    }
}
