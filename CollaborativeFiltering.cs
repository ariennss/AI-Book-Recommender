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
            var userSimilarities = GetMostSimilarUsers(username);
            var b = _bookRepository.GetAllBooks();
            var currentUserReviews = reviews.Where(x => x.UserId == username).ToList();
            var currentUserBookIds = currentUserReviews.Select(r => r.BookId).ToHashSet();


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


        public Dictionary<string, double> GetMostSimilarUsers(string username)
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
            var userSimilarities = userGroups
                .Select(group =>
                {
                    var otherUserId = group.Key;
                    var otherUserReviews = group.ToList();

                    // Find common reviews (books both users rated)
                    var commonReviews = otherUserReviews
                        .Where(r => currentUserBookIds.Contains(r.BookId))
                        .ToList();

                    if (commonReviews.Count < 3)
                        return null; // Skip users with fewer than 3 books in common

                    // Build rating vectors
                    var ratingPairs = commonReviews
                        .Select(review => new
                        {
                            CurrentUserRating = currentUserReviews.First(r => r.BookId == review.BookId).Rating,
                            OtherUserRating = review.Rating
                        })
                        .ToList();

                    // Calculate cosine similarity
                    var dotProduct = ratingPairs.Sum(pair => pair.CurrentUserRating * pair.OtherUserRating);
                    var magnitudeCurrentUser = Math.Sqrt(ratingPairs.Sum(pair => pair.CurrentUserRating * pair.CurrentUserRating));
                    var magnitudeOtherUser = Math.Sqrt(ratingPairs.Sum(pair => pair.OtherUserRating * pair.OtherUserRating));

                    var cosineSimilarity = (magnitudeCurrentUser == 0 || magnitudeOtherUser == 0) ? 0 : dotProduct / (magnitudeCurrentUser * magnitudeOtherUser);

                    return new { otherUserId, cosineSimilarity };
                })
                .Where(result => result != null) // Filter out nulls (users with < 3 common reviews)
                .ToDictionary(result => result.otherUserId, result => result.cosineSimilarity);

            return userSimilarities;

            //// Sort users by similarity (descending order)
            //var sortedSimilarUsers = userSimilarities
            //    .OrderByDescending(pair => pair.Value)
            //    .Select(pair => pair.Key)
            //    .ToList();
        }
    }
}
