using BookRecommender.Repositories;

namespace WebApplication1
{
    public class Recommendations
    {
        private readonly IBookRepository _bookRepository;
        private readonly IReviewRepository _reviewRepository;

        public Recommendations(IBookRepository bookrepo, IReviewRepository reviewrepo)
        {
            _bookRepository = bookrepo;
            _reviewRepository = reviewrepo;
        }

        public void TopRatedBooks()
        {
            var reviews = _reviewRepository.GetAllReviews();
            var topRatedBooks = reviews
                .GroupBy(r => r.BookId)
                .Select(g => new
                {
                    BookId = g.Key,
                    AverageRating = g.Average(r => r.Rating),
                    NumberOfReviews = g.Count()
                })
                .Where(g => g.AverageRating > 4.5)
                .OrderByDescending(g => g.NumberOfReviews)
                .Take(5)
                .ToList();

            var topRatedBookIds = topRatedBooks.Select(x => x.BookId).ToList();
            var mostPopularBooks = _bookRepository.GetBooksByIds(topRatedBookIds);
        }

        public void CollaborativeFiltering()
        {
            var currentUserReviews = _reviewRepository.GetUserReview(CurrentUser.username);
        }
            
    }
}
