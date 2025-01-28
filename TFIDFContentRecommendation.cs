using BookRecommender.DBObjects;
using BookRecommender.Repositories;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace WebApplication1
{
    public class TFIDFContentRecommendation
    {
        private readonly IBookRepository _bookRepository;
        private static Dictionary<int, List<string>> descriptionVectors = new Dictionary<int, List<string>>();
        private static List<string> stopwords = new List<string>();

        public TFIDFContentRecommendation(IBookRepository bookrepo)
        {
            // Load stopwords
            string filePath = "C:\\tesi\\stopwordsKaggle.txt";
            stopwords = new List<string>(File.ReadAllLines(filePath).Select(word => word.ToLower()));

            // Load the book repository and preprocess book descriptions
            _bookRepository = bookrepo;
            var selectedPopularBooks = bookrepo.GetAllBooks().Where(x => x.RatingsCount > 1000).ToList();
            foreach (var selectedBook in selectedPopularBooks)
            {
                var wordsInDescription = PreprocessText(selectedBook.Description);
                descriptionVectors.Add(selectedBook.Id, wordsInDescription);
            }
        }

        public void FindTop10MostSimilarToDescription(string description)
        {
            // Preprocess the input description
            List<string> inputWords = PreprocessText(description);

            // Compute IDF values
            var idf = ComputeIDF();

            // Calculate TF-IDF vector for the input description
            var inputTfidf = CalculateTFIDF(inputWords, idf);

            // List to hold book IDs and their similarity scores
            var similarities = new List<(int BookId, double Similarity)>();

            // Calculate similarity for each book description
            foreach (var (bookId, words) in descriptionVectors)
            {
                var bookTfidf = CalculateTFIDF(words, idf);
                var similarity = CosineSimilarity(inputTfidf, bookTfidf);
                similarities.Add((bookId, similarity));
            }

            // Get the top 10 most similar books
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

        private List<string> PreprocessText(string text)
        {
            return text
                .ToLower() // Convert text to lowercase
                .Split(' ') // Split into words
                .Select(word => new string(word.Where(char.IsLetterOrDigit).ToArray())) // Keep only letters and digits
                .Where(cleanedWord => !string.IsNullOrEmpty(cleanedWord) && !stopwords.Contains(cleanedWord)) // Remove stopwords
                .ToList();
        }

        private Dictionary<string, double> CalculateTFIDF(List<string> words, Dictionary<string, double> idf)
        {
            // Sublinear scaling for term frequency
            var termFrequency = words
                .GroupBy(word => word)
                .ToDictionary(g => g.Key, g => 1 + Math.Log(g.Count()));

            // Calculate TF-IDF by combining TF and IDF
            var tfidf = termFrequency
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value * idf.GetValueOrDefault(kvp.Key, 0)); // Default to 0 if term not in IDF

            return tfidf;
        }

        private Dictionary<string, double> ComputeIDF()
        {
            int totalDocuments = descriptionVectors.Count();
            var termDocumentFrequency = new Dictionary<string, int>();

            // Count document frequency for each term
            foreach (var vector in descriptionVectors.Values)
            {
                foreach (var term in vector.Distinct()) // Count each term only once per document
                {
                    if (!termDocumentFrequency.ContainsKey(term))
                        termDocumentFrequency[term] = 0;

                    termDocumentFrequency[term]++;
                }
            }

            // Compute IDF for each term
            return termDocumentFrequency
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => Math.Log(1 + (totalDocuments / (1.0 + kvp.Value))) // Smoothing to prevent division by zero
                );
        }

        private double CosineSimilarity(Dictionary<string, double> vec1, Dictionary<string, double> vec2)
        {
            // Get the intersecting terms
            var intersect = vec1.Keys.Intersect(vec2.Keys);

            // Calculate the dot product
            var dotProduct = intersect.Sum(key => vec1[key] * vec2[key]);

            // Add a boost for terms appearing in both query and document
            const double WEIGHT = 0.5;
            dotProduct += intersect.Count() * WEIGHT;

            // Calculate vector magnitudes
            var magnitude1 = Math.Sqrt(vec1.Values.Sum(val => val * val));
            var magnitude2 = Math.Sqrt(vec2.Values.Sum(val => val * val));

            // Compute cosine similarity
            return magnitude1 == 0 || magnitude2 == 0 ? 0 : dotProduct / (magnitude1 * magnitude2);
        }
    }
}
