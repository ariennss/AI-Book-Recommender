using BookRecommender.DBObjects;
using BookRecommender.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using WebApplication1.Models;
using WebApplication1.ViewModels;

namespace WebApplication1.Controllers
{
    public class RatingsController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IBookRepository _bookRepository;
        private readonly IReviewRepository _reviewRepository;

        public RatingsController(IBookRepository br, IReviewRepository rr)
        {
            _bookRepository = br;
            _reviewRepository = rr;
        }

        public async Task<IActionResult> AddRating(int BookId, int rating)
        {
          
                var review = new Review { BookId = BookId, Rating = rating, UserId = CurrentUser.username };
                await _reviewRepository.AddReviewAsync(review);
                return RedirectToAction("Index", "Home");
            
         
        }
    }
}
