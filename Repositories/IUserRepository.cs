using BookRecommender.DBObjects;

namespace WebApplication1.Repositories
{
    public interface IUserRepository
    {
        public User? CheckCredentials(string username, string pwd);
        public User? GetUserByUsername(string username);
        public List<User> GetAllUsers();
        public void AddUser(User user);
    }
}
