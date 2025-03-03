using BookRecommender.DBObjects;
using BookRecommender.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WebApplication1
{
    public class TagsSimilarity : ITagsSimilarity
    {
        private Dictionary<int, List<string>> bookTags = new(); // coppia book id - tag del libro
        private Dictionary<string, float> idfCache = new(); // Per ogni tag calcola il suo IDF cioè quanto è importante considerando gli altri book
        private readonly IBookRepository _bookRepository;

        public TagsSimilarity(IBookRepository br)
        {
            _bookRepository = br;
            var books = _bookRepository.GetAllBooks();
            bookTags.Clear();
            foreach (var book in books)
            {
                bookTags[book.Id] = book.Tags ?? new List<string>();
            }
        }

        public List<Book> GetSimilarBooks(int inputBookId, int topN = 10)
        {
            ComputeIDF();

            if (!bookTags.ContainsKey(inputBookId) || bookTags[inputBookId].Count == 0)
            {
                Console.WriteLine($"Book ID {inputBookId} not found or has no tags.");
                return new List<Book>();
            }

            var inputTags = bookTags[inputBookId];
            var inputTfidf = CalculateTFIDF(inputTags);

            var similarities = new Dictionary<int, float>();

            foreach (var (bookId, tags) in bookTags)
            {
                if (bookId == inputBookId || tags.Count == 0)
                {
                    continue;
                }

                Dictionary<string, float> bookTfidf = CalculateTFIDF(tags);
                float similarity = CosineSimilarity(inputTfidf, bookTfidf);

                similarities[bookId] = similarity;
            }

            // ordino per trovare i più simili sulla base del risultato del calcolo della cosine similarity
            var similarBookIds = similarities
                .OrderByDescending(x => x.Value)
                .Take(topN)
                .Select(pair => pair.Key)
                .ToList();

            return _bookRepository.GetBooksByIds(similarBookIds);
        }

        private float CosineSimilarity(Dictionary<string, float> vec1, Dictionary<string, float> vec2)
        {
            float dotProduct = 0, normA = 0, normB = 0;

            foreach (var key in vec1.Keys)
            {
                if (vec2.ContainsKey(key))
                    dotProduct += vec1[key] * vec2[key];
                normA += vec1[key] * vec1[key];
            }

            foreach (var value in vec2.Values)
                normB += value * value;

            return (normA == 0 || normB == 0) ? 0 : dotProduct / (float)(Math.Sqrt(normA) * Math.Sqrt(normB));
        }

        private Dictionary<string, float> CalculateTFIDF(List<string> tags)
        {
            var tf = tags.GroupBy(w => w).ToDictionary(g => g.Key, g => (float)g.Count() / tags.Count);
            return tf.ToDictionary(kvp => kvp.Key, kvp => kvp.Value * idfCache.GetValueOrDefault(kvp.Key, 0f));
        }

        private void ComputeIDF()
        {
            int totalBooks = bookTags.Count;
            var termDocumentFrequency = new Dictionary<string, int>();

            foreach (var tags in bookTags.Values)
            {
                foreach (var tag in tags.Distinct())
                {
                    if (!termDocumentFrequency.ContainsKey(tag))
                        termDocumentFrequency[tag] = 0;
                    termDocumentFrequency[tag]++;
                }
            }

            idfCache.Clear(); 

            foreach (var (tag, docCount) in termDocumentFrequency)
            {
                idfCache[tag] = (float)Math.Log((float)totalBooks / (1 + docCount));
            }
        }

        public Dictionary<int, float> GetTagSimilarity(List<string> lemmatizedInput)
        {
            ComputeIDF();

            
            var inputTfidf = CalculateTFIDF(lemmatizedInput);

            var similarities = new Dictionary<int, float>();

            foreach (var (bookId, tags) in bookTags)
            {
                if (tags.Count == 0)
                {
                    continue;
                }

                Dictionary<string, float> bookTfidf = CalculateTFIDF(tags);
                float similarity = CosineSimilarity(inputTfidf, bookTfidf);

                similarities[bookId] = similarity;
            }

            return similarities;
        }
    }
}
