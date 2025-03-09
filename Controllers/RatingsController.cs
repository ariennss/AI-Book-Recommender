using BookRecommender.DBObjects;
using BookRecommender.Repositories;
using javax.xml.transform;
using Microsoft.AspNetCore.Http;
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
        private readonly IHttpContextAccessor _contextAccessor;

        public RatingsController(IBookRepository br, IReviewRepository rr, IHttpContextAccessor ca)
        {
            _contextAccessor = ca;
            _bookRepository = br;
            _reviewRepository = rr;
        }

        public async Task<IActionResult> AddRating(int BookId, int rating)
        {
            int[] ok = { 1, 2, 3, 4, 5 };
                if (ok.Contains(rating))
            {
                var review = new Review { BookId = BookId, Rating = rating, UserId = _contextAccessor.HttpContext?.User?.Identity?.Name };
                await _reviewRepository.AddReviewAsync(review);
                return RedirectToAction("Index", "Home");
            }
            else
            {
                return BadRequest("An error occurred while processing your request.");
            }

            
               
            
         
        }
    }
}
