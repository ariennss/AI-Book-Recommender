using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookRecommender.DBObjects
{
    public class Review
    {
        public int Id { get; set; }
        public int BookId { get; set; } 
        public string UserId { get; set; }
        public int Rating { get; set; }
    }
}
