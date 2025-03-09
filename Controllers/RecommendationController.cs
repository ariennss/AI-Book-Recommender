using BookRecommender.DBObjects;
using BookRecommender.Repositories;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.ViewModels;

namespace WebApplication1.Controllers
{
    public class RecommendationController : Controller
    {
        private readonly IReviewRepository _reviewRepository;
        private readonly IBookRepository _bookRepository;
        private readonly ITagsSimilarity _tagSimilarity;

        public RecommendationController(IReviewRepository rr, IBookRepository br, ITagsSimilarity ts)
        {
            _reviewRepository = rr;
            _bookRepository = br;
            _tagSimilarity = ts;
        }

        [HttpGet]
        public IActionResult BookByTag()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }
            else
            {
                return View(new BookByTagModel
                {
                    Query = "",
                    Recommendations = new List<Book>()
                }); //Modell vuoto quando voglio caricare la pagina al primo giro, senza che ancora ho richiesto nulla.
            }

        }


        [HttpPost]
        public IActionResult GetBooksByTags(int bookId)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }
            var username = User.Identity.Name;
            var myReviews = _reviewRepository.GetUserReview(username);
            var myBooks = _bookRepository.GetBooksByIds(myReviews.Select(x => x.BookId));
            var bookRatingDict = new Dictionary<Book, int>();
            foreach (var book in myBooks)
            {
                bookRatingDict.Add(book, myReviews.Where(x => x.BookId == book.Id).Select(y => y.Rating).FirstOrDefault());
            }

            var startingBook = _bookRepository.GetBookById(bookId);
            var recommendations = _tagSimilarity.GetSimilarBooks(startingBook.Id);
            var model = new BookByTagModel
            {
                ChosenOne = startingBook,
                Recommendations = recommendations,
                AlreadyRatedBooks = bookRatingDict
            }; //Modello valorizzato con i suggerimenti.
            return View("BookByTag", model);
        }

        [HttpPost]
        public IActionResult ShowResults(string title)
        {
            var books = _bookRepository.GetAllBooks();
            var results = books.Where(x => x.Title.ToLower().Contains(title.ToLower())).ToList();
            var model = new BookByTagModel { SearchedBooks = results };
            return View("BookByTag", model);
        }
    }
}
