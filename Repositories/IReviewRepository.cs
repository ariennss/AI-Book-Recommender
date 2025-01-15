using BookRecommender.DBObjects;

namespace BookRecommender.Repositories
{
    public interface IReviewRepository
    {
        void AddReview(Review review);
        List<Review> GetAllReviews();
        List<Review> GetUserReview(string username);

    }
}