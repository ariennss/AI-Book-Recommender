using BookRecommender.DBObjects;

namespace BookRecommender.Repositories
{
    public interface IReviewRepository
    {
        Task AddReviewAsync(Review review);
        List<Review> GetAllReviews();
        List<Review> GetUserReview(string username);

    }
}