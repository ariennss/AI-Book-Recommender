using BookRecommender.DBObjects;

namespace WebApplication1
{
    public interface ITagsSimilarity
    {
        List<Book> GetSimilarBooks(int inputBookId, int topN = 10);
        Dictionary<int, float> GetTagSimilarity(List<string> lemmatizedInput);
    }
}