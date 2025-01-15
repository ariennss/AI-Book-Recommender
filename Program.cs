using BookRecommender.Repositories;
using Microsoft.AspNetCore.Authentication.Cookies;
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

var a = new ReviewRepository();
var x = new BookRepository(a);
var c = new Recommendations(x,a);
c.TopRatedBooks();
var e = x.GetMostPopularBooks();
app.Run();
