using BookRecommender.DBObjects;
using BookRecommender.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using WebApplication1.Models;
using WebApplication1.ViewModels;

namespace WebApplication1.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IBookRepository _bookRepository;
        private readonly IReviewRepository _reviewRepository;
        private readonly ICollaborativeFiltering _collaborativeFiltering;
        private readonly IHybridContentRecommendation _contentRecommender;

        public HomeController(ILogger<HomeController> logger, IBookRepository br, IReviewRepository rr, ICollaborativeFiltering collaborativeFiltering, IHybridContentRecommendation contentrec)
        {
            _logger = logger;
            _bookRepository = br;
            _reviewRepository = rr;
            _collaborativeFiltering = collaborativeFiltering;
            _contentRecommender = contentrec;
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
                    // User has rated enough books, show recommendations
                    
                    var suggestedBooks = _collaborativeFiltering.SuggestionsFor(username);
                    //var bestReviewedBooks = _bookRepository.GetBestReviewedBooksAsync(5);

                    var viewModel = new HomeViewModel
                    {
                        MostPopularBooks = mostPopularBooks,
                        SuggestedBooks = suggestedBooks,
                        //BestReviewedBooks = bestReviewedBooks
                    };

                    return View("Recommendations", viewModel); // Return a view called Recommendations
                }
                else
                {
                    return View("ColdUser", new HomeViewModel { MostPopularBooks = mostPopularBooks }); //TODO: creare questa view
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
            return View(new AISuggestionsModel
            {
                Query = "", // Default empty query
                Recommendations = new List<Book>() // Empty list to avoid null errors
            });
        }


        [HttpPost]
        public IActionResult GetAISuggestions(string query)
        {
            // Here, you would process the query and return recommendations
            // For now, just redirect back to the same page
            var recommendations = _contentRecommender.FindTop10MostSimilarToDescriptionAsync(query).Result;
            var model = new AISuggestionsModel
            {
                Query = query,
                Recommendations = recommendations,
            };
            return View("AISuggestions", model);
        }
    }
}
