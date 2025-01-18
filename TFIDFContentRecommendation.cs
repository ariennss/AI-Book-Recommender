using BookRecommender.Repositories;

namespace WebApplication1
{
    public class TFIDFContentRecommendation
    {
        private readonly IBookRepository _bookRepository;
        private static Dictionary<int, List<string>> descriptionVectors = new Dictionary<int, List<string>>();

        public TFIDFContentRecommendation(IBookRepository bookrepo)
        {
            _bookRepository = bookrepo;
            var selectedPopularBooks = bookrepo.GetAllBooks().Where(x => x.RatingsCount > 1000).ToList();
            foreach (var selectedBook in selectedPopularBooks)
            {
                var wordsInDescription = selectedBook.Description.Split(' ')
                .Select(word => new string(word.Where(char.IsLetterOrDigit).ToArray())) // Keep only letters and digits
                .Where(cleanedWord => !string.IsNullOrEmpty(cleanedWord)) // Filter out empty strings
                .ToList();
                descriptionVectors.Add(selectedBook.Id, wordsInDescription);
            }
        }
    }
}
