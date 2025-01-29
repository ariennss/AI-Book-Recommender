using BookRecommender.Repositories;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
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
            _httpClient.BaseAddress = new Uri("http://localhost:5000/");

            _bookRepository = bookrepo;

            // Preload and lemmatize all descriptions in one batch
            
        }

        private async Task PreprocessAndLemmatizeDescriptionsAsync()
        {
            var allBooks = _bookRepository.GetAllBooks();

            // Prepare the payload for the API
            var descriptionsPayload = new
            {
                descriptions = allBooks.Select(book => new
                {
                    id = book.Id,
                    text = book.Description
                }).ToList()
            };

            // Serialize the payload to JSON
            var jsonPayload = JsonSerializer.Serialize(descriptionsPayload);

            try
            {
                // Send a POST request to the Python API
                var response = _httpClient.PostAsync("batch_lemmatize",
                    new StringContent(jsonPayload, Encoding.UTF8, "application/json")).Result;

                if (response.IsSuccessStatusCode)
                {
                    // Deserialize the response into a dictionary
                    var responseContent = await response.Content.ReadAsStringAsync();
                    descriptionVectors = JsonSerializer.Deserialize<Dictionary<int, List<string>>>(responseContent);

                    Console.WriteLine("Descriptions successfully lemmatized and cached.");
                }
                else
                {
                    Console.WriteLine($"Error: Unable to preprocess descriptions. Status: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during preprocessing: {ex.Message}");
            }
        }

        public async Task FindTop10MostSimilarToDescriptionAsync(string description)
        {
            await PreprocessAndLemmatizeDescriptionsAsync();
            // Process the input description (lemmatize it via the Python API)
            var lemmatizedInput = await LemmatizeText(description);

            var idf = ComputeIDF();
            var inputTfidf = CalculateTFIDF(lemmatizedInput, idf);

            var similarities = new List<(int BookId, double Similarity)>();

            foreach (var (bookId, words) in descriptionVectors)
            {
                var bookTfidf = CalculateTFIDF(words, idf);
                var similarity = CosineSimilarity(inputTfidf, bookTfidf);
                similarities.Add((bookId, similarity));
            }

            var top10Books = similarities
                .OrderByDescending(pair => pair.Similarity)
                .Take(100)
                .Select(pair => _bookRepository.GetBookById(pair.BookId))
                .OrderByDescending(x => x.RatingsCount)
                .Take(10)
                .ToList();

            Console.WriteLine("Top 10 most similar books:");
            foreach (var book in top10Books)
            {
                Console.WriteLine($"- {book.Title}");
            }
        }

        private async Task<List<string>> LemmatizeText(string text)
        {
            try
            {
                var payload = new StringContent($"{{\"text\":\"{text}\"}}", Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("lemmatize", payload);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    var lemmatizedWords = JsonSerializer.Deserialize<List<string>>(result);
                    return lemmatizedWords ?? new List<string>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during lemmatization: {ex.Message}");
            }

            return new List<string>();
        }

        private Dictionary<string, double> CalculateTFIDF(List<string> words, Dictionary<string, double> idf)
        {
            var termFrequency = words
                .GroupBy(word => word)
                .ToDictionary(g => g.Key, g => g.Count() / (double)words.Count);
            var tfidf = termFrequency
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value * idf.GetValueOrDefault(kvp.Key, 0));
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
