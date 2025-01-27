using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookRecommender.DBObjects
{
    public class Book
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int AuthorId { get; set; }
        public string AuthorName { get; set; }
        public string ImgUrl { get; set; }
        public List<Review> Reviews { get; set; }
        public List<string> Genres { get; set; }

        public int RatingsCount { get; set; }   
    }
}
