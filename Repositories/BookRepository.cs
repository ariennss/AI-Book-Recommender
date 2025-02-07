using BookRecommender.DBObjects;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WebApplication1;
using static System.Reflection.Metadata.BlobBuilder;

namespace BookRecommender.Repositories
{
    public class BookRepository : IBookRepository
    { 
        private readonly string ConnectionString = "Data Source=C:\\tesi\\bookRecommender.db;Version=3";
        private List<Book> books = new List<Book>();
        private readonly IReviewRepository _reviewRepository;

        public BookRepository(IReviewRepository reviewrepo)
        {
            _reviewRepository = reviewrepo;
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                var command = new SQLiteCommand("SELECT Books.*, Authors.full_name, GROUP_CONCAT(Book_Genres.genre) AS Genres\r\n         FROM Books\r\n         INNER JOIN Authors ON Books.author_id = Authors.author_id\r\n         INNER JOIN Book_Genres ON Books.book_id = Book_Genres.book_id\r\n \r\n        where Books.lcv = 0 GROUP BY Books.book_id", connection);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var urlSmallImage = reader.GetString(4);
                        var largeUrlImage = Regex.Replace(urlSmallImage, @"(?<=\d)m(?=\/\d+\.jpg$)", "l");

                        books.Add(new Book
                        {
                            Id = reader.GetInt32(0),
                            Title = reader.GetValue(1).ToString(),
                            AuthorId = reader.GetInt32(2),
                            Description = reader.GetString(3),
                            ImgUrl = largeUrlImage,
                            RatingsCount = reader.GetInt32(5),
                            AuthorName = reader.GetString(7),
                            Genres = reader.IsDBNull(8) ? new List<string>() : reader.GetString(8).Split(',').ToList()
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
            return books;
        }

        public Book GetBookById(int id)
        {
            return books.Where(x => x.Id == id).SingleOrDefault();
        }

        public List<Book> GetMostPopularBooks()
       {
            var reviews = _reviewRepository.GetAllReviews();
            var currentUserReviews = reviews.Where(x => x.UserId == CurrentUser.username).ToList();
            var currentUserBookIds = currentUserReviews.Select(r => r.BookId).ToHashSet();
            
            var bookPopularity = reviews
                 .Where(r => !currentUserBookIds.Contains(r.BookId))
                 .GroupBy(r => r.BookId)
                 .Select(group => new
                 {
                     BookId = group.Key,
                     TotalReviews = group.Count(),
                     AverageRating = group.Average(r => r.Rating),
                     PopularityScore = group.Count() * group.Average(r => r.Rating)
                 })
                 .OrderByDescending(b => b.PopularityScore)
                 .Take(5) // Take the top 5 books
                 .ToList();

            // Get the actual Book objects based on BookId
            var mostPopularBooks = bookPopularity
                .Join(books,
                      pop => pop.BookId,
                      book => book.Id,
                      (pop, book) => book)
                .ToList();

            return mostPopularBooks;
        }

        public List<Book> TopRatedBooks()
        {
            throw new NotImplementedException();
        }

        public List<Book> GetBooksByIds(IEnumerable<int> ids)
        {
            return books.Where(x => ids.Contains(x.Id)).ToList();
        }

        public List<Book> GetBooksForContentBased()
        {
            throw new NotImplementedException();
        }
    }
}
