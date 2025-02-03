using BookRecommender.DBObjects;

namespace WebApplication1
{
    public interface IHybridContentRecommendation
    {
        Task<List<Book>> FindTop10MostSimilarToDescriptionAsync(string description);
    }
}
