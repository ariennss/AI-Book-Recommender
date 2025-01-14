using BookRecommender.DBObjects;
using System.Text;
using System.Data.SQLite;
using System.Data.Entity.ModelConfiguration.Conventions;

namespace WebApplication1.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly string ConnectionString = "Data Source=C:\\tesi\\bookRecommender.db;Version=3";
        private static List<User> users = new List<User>();

        // all'inizializzazione del repository fillo la lista così da fare le query poi in locale
        public UserRepository()
        {
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                var command = new SQLiteCommand("SELECT * FROM Users", connection);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        users.Add(new User
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            Surname = reader.GetString(2),
                            Email = reader.GetString(3),
                            Username = reader.GetString(4),
                            Password = reader.GetString(5),
                        });
                    }
                }
            }

        }

        public void AddUser(User user)
        {
            var username = user.Username;
            var pwd = user.Password;
            var email = user.Email;
            var firstname = user.Name;
            var surname = user.Surname;

            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                string query = $"INSERT INTO Users(firstname, surname, email, username, password) VALUES('{firstname}', '{surname}', '{email}', '{username}', '{pwd}')";
                var command = new SQLiteCommand(query, connection);
                try
                {
                    command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Something wrong with the query");
                }

            }

            users.Add(user);

        }

        public List<User> GetAllUsers()
        {
            return users;
        }

        public User? GetUserByUsername(string username)
        {
            return users.Where(x => x.Username == username).SingleOrDefault();
        }

        public User? CheckCredentials(string username, string pwd)
        {
            var users = GetAllUsers();
            var correctUser = users.Where(x => x.Username == username && x.Password == pwd).SingleOrDefault();
            return correctUser;
        }
    }
}
