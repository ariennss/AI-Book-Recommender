using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using WebApplication1.Repositories;
using BookRecommender.DBObjects;
using WebApplication1;

public class AccountController : Controller
{
    private readonly IUserRepository _userRepository;
    public AccountController(IUserRepository userRep)
    {
        _userRepository = userRep;
    }
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string username, string password)
    {
        // Qui dovresti implementare la logica di verifica delle credenziali
        // (es. query al database)
        var current = _userRepository.CheckCredentials(username, password);
        if (current != null) // Esempio semplificato
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, "User"), // Esempio di claim ruolo
            };

            var claimsIdentity = new ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity));

            CurrentUser.username = username;
            CurrentUser.password = password;
            return RedirectToAction("Index", "Home");
        }

        ViewData["ErrorMessage"] = "Credenziali non valide.";
        return View();
    }

    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        CurrentUser.Clear();
        return RedirectToAction("Homepage", "Home");
    }

    public IActionResult AccessDenied()
    {
        return View();
    }

    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(string username, string password, string confirmPassword)
    {
        if (password != confirmPassword)
        {
            ViewData["ErrorMessage"] = "Le password non corrispondono.";
            return View();
        }

        var attempt = _userRepository.GetUserByUsername(username);
        if ( attempt != null)
        {
            ViewData["ErrorMessage"] = "Username già esistente.";
            return View();
        }

        var newUser = new User() { 
            Username = username,
            Password = password,
            };
        _userRepository.AddUser(newUser);


        // Qui dovresti implementare la logica di registrazione dell'utente:
        // 1. Controllare se l'utente esiste già (es. query al database)
        // 2. Hash della password (MOLTO IMPORTANTE)
        // 3. Creazione del nuovo utente nel database

        //if (1 == 1)
        //{
        //    ViewData["ErrorMessage"] = "Username già esistente.";
        //    return View();
        //}

        // Esempio semplificato (SENZA HASHING - NON USARE IN PRODUZIONE)
        // In una vera applicazione, usa PasswordHasher o un metodo simile
        // Inserisci l'utente nel database qui...

        return RedirectToAction("Login"); // Reindirizza al login dopo la registrazione
    }
}