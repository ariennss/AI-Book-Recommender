using BookRecommender.DBObjects;
using javax.management.relation;
using static com.sun.tools.@internal.xjc.reader.xmlschema.bindinfo.BIConversion;

namespace WebApplication1
{
    public static class CurrentUser
    {
        public static string? username;
        public static string? password;
        //public static List<Review> currentUserReviews;

        public static void Clear()
        {
            username = null;
            password = null;
        }
    }
}
