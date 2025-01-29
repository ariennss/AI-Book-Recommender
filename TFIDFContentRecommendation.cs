using BookRecommender.Repositories;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace WebApplication1
{
    public class HybridContentRecommendation
    {
        private readonly IBookRepository _bookRepository;
        private static Dictionary<int, List<string>> descriptionVectors = new Dictionary<int, List<string>>();
        private readonly HttpClient _httpClient;
        private readonly string _dbPath = "Data Source=C:\\test\\bookRecommender.db";

        public HybridContentRecommendation(IBookRepository bookrepo)
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("http://localhost:5000/");
            _bookRepository = bookrepo;
        }

        /// <summary>
        /// Finds the top 10 books using both TF-IDF and Word2Vec similarity.
        /// </summary>
        public async Task FindTop10MostSimilarToDescriptionAsync(string description)
        {
            // 1️⃣ Preprocess and lemmatize all book descriptions for TF-IDF
            await PreprocessAndLemmatizeDescriptionsAsync();

            // 2️⃣ Preprocess and lemmatize the input phrase
            var lemmatizedInput = await LemmatizeText(description);

            // 3️⃣ Call Python API to get the Word2Vec embedding of the input phrase
            var inputEmbedding = await GetWord2VecEmbedding(description);
            if (inputEmbedding == null)
            {
                Console.WriteLine("Error: Could not generate Word2Vec embedding.");
                return;
            }

            // 4️⃣ Compute TF-IDF Similarity
            var idf = ComputeIDF();
            var inputTfidf = CalculateTFIDF(lemmatizedInput, idf);
            var tfidfSimilarities = new Dictionary<int, double>();

            foreach (var (bookId, words) in descriptionVectors)
            {
                var bookTfidf = CalculateTFIDF(words, idf);
                var similarity = CosineSimilarity(inputTfidf, bookTfidf);
                tfidfSimilarities[bookId] = similarity;
            }

            // 5️⃣ Compute Word2Vec Similarity (Using DB)
            var word2vecSimilarities = await ComputeWord2VecSimilarities(inputEmbedding);

            // 6️⃣ Combine TF-IDF & Word2Vec Similarity
            var combinedScores = new Dictionary<int, double>();
            foreach (var bookId in tfidfSimilarities.Keys)
            {
                double tfidfScore = tfidfSimilarities.GetValueOrDefault(bookId, 0);
                double w2vScore = word2vecSimilarities.GetValueOrDefault(bookId, 0);

                // Weighting: 50% TF-IDF + 50% Word2Vec
                double finalScore = (tfidfScore * 0.5) + (w2vScore * 0.5);
                combinedScores[bookId] = finalScore;
            }

            // 7️⃣ Retrieve & Sort Books by Combined Score
            var topBooks = combinedScores
                .OrderByDescending(pair => pair.Value)
                .Take(100) // First sort by similarity
                .Select(pair => _bookRepository.GetBookById(pair.Key))
                .OrderByDescending(x => x.RatingsCount) // Then sort by popularity
                .Take(10)
                .ToList();

            // Print Results
            Console.WriteLine("Top 10 most similar books:");
            foreach (var book in topBooks)
            {
                Console.WriteLine($"- {book.Title}");
            }
        }

        /// <summary>
        /// Calls the Python API to get the Word2Vec embedding of the input phrase.
        /// </summary>
        private async Task<List<float>> GetWord2VecEmbedding(string text)
        {
            try
            {
                var payload = new StringContent($"{{\"text\":\"{text}\"}}", Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("word2vec_embed", payload);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<float>>(result);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during embedding: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Preprocesses and lemmatizes all book descriptions for TF-IDF.
        /// </summary>
        private async Task PreprocessAndLemmatizeDescriptionsAsync()
        {
            var allBooks = _bookRepository.GetAllBooks();

            var descriptionsPayload = new
            {
                descriptions = allBooks.Select(book => new
                {
                    id = book.Id,
                    text = book.Description
                }).ToList()
            };

            var jsonPayload = JsonSerializer.Serialize(descriptionsPayload);

            try
            {
                var response = await _httpClient.PostAsync("batch_lemmatize",
                    new StringContent(jsonPayload, Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    descriptionVectors = JsonSerializer.Deserialize<Dictionary<int, List<string>>>(responseContent);
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

        private async Task<List<string>> LemmatizeText(string text)
        {
            try
            {
                var payload = new StringContent($"{{\"text\":\"{text}\"}}", Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("lemmatize", payload);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<string>>(result) ?? new List<string>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during lemmatization: {ex.Message}");
            }

            return new List<string>();
        }

        private async Task<Dictionary<int, double>> ComputeWord2VecSimilarities(List<float> inputVector)
        {
            var word2vecSimilarities = new Dictionary<int, double>();

            using (var conn = new SqliteConnection(_dbPath))
            {
                await conn.OpenAsync();
                var command = conn.CreateCommand();
                command.CommandText = "SELECT book_id, embedding FROM BookEmbeddings";

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        int bookId = reader.GetInt32(0);
                        string embeddingStr = reader.GetString(1);
                        var embeddingList = embeddingStr.Trim('[', ']').Split(',').Select(float.Parse).ToList();

                        double similarity = CosineSimilarity(inputVector, embeddingList);
                        word2vecSimilarities[bookId] = similarity;
                    }
                }
            }

            return word2vecSimilarities;
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

        private Dictionary<string, double> CalculateTFIDF(List<string> words, Dictionary<string, double> idf)
        {
            var termFrequency = words
                .GroupBy(word => word)
                .ToDictionary(g => g.Key, g => g.Count() / (double)words.Count);
            return termFrequency
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value * idf.GetValueOrDefault(kvp.Key, 0));
        }

        private double CosineSimilarity(List<float> vec1, List<float> vec2)
        {
            if (vec1.Count != vec2.Count) return 0;

            double dotProduct = vec1.Zip(vec2, (a, b) => a * b).Sum();
            double magnitude1 = Math.Sqrt(vec1.Sum(v => v * v));
            double magnitude2 = Math.Sqrt(vec2.Sum(v => v * v));

            return magnitude1 == 0 || magnitude2 == 0 ? 0 : dotProduct / (magnitude1 * magnitude2);
        }
    }
}
