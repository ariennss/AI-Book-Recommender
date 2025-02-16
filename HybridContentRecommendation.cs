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

        /// <summary>
        /// Finds the top 10 books using both TF-IDF and Word2Vec similarity.
        /// </summary>
        public async Task<List<Book>> FindTop10MostSimilarToDescriptionAsync(string description)
        {
            if (string.IsNullOrWhiteSpace(description))
            {
                Console.WriteLine("Error: Empty input.");
                return null;
            }
            // 1️⃣ Preprocess and lemmatize all book descriptions for TF-IDF
            await PreprocessAndLemmatizeDescriptionsAsync();

            // 2️⃣ Preprocess and lemmatize the input phrase
            var lemmatizedInput = await LemmatizeText(description);
            if (lemmatizedInput == null || !lemmatizedInput.Any())
            {
                Console.WriteLine("Error: Unable to process input description.");
                return null;
            }

            // 3️⃣ Get Word2Vec embedding for the input phrase
            var inputEmbedding = await GetWord2VecEmbedding(description);
            if (inputEmbedding == null || inputEmbedding.Count == 0)
            {
                Console.WriteLine("Error: Could not generate Word2Vec embedding.");
                return null;
            }

            // 4️⃣ Compute TF-IDF Similarity
            var idf = ComputeIDF();
            var inputTfidf = CalculateTFIDF(lemmatizedInput, idf);
            var tfidfSimilarities = ComputeTFIDFSimilarity(inputTfidf);

            // 5️⃣ Compute Word2Vec Similarity
           var word2vecSimilarities = await ComputeWord2VecSimilarities(inputEmbedding);

            //var collaborativeScores = new Dictionary<int, double>();
            //var similarUsers = _collaborativeFiltering.GetMostSimilarUsers("ariennss");
            //foreach (var user in similarUsers)
            //{
            //    var userId = user.Key;
            //    var similarityScore = user.Value;  // This is the cosine similarity score

            //    var userRatings = _reviewRepository.GetUserReview(userId);  // Get this user's ratings

            //    foreach (var rating in userRatings)
            //    {
            //        if (!collaborativeScores.ContainsKey(rating.BookId))
            //        {
            //            collaborativeScores[rating.BookId] = 0;
            //        }

            //        // Weighted Score: Rating * Similarity Score
            //        collaborativeScores[rating.BookId] += rating.Rating * similarityScore;
            //    }
            //}

            //// 4️⃣ Normalize Scores
            //if (collaborativeScores.Count > 0 && collaborativeScores.Values.Max() > 0)
            //{
            //    double maxScore = collaborativeScores.Values.Max();
            //    foreach (var bookId in collaborativeScores.Keys.ToList())
            //    {
            //        collaborativeScores[bookId] /= maxScore; // Normalize between 0 and 1
            //    }
            //}

            var combinedScores = new Dictionary<int, double>();

            foreach (var bookId in tfidfSimilarities.Keys)
            {
                double tfidfScore = tfidfSimilarities.GetValueOrDefault(bookId, 0);
                double w2vScore = word2vecSimilarities.GetValueOrDefault(bookId, 0);
                //double collaborativeScore = collaborativeScores.GetValueOrDefault(bookId, 0);

                // Weighted Combination (Adjust Weights as Needed)
                double finalScore = (tfidfScore * 0.5) + (w2vScore * 0.5); /*+ (collaborativeScore * 0);*/
                combinedScores[bookId] = finalScore;
            }



            // UNCOMMENT IF NEEDED COMBINED SCORES THAT DO NOT CONSIDER COLLABORATIVE FILTERING!
            //var combinedScores = MergeSimilarityScores(tfidfSimilarities, word2vecSimilarities);

            // 7️⃣ Retrieve & Sort Books by Combined Score
            var username = _contextAccessor.HttpContext?.User?.Identity?.Name;
            var myReviews = _reviewRepository.GetUserReview(username);
            var myBookIds = (_bookRepository.GetBooksByIds(myReviews.Select(x => x.BookId))).Select(x => x.Id).ToList();

            var topBooks = combinedScores
               .OrderByDescending(pair => pair.Value)
               .Take(20) // First sort by similarity
               .Select(pair => _bookRepository.GetBookById(pair.Key))
               .OrderByDescending(x => x.RatingsCount) // Then sort by popularity
               .Where(x => !myBookIds.Contains(x.Id))
               .Take(10)
               .ToList();


            return topBooks;
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

        /// <summary>
        /// Preprocesses and lemmatizes all book descriptions for TF-IDF.
        /// </summary>
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
            var idf = ComputeIDF();  // Compute IDF once and store it as float values

            // Get all unique terms in the vocabulary for proper alignment
            var allTerms = new HashSet<string>(inputTfidf.Keys);

            foreach (var (bookId, words) in descriptionVectors)
            {
                var bookTfidf = CalculateTFIDF(words, idf);

                // Ensure both input and book vectors are properly aligned
                var inputVector = allTerms.Select(term => inputTfidf.GetValueOrDefault(term, 0f)).ToList();
                var bookVector = allTerms.Select(term => bookTfidf.GetValueOrDefault(term, 0f)).ToList();

                float similarity = CosineSimilarity(inputVector, bookVector);
                similarities[bookId] = similarity;
            }

            return similarities;
        }



        private Dictionary<int, float> MergeSimilarityScores(Dictionary<int, float> tfidfScores, Dictionary<int, float> w2vScores)
        {
            return tfidfScores.Keys
                .Union(w2vScores.Keys)
                .ToDictionary(bookId => bookId, bookId =>
                    (tfidfScores.GetValueOrDefault(bookId, 0) * 0.5f) +
                    (w2vScores.GetValueOrDefault(bookId, 0) * 0.5f));
        }

        private Dictionary<string, float> ComputeIDF()
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


        private Dictionary<string, float> CalculateTFIDF(List<string> words, Dictionary<string, float> idf)
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
