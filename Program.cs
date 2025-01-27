using BookRecommender.Repositories;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Data.SQLite;
using WebApplication1;
using WebApplication1.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login"; // Percorso della pagina di login
        options.LogoutPath = "/Account/Logout"; // Percorso per il logout
        options.AccessDeniedPath = "/Account/AccessDenied"; // Percorso per accesso negato
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30); // Durata del cookie
        options.SlidingExpiration = true; // Rinnova il cookie ad ogni richiesta
    });
builder.Services.AddAuthorization();
builder.Services.AddSingleton<IUserRepository, UserRepository>();
builder.Services.AddSingleton<IBookRepository, BookRepository>();
builder.Services.AddSingleton<IReviewRepository, ReviewRepository>();
builder.Services.AddSingleton<ICollaborativeFiltering, CollaborativeFiltering>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication(); // Importante: deve essere prima di 
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

//var a = new ReviewRepository();
//var x = new BookRepository(a);
//var c = new Recommendations(x,a);
//c.TopRatedBooks();
//var e = x.GetMostPopularBooks();
//var tfids = new TFIDFContentRecommendation(x);
//tfids.FindTop10MostSimilarToDescription("a flying sheep");
//var cf = new CollaborativeFiltering(x,a);
//cf.SuggestionsFor("ariennss");



//app.Run();

string connectionString = "Data Source=C:\\tesi\\bookRecommender.db;Version=3;";
string outputCsvFile = "books.csv";  // Output CSV file

// SQL query to select book_id and description
string query = "SELECT book_id, description FROM Books WHERE lcv = 0"; // You can add any condition you need

// Create a new StreamWriter to write to the CSV file
using (var writer = new StreamWriter(outputCsvFile))
{
    // Write the header to the CSV file
    writer.WriteLine("book_id,description");

    using (var connection = new SQLiteConnection(connectionString))
    {
        // Open the connection
        connection.Open();

        // Create the command
        using (var command = new SQLiteCommand(query, connection))
        {
            // Execute the command and get the data
            using (var reader = command.ExecuteReader())
            {
                // Read each row from the result set
                while (reader.Read())
                {
                    // Extract book_id and description
                    var bookId = reader.GetInt32(0);
                    var description = reader.GetString(1);

                    // Write the book_id and description to the CSV file
                    writer.WriteLine($"{bookId},{description}");
                }
            }
        }
    }
}

Console.WriteLine("CSV file has been generated: " + outputCsvFile);
    
