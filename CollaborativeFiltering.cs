using BookRecommender.Repositories;

namespace WebApplication1
{
    public class CollaborativeFiltering
    {
        private readonly IBookRepository _bookRepository;
        private readonly IReviewRepository _reviewRepository;

        public CollaborativeFiltering(IBookRepository bookrepo, IReviewRepository reviewrepo)
        {
            _bookRepository = bookrepo;
            _reviewRepository = reviewrepo;
        }

        public void UserWithAtLeastThreeBooksInCommonAndMore(string username)
        {
            var reviews = _reviewRepository.GetAllReviews();

            //var usersWithReviewsInCommonAndMore = reviews.
        }
    }
}
