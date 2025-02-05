//TODO: remove from hybrid suggestions the books that i have already rated!
//TODO: implementare il sistema di recensioni
//TODO: rendere non inviabile il form di rating per quei libri che ho già recensito
//TODO: fare le schermate 
//TODO: ricordarsi la pagina deel profilo in cui l'utente può vedere i film che ha già recensito
//TODO: creare la view relativa all'utente che ancora non ha reviews.
//TODO: when ranking books with tf idf and w2v i can also create a ranking for each book based on how it is liked by my
//TODO:   users similarity e pesare la suggestion finale anche in base a quello. 
//TODO: nel collaborative filtering tenere conto della quantità di libri in comune a parità di punteggio per determinarel'utente piu simile


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
builder.Services.AddSingleton<IHybridContentRecommendation, HybridContentRecommendation>();

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
    pattern: "{controller=Home}/{action=HomePage}/{id?}");

//var a = new ReviewRepository();
//var x = new BookRepository(a);
//var c = new Recommendations(x, a);
//c.TopRatedBooks();
//var cf = new CollaborativeFiltering(x,a);
//var e = x.GetMostPopularBooks();
//var tfids = new HybridContentRecommendation(x, cf, a);



//var x66 = tfids.FindTop10MostSimilarToDescriptionAsync("a love story in an ancient castle");
//var x66 = tfids.FindTop10MostSimilarToDescriptionAsync("a horror story about two sisters in an abandoned house");
//var x66 = tfids.FindTop10MostSimilarToDescriptionAsync("a dystopian society");
//var x46 = tfids.FindTop10MostSimilarToDescriptionAsync("a vampire love story");
//var x46 = tfids.FindTop10MostSimilarToDescriptionAsync("a dog with superpowers");
//var cf = new CollaborativeFiltering(x,a);
//cf.SuggestionsFor("ariennss");



app.Run();


    
