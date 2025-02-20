using BookRecommender.DBObjects;
using BookRecommender.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using WebApplication1.Models;
using WebApplication1.ViewModels;
using Microsoft.AspNetCore.Http;

namespace WebApplication1.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IBookRepository _bookRepository;
        private readonly IReviewRepository _reviewRepository;
        private readonly ICollaborativeFiltering _collaborativeFiltering;
        private readonly IHybridContentRecommendation _contentRecommender;
        private readonly ITagsSimilarity _tagSimilarity;

        public HomeController(ILogger<HomeController> logger, IBookRepository br, IReviewRepository rr, ICollaborativeFiltering collaborativeFiltering, IHybridContentRecommendation contentrec, ITagsSimilarity ts)
        {
            _logger = logger;
            _bookRepository = br;
            _reviewRepository = rr;
            _collaborativeFiltering = collaborativeFiltering;
            _contentRecommender = contentrec;
            _tagSimilarity = ts;
        }

        public IActionResult HomePage()
        {
            return View("Homepage");
        }

        public async Task<IActionResult> Index()
        {
            var books = _bookRepository.GetAllBooks();
            if (User.Identity.IsAuthenticated)
            {
                var mostPopularBooks = _bookRepository.GetMostPopularBooks();
                var username = User.Identity.Name;
                int minimumRatings = 3;
                int userRatingsCount = _reviewRepository.GetUserReview(username).Count();
                if (userRatingsCount >= minimumRatings)
                {
                    var myReviews = _reviewRepository.GetUserReview(username);
                    var myBooks = _bookRepository.GetBooksByIds(myReviews.Select(x => x.BookId));
                    Random random = new Random();
                    var randomBook = myBooks.ElementAt(random.Next(myBooks.Count()));

                    //QUESTO COMMENTATO E FATTO CON SIMILARITA TRA LE TRAME! 
                    //var similarToRandom = await _contentRecommender.FindTop10MostSimilarToDescriptionAsync(randomBook.Description);

                    //questo invece con il TF IDF dei Tag. 
                    var similarToRandom = _tagSimilarity.GetSimilarBooks(randomBook.Id);
                    var suggestedBooks = _collaborativeFiltering.SuggestionsFor(username);
                   

                    var viewModel = new HomeViewModel
                    {
                        MostPopularBooks = mostPopularBooks,
                        SuggestedBooks = suggestedBooks,
                        SimilarToRandom = similarToRandom,
                        RandomBook = randomBook,
                       
                    };

                    return View("Recommendations", viewModel);
                }
                else
                {
                    return View("ColdUser", new HomeViewModel { MostPopularBooks = mostPopularBooks });
                }
            }
            else
            {
                return RedirectToAction("Login", "Account");
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpGet]
        public IActionResult AISuggestions()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }
            else 
            {
                return View(new AISuggestionsModel
                {
                    Query = "",
                    Recommendations = new List<Book>()
                }); //Modell vuoto quando voglio caricare la pagina al primo giro, senza che ancora ho richiesto nulla.
            }
                
        }


        [HttpPost]
        public IActionResult GetAISuggestions(string query)
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

            var recommendations = _contentRecommender.FindTop10MostSimilarToDescriptionAsync(query).Result;
            var model = new AISuggestionsModel
            {
                Query = query,
                Recommendations = recommendations,
                AlreadyRatedBooks = bookRatingDict
            }; //Modello valorizzato con i suggerimenti.
            return View("AISuggestions", model);
        }

        public IActionResult GoToReviewPage()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }
            else
            {
                return View("RatingPage", new RatingsModel
                {
                    BookTitle = "",
                    AuthorName = "",
                    MatchingBooks = new List<Book>(), 
                    AlreadyRatedBooks = new Dictionary<Book, int>(),
                });
            }
        }
        public IActionResult GetMatchingBook(string bookTitle, string bookAuthor)
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

            if (bookTitle != null)
            { 
                var books = _bookRepository.GetAllBooks();
                var matchingBooks = books.Where(x => x.Title.ToLower().Contains(bookTitle.ToLower())).ToList();
                

                return View("RatingPage", new RatingsModel
                {
                    BookTitle = bookTitle,
                    AuthorName = "",
                    MatchingBooks = matchingBooks,
                    AlreadyRatedBooks = bookRatingDict,
                });
            }
            else if (bookAuthor != null) {
                var books = _bookRepository.GetAllBooks();
                var matchingBooks = books.Where(x => x.AuthorName.ToLower().Contains(bookAuthor.ToLower())).ToList();
                return View("RatingPage", new RatingsModel
                {
                    BookTitle = "",
                    AuthorName = bookAuthor,
                    MatchingBooks = matchingBooks,
                    AlreadyRatedBooks = bookRatingDict,
                });
            }
            else
            {
                return View("RatingPage", new RatingsModel
                {
                    BookTitle = "",
                    AuthorName = "",
                    MatchingBooks = new List<Book>(),
                    AlreadyRatedBooks = bookRatingDict,
                });
            }
        }

        public IActionResult GoToProfile()
        {
            var username = User.Identity.Name;
            var userRatings = _reviewRepository.GetUserReview(username);
            var bookRatingCouple = new Dictionary<int, int>();
            foreach (var item in userRatings)
            {
                bookRatingCouple.Add(item.BookId, item.Rating);
            }
            var usersBooks = _bookRepository.GetBooksByIds(userRatings.Select(x => x.BookId));
            var model = new ProfileModel
            {
                currentUserName = username,
                CurrentUserRatings = usersBooks,
                BookIdRating = bookRatingCouple,
            };
            return View("Profile", model);
        }
    }
}
