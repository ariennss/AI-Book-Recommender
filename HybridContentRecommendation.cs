using BookRecommender.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using System.Data.SQLite;
using WebApplication1.Repositories;
using BookRecommender.DBObjects;

namespace WebApplication1
{
   
    public class HybridContentRecommendation : IHybridContentRecommendation
    {
        private readonly IBookRepository _bookRepository;
        private static Dictionary<int, List<string>> descriptionVectors = new();
        private readonly HttpClient _httpClient;
        private readonly string _dbPath = "Data Source=C:\\tesi\\bookRecommender.db;Version=3";
        private readonly ICollaborativeFiltering _collaborativeFiltering;
        private readonly IReviewRepository _reviewRepository;
        private readonly IHttpContextAccessor _contextAccessor;

        public HybridContentRecommendation(IBookRepository bookrepo, ICollaborativeFiltering collaborativeFiltering, IReviewRepository rewrepo, IHttpContextAccessor ca)
        {
            _httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:5000/") };
            _bookRepository = bookrepo;
            _collaborativeFiltering = collaborativeFiltering;
            _reviewRepository = rewrepo;
            _contextAccessor = ca;
        }

        public async Task<List<Book>> FindTop10MostSimilarToDescriptionAsync(string description)
        {
            // processamento del testo

            if (string.IsNullOrWhiteSpace(description))
            {
                Console.WriteLine("Error: Empty input.");
                return null;
            }

            await PreprocessAndLemmatizeDescriptionsAsync();


            var lemmatizedInput = await LemmatizeText(description);
            if (lemmatizedInput == null || !lemmatizedInput.Any())
            {
                Console.WriteLine("Error: Unable to process input description.");
                return null;
            }

            // chiamata API per embedding frase in input
            var inputEmbedding = await GetWord2VecEmbedding(description);
            if (inputEmbedding == null || inputEmbedding.Count == 0)
            {
                Console.WriteLine("Error: Could not generate Word2Vec embedding.");
                return null;
            }

            // tf-idf frase in input
            var idf = CalculateIDF();
            var inputTfidf = CalculateTF(lemmatizedInput, idf);
            var tfidfSimilarities = ComputeTFIDFSimilarity(inputTfidf);

           var word2vecSimilarities = await ComputeWord2VecSimilarities(inputEmbedding);

            var combinedScores = new Dictionary<int, double>();

            foreach (var bookId in tfidfSimilarities.Keys)
            {
                double tfidfScore = tfidfSimilarities.GetValueOrDefault(bookId, 0);
                double w2vScore = word2vecSimilarities.GetValueOrDefault(bookId, 0);
                //double collaborativeScore = collaborativeScores.GetValueOrDefault(bookId, 0); // decommentare se voglio anche il peso per collaborative filtering

                // Testare quale combinazione di peso funziona meglio
                double finalScore = (tfidfScore * 0.5) + (w2vScore * 0.5); /*+ (collaborativeScore * 0);*/ // se voglio pesare anche per collaborative filtering
                combinedScores[bookId] = finalScore;
            }

            // eliminare libri che ho già letto.
            var username = _contextAccessor.HttpContext?.User?.Identity?.Name;
            var myReviews = _reviewRepository.GetUserReview(username);
            var myBookIds = (_bookRepository.GetBooksByIds(myReviews.Select(x => x.BookId))).Select(x => x.Id).ToList();

            var topBooks = combinedScores
               .OrderByDescending(pair => pair.Value)
               .Take(20) // Sort per somiglianza
               .Select(pair => _bookRepository.GetBookById(pair.Key))
               .OrderByDescending(x => x.RatingsCount) // Poi sort per quante recensioni ha 
               .Where(x => !myBookIds.Contains(x.Id))
               .Take(10)
               .ToList();


            return topBooks;

            // test
            Console.WriteLine("Top 10 most similar books:");
            foreach (var book in topBooks)
            {
                Console.WriteLine($"- {book.Title}");
            }
        }


        private async Task<List<float>> GetWord2VecEmbedding(string text)
        {
            try
            {
                var payload = new StringContent($"{{\"text\":\"{text}\"}}", Encoding.UTF8, "application/json");
                Console.WriteLine($"Payload being sent: {payload.ReadAsStringAsync().Result}");
                var response = await _httpClient.PostAsync("word2vec_embed", payload);
                var resulttest = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Response Status Code: {response.StatusCode}");
                Console.WriteLine($"Response Content: {resulttest}");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<float>>(result) ?? new List<float>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during embedding: {ex.Message}");
            }
            return new List<float>();
        }

        private async Task PreprocessAndLemmatizeDescriptionsAsync()
        {
            var allBooks = _bookRepository.GetAllBooks();
            var descriptionsPayload = new { descriptions = allBooks.Select(book => new { id = book.Id, text = book.Description }).ToList() };

            try
            {
                var response = await _httpClient.PostAsync("batch_lemmatize",
                    new StringContent(JsonSerializer.Serialize(descriptionsPayload), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    descriptionVectors = JsonSerializer.Deserialize<Dictionary<int, List<string>>>(responseContent) ?? new();
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

        private async Task<Dictionary<int, float>> ComputeWord2VecSimilarities(List<float> inputVector)
        {
            var word2vecSimilarities = new Dictionary<int, float>();

            using var conn = new SQLiteConnection(_dbPath);
            await conn.OpenAsync();
            var command = conn.CreateCommand();
            command.CommandText = "SELECT book_id, embedding FROM BookEmbeddings";

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                int bookId = reader.GetInt32(0);
                var embeddingList = reader.GetString(1).Trim('[', ']').Split(',').Select(float.Parse).ToList();
                float similarity = CosineSimilarity(inputVector, embeddingList);
                word2vecSimilarities[bookId] = similarity;
            }
            return word2vecSimilarities;
        }

        private Dictionary<int, float> ComputeTFIDFSimilarity(Dictionary<string, float> inputTfidf)
        {
            var similarities = new Dictionary<int, float>();
            var idf = CalculateIDF(); 

           
            var allTerms = new HashSet<string>(inputTfidf.Keys);

            foreach (var (bookId, words) in descriptionVectors)
            {
                var bookTfidf = CalculateTF(words, idf);

                
                var inputVector = allTerms.Select(term => inputTfidf.GetValueOrDefault(term, 0f)).ToList();
                var bookVector = allTerms.Select(term => bookTfidf.GetValueOrDefault(term, 0f)).ToList();

                float similarity = CosineSimilarity(inputVector, bookVector);
                similarities[bookId] = similarity;
            }

            return similarities;
        }


        private Dictionary<string, float> CalculateIDF()
        {
            var totalDocuments = (float)descriptionVectors.Count;
            var termDocumentFrequency = new Dictionary<string, int>();

            foreach (var vector in descriptionVectors.Values)
            {
                foreach (var term in vector.Distinct())
                {
                    termDocumentFrequency[term] = termDocumentFrequency.GetValueOrDefault(term, 0) + 1;
                }
            }

            return termDocumentFrequency
                .ToDictionary(kvp => kvp.Key, kvp => (float)Math.Log(totalDocuments / (1 + kvp.Value)));
        }


        private Dictionary<string, float> CalculateTF(List<string> words, Dictionary<string, float> idf)
        {
            var termFrequency = words
                .GroupBy(w => w)
                .ToDictionary(g => g.Key, g => (float)g.Count() / words.Count);

            return termFrequency
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value * idf.GetValueOrDefault(kvp.Key, 0f));
        }


        private float CosineSimilarity(List<float> vec1, List<float> vec2)
        {
            if (vec1.Count != vec2.Count) return 0f;

            float dotProduct = vec1.Zip(vec2, (a, b) => a * b).Sum();
            float magnitude1 = (float)Math.Sqrt(vec1.Sum(v => v * v));
            float magnitude2 = (float)Math.Sqrt(vec2.Sum(v => v * v));

            return magnitude1 == 0 || magnitude2 == 0 ? 0f : dotProduct / (magnitude1 * magnitude2);
        }

    }
}
