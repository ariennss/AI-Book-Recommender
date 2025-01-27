using BookRecommender.DBObjects;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookRecommender.Repositories
{
    public class ReviewRepository : IReviewRepository
    {
        private readonly string ConnectionString = "Data Source=C:\\tesi\\bookRecommender.db;Version=3";
        private static List<Review> reviews = new List<Review>();

        public ReviewRepository()
        {
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                var command = new SQLiteCommand("SELECT * FROM Reviews where lcv = 0", connection);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        reviews.Add(new Review
                        {
                            Id = reader.GetInt32(0),
                            BookId = reader.GetInt32(1),
                            UserId = reader.GetString(2),
                            Rating = reader.GetInt32(3),
                        });
                    }
                }
            }

        }

        public void AddReview(Review review)
        {
            int bookid = review.BookId;
            string userid = review.UserId; //username
            int rating = review.Rating;
            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();
                string query = $"insert into Reviews(book_id, user_id, rating) VALUES({bookid}, '{userid}', {rating})";
                var command = new SQLiteCommand(query, conn);
                command.ExecuteNonQuery();
            }
            
            reviews.Add(review);
        }

        public List<Review> GetAllReviews()
        {
            return reviews;
        }

        public List<Review> GetUserReview(string username)
        {
            return reviews.Where(x => x.UserId == username).ToList();
        }
    }
}
