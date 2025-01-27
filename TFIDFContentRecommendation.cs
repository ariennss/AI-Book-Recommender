using BookRecommender.DBObjects;
using BookRecommender.Repositories;
using System.Net.NetworkInformation;

namespace WebApplication1
{
    public class TFIDFContentRecommendation
    {
        private readonly IBookRepository _bookRepository;
        private static Dictionary<int, List<string>> descriptionVectors = new Dictionary<int, List<string>>();
        private static List<string> stopwords = new List<string>();

        public TFIDFContentRecommendation(IBookRepository bookrepo)
        {
            string filePath = "C:\\tesi\\stopwordsKaggle.txt";
            stopwords = new List<string>(File.ReadAllLines(filePath).Select(word => word.ToLower())); // Lowercase stopwords

            _bookRepository = bookrepo;
            var selectedPopularBooks = bookrepo.GetAllBooks().Where(x => x.RatingsCount > 1000).ToList();
            foreach (var selectedBook in selectedPopularBooks)
            {
                var wordsInDescription = selectedBook.Description
                    .ToLower() // Convert description to lowercase
                    .Split(' ')
                    .Select(word => new string(word.Where(char.IsLetterOrDigit).ToArray())) // Keep only letters and digits
                    .Where(cleanedWord => !string.IsNullOrEmpty(cleanedWord) && !stopwords.Contains(cleanedWord)) // Filter out empty strings
                    .ToList();
                descriptionVectors.Add(selectedBook.Id, wordsInDescription);
            }
        }

        public void FindTop10MostSimilarToDescription(string description)
        {
            List<string> inputTransformedIntoWordList = description
                .ToLower() // Convert input description to lowercase
                .Split(' ')
                .Select(word => new string(word.Where(char.IsLetterOrDigit).ToArray())) // Keep only letters and digits
                .Where(cleanedWord => !string.IsNullOrEmpty(cleanedWord) && !stopwords.Contains(cleanedWord)) // Filter out empty strings
                .ToList();
            var idf = ComputeIDF();
            var inputTfidf = CalculateTFIDF(inputTransformedIntoWordList, idf);

            // List to hold book IDs and their similarity scores
            var similarities = new List<(int BookId, double Similarity)>();

            foreach (var (bookId, words) in descriptionVectors)
            {
                var bookTfidf = CalculateTFIDF(words, idf);
                var similarity = CosineSimilarity(inputTfidf, bookTfidf);
                similarities.Add((bookId, similarity));
            }

            // Sort by similarity descending and take the top 10
            var top10Books = similarities
                .OrderByDescending(pair => pair.Similarity)
                .Take(10)
                .Select(pair => _bookRepository.GetBookById(pair.BookId))
                .ToList();

            // Print the top 10 most similar books
            Console.WriteLine("Top 10 most similar books:");
            foreach (var book in top10Books)
            {
                Console.WriteLine($"- {book.Title} (Similarity: {similarities.First(s => s.BookId == book.Id).Similarity:F4})");
            }
        }

        private Dictionary<string, double> CalculateTFIDF(List<string> words, Dictionary<string, double> idf)
        {
            var termFrequency = words
                .GroupBy(word => word)
                .ToDictionary(g => g.Key, g => g.Count() / (double)words.Count);
            var tfidf = termFrequency
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value * idf.GetValueOrDefault(kvp.Key, 0)); // Use 0 if key is missing
            return tfidf;
        }

        private Dictionary<string, double> ComputeIDF()
        {
            var totalDocuments = descriptionVectors.Count();
            var termDocumentFrequency = new Dictionary<string, int>();

            foreach (var vector in descriptionVectors.Values)
            {
                foreach (var term in vector.Distinct())
                {
                    if (!termDocumentFrequency.ContainsKey(term))
                        termDocumentFrequency[term] = 0;

                    termDocumentFrequency[term]++;
                }
            }

            return termDocumentFrequency
                .ToDictionary(kvp => kvp.Key, kvp => Math.Log(totalDocuments / (1 + kvp.Value)));
        }

        private double CosineSimilarity(Dictionary<string, double> vec1, Dictionary<string, double> vec2)
        {
            var intersect = vec1.Keys.Intersect(vec2.Keys);
            var dotProduct = intersect.Sum(key => vec1[key] * vec2[key]);

            var magnitude1 = Math.Sqrt(vec1.Values.Sum(val => val * val));
            var magnitude2 = Math.Sqrt(vec2.Values.Sum(val => val * val));

            return magnitude1 == 0 || magnitude2 == 0 ? 0 : dotProduct / (magnitude1 * magnitude2);
        }
    }
}
