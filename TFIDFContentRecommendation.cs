using BookRecommender.Repositories;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace WebApplication1
{
    public class TFIDFContentRecommendation
    {
        private readonly IBookRepository _bookRepository;
        private static Dictionary<int, List<string>> descriptionVectors = new Dictionary<int, List<string>>();
        private static List<string> stopwords = new List<string>();
        private readonly HttpClient _httpClient;

        public TFIDFContentRecommendation(IBookRepository bookrepo)
        {
            // Initialize the HTTP client for the Python API
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("http://localhost:5000/"); // Python API base address

            // Load stopwords
            string filePath = "C:\\tesi\\stopwordsKaggle.txt";
            stopwords = new List<string>(File.ReadAllLines(filePath).Select(word => word.ToLower())); // Lowercase stopwords

            _bookRepository = bookrepo;
            var selectedPopularBooks = bookrepo.GetAllBooks().Where(x => x.RatingsCount > 1000).ToList();

            foreach (var selectedBook in selectedPopularBooks)
            {
                // Get lemmatized description
                var lemmatizedWords = LemmatizeText(selectedBook.Description).Result;

                // Further preprocess: remove stopwords, clean punctuation
                var processedWords = lemmatizedWords
                    .Select(word => new string(word.Where(char.IsLetterOrDigit).ToArray())) // Keep only letters and digits
                    .Where(cleanedWord => !string.IsNullOrEmpty(cleanedWord) && !stopwords.Contains(cleanedWord)) // Remove stopwords
                    .ToList();

                descriptionVectors.Add(selectedBook.Id, processedWords);
            }
        }

        public void FindTop10MostSimilarToDescription(string description)
        {
            // Preprocess and lemmatize the input description
            var lemmatizedInput = LemmatizeText(description).Result;

            // Remove stopwords and punctuation
            var inputTransformedIntoWordList = lemmatizedInput
                .Select(word => new string(word.Where(char.IsLetterOrDigit).ToArray())) // Keep only letters and digits
                .Where(cleanedWord => !string.IsNullOrEmpty(cleanedWord) && !stopwords.Contains(cleanedWord)) // Remove stopwords
                .ToList();

            // Compute TF-IDF for input description
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

        private async Task<List<string>> LemmatizeText(string text)
        {
            try
            {
                // Prepare JSON payload for the API
                var payload = new StringContent($"{{\"text\":\"{text}\"}}", Encoding.UTF8, "application/json");

                // Send POST request to the Python API
                var response = await _httpClient.PostAsync("lemmatize", payload);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();

                    // Extract and return the lemmatized words
                    var lemmatizedWords = System.Text.Json.JsonSerializer.Deserialize<LemmatizerResponse>(result);
                    return lemmatizedWords.Lemmatized;
                }
                else
                {
                    Console.WriteLine($"Error: Unable to lemmatize text. HTTP Status: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during lemmatization: {ex.Message}");
            }

            return new List<string>(); // Return an empty list in case of failure
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

    public class LemmatizerResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("lemmatized")]
        public List<string> Lemmatized { get; set; }
    }
}
