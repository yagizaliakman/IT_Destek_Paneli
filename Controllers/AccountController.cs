using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using IT_Destek_Panel.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace IT_Destek_Panel.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            // 1. Gelen düz şifreyi kriptolayıp Hash'e çeviriyoruz
            string hashedPassword = IT_Destek_Panel.Helpers.SecurityHelper.HashPassword(password);

            // 2. Artık veritabanında "password" ile değil, "hashedPassword" ile arama yapıyoruz
            var user = _context.Users.FirstOrDefault(u => u.Username == username && u.Password == hashedPassword && u.IsDeleted == false);

            if (user != null)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim("UserId", user.Id.ToString()),
                    // Enum'ı ToString() ile metne çevirip çereze atıyoruz
                    new Claim(ClaimTypes.Role, user.Role.ToString())
                };

                var claimsIdentity = new ClaimsIdentity(claims, "CookieAuth");
                await HttpContext.SignInAsync("CookieAuth", new ClaimsPrincipal(claimsIdentity));

                // EĞER GİREN ADMİN İSE DİREKT YÖNETİM PANELİNE AT
                if (user.Role == IT_Destek_Panel.Models.UserRole.Admin)
                {
                    return RedirectToAction("Index", "Admin");
                }
                // EĞER ÖĞRENCİYSE KENDİ SAYFASINA AT
                else
                {
                    return RedirectToAction("Index", "Ticket");
                }
            }

            ViewBag.Error = "Kullanıcı adı veya şifre hatalı!";
            return View();
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public IActionResult Register(string username, string password)
        {
            if (_context.Users.Any(u => u.Username == username))
            {
                ViewBag.Error = "Bu kullanıcı adı zaten alınmış.";
                return View();
            }

            var newUser = new User
            {
                Username = username,
                // İŞTE SİHİRLİ GÜVENLİK DOKUNUŞU: Düz şifreyi Hash'liyoruz!
                Password = IT_Destek_Panel.Helpers.SecurityHelper.HashPassword(password),
                Role = UserRole.User, // Artık "User" değil Enum kullanıyoruz
                IsDeleted = false
            };

            _context.Users.Add(newUser);
            _context.SaveChanges();

            return RedirectToAction("Login");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("CookieAuth");
            return RedirectToAction("Login");
        }
    
    // 404 Hata Sayfasına Yönlendiren Metot
public IActionResult NotFoundPage()
        {
            Response.StatusCode = 404; // Tarayıcıya ve Google'a "Bu sayfa yok" diyoruz
            return View();
        }
        [HttpGet]
        public IActionResult LoadWeather(string city, string lat, string lon)
        {
            // JavaScript'ten gelen verileri ViewComponent'a yolluyoruz
            return ViewComponent("WeatherWidget", new { city = city, lat = lat, lon = lon });
        }
    }

}
