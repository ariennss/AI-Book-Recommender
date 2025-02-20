using BookRecommender.DBObjects;
using BookRecommender.Repositories;

namespace WebApplication1
{
    public class CollaborativeFiltering : ICollaborativeFiltering
    {
        private readonly IBookRepository _bookRepository;
        private readonly IReviewRepository _reviewRepository;

        public CollaborativeFiltering(IBookRepository bookrepo, IReviewRepository reviewrepo)
        {
            _bookRepository = bookrepo;
            _reviewRepository = reviewrepo;
        }

        public List<Book> SuggestionsFor(string username)
        {
            var reviews = _reviewRepository.GetAllReviews();
            var userSimilarities = GetMostSimilarUsers(username);
            var b = _bookRepository.GetAllBooks();
            var currentUserReviews = reviews.Where(x => x.UserId == username).ToList();
            var currentUserBookIds = currentUserReviews.Select(r => r.BookId).ToHashSet();


            // Sort users by similarity (descending order)
            var sortedSimilarUsers = userSimilarities
                .OrderByDescending(pair => pair.Value)
                .Select(pair => pair.Key)
                .ToList();

            var numberOfSuggestions = 5;
            var suggestedBookIds = new HashSet<int>();
            foreach (var similarUserId in sortedSimilarUsers)
            {
                if (suggestedBookIds.Count >= numberOfSuggestions)
                {
                    break; // Si esce se abbiamo già abbastanza recommendation
                }

                var similarUserReviews = reviews
                    .Where(r => r.UserId == similarUserId && r.Rating >= 4) // solo recensioni alte
                    .Where(r => !currentUserBookIds.Contains(r.BookId)) // evitando i libri che l'utente ha già recensito
                    .Select(r => r.BookId)
                    .ToList();

                foreach (var bookId in similarUserReviews)
                {
                    if (suggestedBookIds.Count >= numberOfSuggestions)
                    {
                        break;
                    }

                    suggestedBookIds.Add(bookId);
                }


            }

            var suggestedBooks = _bookRepository.GetBooksByIds(suggestedBookIds);
            var booksWithRatings = suggestedBooks
        .Select(book => new
        {
            Book = book,
            AverageRating = reviews
                .Where(r => r.BookId == book.Id)
                .Average(r => r.Rating),
            RatingsCount = reviews
                .Count(r => r.BookId == book.Id)
        })
        .OrderByDescending(x => x.AverageRating) // ordinati per media delle recensioni
        .ThenByDescending(x => x.RatingsCount)  // E poi ordinati per quante recensioni hanno
        .Take(5) // prendo i primi 5 - variabile
        .Select(x => x.Book)
        .ToList();
            return suggestedBooks.OrderByDescending(x => x.RatingsCount).Take(5).ToList();


        }


        public Dictionary<string, double> GetMostSimilarUsers(string username)
        {
            var reviews = _reviewRepository.GetAllReviews();
            var b = _bookRepository.GetAllBooks();
            
            var currentUserReviews = reviews.Where(x => x.UserId == username).ToList();
            var currentUserBookIds = currentUserReviews.Select(r => r.BookId).ToHashSet();

            // raggruppo gli utenti escludento l'utente loggato
            var userGroups = reviews
                .Where(r => r.UserId != username)
                .GroupBy(r => r.UserId);

            // calcolo la similarità dei coseni per ogni utente.
            var userSimilarities = userGroups
                .Select(group =>
                {
                    var otherUserId = group.Key;
                    var otherUserReviews = group.ToList();

                    // trovo per ogni utente le recensioni in comune con l'utente corrente
                    var commonReviews = otherUserReviews
                        .Where(r => currentUserBookIds.Contains(r.BookId))
                        .ToList();

                    if (commonReviews.Count < 3)
                        return null; // Salto chi non ha almeno tre recensioni in comune

                    // Accoppio recensioni di ogni utente con quelle dell'utente corrente
                    var ratingPairs = commonReviews
                        .Select(review => new
                        {
                            CurrentUserRating = currentUserReviews.First(r => r.BookId == review.BookId).Rating,
                            OtherUserRating = review.Rating
                        })
                        .ToList();

                    // Calcolo cosine similarity
                    var dotProduct = ratingPairs.Sum(pair => pair.CurrentUserRating * pair.OtherUserRating);
                    var magnitudeCurrentUser = Math.Sqrt(ratingPairs.Sum(pair => pair.CurrentUserRating * pair.CurrentUserRating));
                    var magnitudeOtherUser = Math.Sqrt(ratingPairs.Sum(pair => pair.OtherUserRating * pair.OtherUserRating));

                    var cosineSimilarity = (magnitudeCurrentUser == 0 || magnitudeOtherUser == 0) ? 0 : dotProduct / (magnitudeCurrentUser * magnitudeOtherUser);

                    return new { otherUserId, cosineSimilarity };
                })
                .Where(result => result != null) 
                .ToDictionary(result => result.otherUserId, result => result.cosineSimilarity);

            return userSimilarities;

            // parte tolta perché non è questo il luogo adatto.

            //// Sort users by similarity (descending order)
            //var sortedSimilarUsers = userSimilarities
            //    .OrderByDescending(pair => pair.Value)
            //    .Select(pair => pair.Key)
            //    .ToList();
        }
    }
}
