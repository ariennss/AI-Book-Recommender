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

        public async Task AddReviewAsync(Review review)
        {
            int bookid = review.BookId;
            string userid = review.UserId; // Username perché usato come chiave primaria
            int rating = review.Rating;

            using (var conn = new SQLiteConnection(ConnectionString))
            {
                await conn.OpenAsync(); 

                string query = $"INSERT INTO Reviews(book_id, user_id, rating) VALUES(@bookid, @userid, @rating)";

                using (var command = new SQLiteCommand(query, conn))
                {
                  
                    command.Parameters.AddWithValue("@bookid", bookid);
                    command.Parameters.AddWithValue("@userid", userid);
                    command.Parameters.AddWithValue("@rating", rating);

                    await command.ExecuteNonQueryAsync(); 
                }
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
