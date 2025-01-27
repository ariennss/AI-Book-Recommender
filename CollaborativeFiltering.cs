using BookRecommender.DBObjects;
using BookRecommender.Repositories;

namespace WebApplication1
{
    public class CollaborativeFiltering : ICollaborativeFiltering
    {
        private readonly IBookRepository _bookRepository;
        private readonly IReviewRepository _reviewRepository;

        public CollaborativeFiltering(IBookRepository bookrepo, IReviewRepository reviewrepo)
        {
            _bookRepository = bookrepo;
            _reviewRepository = reviewrepo;
        }

        public List<Book> SuggestionsFor(string username)
        {
            var reviews = _reviewRepository.GetAllReviews();
            var b = _bookRepository.GetAllBooks();
            // Current user reviews and book IDs
            var currentUserReviews = reviews.Where(x => x.UserId == username).ToList();
            var currentUserBookIds = currentUserReviews.Select(r => r.BookId).ToHashSet();

            // Group reviews by user (excluding current user)
            var userGroups = reviews
                .Where(r => r.UserId != username)
                .GroupBy(r => r.UserId);

            // Calculate cosine similarity for each user
            var userSimilarities = new Dictionary<string, double>();
            foreach (var group in userGroups)
            {
                var otherUserId = group.Key;
                var otherUserReviews = group.ToList();

                // Reviews in common
                var commonReviews = otherUserReviews
                    .Where(r => currentUserBookIds.Contains(r.BookId))
                    .ToList();

                if (commonReviews.Count < 3)
                {
                    continue; // Skip users with fewer than 3 books in common
                }

                // Build rating vectors for cosine similarity
                var currentUserRatings = new List<double>();
                var otherUserRatings = new List<double>();

                foreach (var review in commonReviews)
                {
                    var currentUserRating = currentUserReviews
                        .First(r => r.BookId == review.BookId).Rating;
                    currentUserRatings.Add(currentUserRating);
                    otherUserRatings.Add(review.Rating);
                }

                // Calculate cosine similarity
                var dotProduct = currentUserRatings.Zip(otherUserRatings, (a, b) => a * b).Sum();
                var magnitudeCurrentUser = Math.Sqrt(currentUserRatings.Sum(r => r * r));
                var magnitudeOtherUser = Math.Sqrt(otherUserRatings.Sum(r => r * r));
                var cosineSimilarity = dotProduct / (magnitudeCurrentUser * magnitudeOtherUser);

                userSimilarities[otherUserId] = cosineSimilarity;
            }

            // Sort users by similarity (descending order)
            var sortedSimilarUsers = userSimilarities
                .OrderByDescending(pair => pair.Value)
                .Select(pair => pair.Key)
                .ToList();

            var numberOfSuggestions = 5;
            var suggestedBookIds = new HashSet<int>();
            foreach (var similarUserId in sortedSimilarUsers)
            {
                if (suggestedBookIds.Count >= numberOfSuggestions)
                {
                    break; // Stop if we have enough suggestions
                }

                var similarUserReviews = reviews
                    .Where(r => r.UserId == similarUserId && r.Rating >= 4) // Highly-rated books
                    .Where(r => !currentUserBookIds.Contains(r.BookId)) // Books not rated by the current user
                    .Select(r => r.BookId)
                    .ToList();

                foreach (var bookId in similarUserReviews)
                {
                    if (suggestedBookIds.Count >= numberOfSuggestions)
                    {
                        break;
                    }

                    suggestedBookIds.Add(bookId);
                }

                
            }

            var suggestedBooks = _bookRepository.GetBooksByIds(suggestedBookIds);
            var booksWithRatings = suggestedBooks
        .Select(book => new
        {
            Book = book,
            AverageRating = reviews
                .Where(r => r.BookId == book.Id)
                .Average(r => r.Rating),
            RatingsCount = reviews
                .Count(r => r.BookId == book.Id)
        })
        .OrderByDescending(x => x.AverageRating) // First order by average rating
        .ThenByDescending(x => x.RatingsCount)  // Then order by ratings count
        .Take(5) // Limit to top 5 books
        .Select(x => x.Book)
        .ToList();
            return suggestedBooks.OrderByDescending(x => x.RatingsCount).Take(5).ToList();


        }
    }
}
