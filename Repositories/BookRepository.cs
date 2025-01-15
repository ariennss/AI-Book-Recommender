using BookRecommender.DBObjects;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookRecommender.Repositories
{
    public class BookRepository : IBookRepository
    { 
        private readonly string ConnectionString = "Data Source=C:\\tesi\\bookRecommender.db;Version=3";
        private static List<Book> books = new List<Book>();
        private readonly IReviewRepository _reviewRepository;

        public BookRepository(IReviewRepository reviewrepo)
        {
            _reviewRepository = reviewrepo;
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                var command = new SQLiteCommand("SELECT * FROM Books", connection);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        
                        books.Add(new Book
                        {
                            Id = reader.GetInt32(0),
                            Title = reader.GetValue(1).ToString(),
                            AuthorId = reader.GetInt32(2),
                            Description = reader.GetString(3),
                            ImgUrl = reader.GetString(4),
                        });
                    }
                }
            }

        }
    
        public void AddBook(Book book)
        {
            throw new NotImplementedException();
        }

        public List<Book> GetAllBooks()
        {
            throw new NotImplementedException();
        }

        public Book GetBookById(int id)
        {
            throw new NotImplementedException();
        }

        public List<Book> GetMostPopularBooks()
        {
            var x =  _reviewRepository.GetAllReviews();
            var a = x.Where(y => y.Rating > 1 && y.Rating < 3).ToList();
            return null;
        }

        public List<Book> TopRatedBooks()
        {
            throw new NotImplementedException();
        }

        public List<Book> GetBooksByIds(IEnumerable<int> ids)
        {
            return books.Where(x => ids.Contains(x.Id)).ToList();
        }
    }
}
