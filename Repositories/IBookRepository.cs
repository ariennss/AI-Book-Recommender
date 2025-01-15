using BookRecommender.DBObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookRecommender.Repositories
{
    public interface IBookRepository
    {
        List<Book> GetAllBooks();
        Book GetBookById(int id);
        List<Book> GetBooksByIds(IEnumerable<int> ids);
        void AddBook(Book book);

        List<Book> GetMostPopularBooks();
        List<Book> TopRatedBooks();
    }
}
