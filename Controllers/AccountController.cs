using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using HcPortal.Models;

namespace HcPortal.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // 1. Tampilkan Halaman Login (GET)
        public IActionResult Login(string? returnUrl = null)
        {
            // Jika sudah login, redirect ke Home
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }
            
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // 2. Proses saat tombol Login ditekan (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, bool rememberMe = false, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Email dan Password harus diisi!";
                return View();
            }

            // Cari user berdasarkan email
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                ViewBag.Error = "Email atau Password salah!";
                return View();
            }

            // Coba login
            var result = await _signInManager.PasswordSignInAsync(user, password, rememberMe, lockoutOnFailure: false);
            
            if (result.Succeeded)
            {
                // Login berhasil
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Email atau Password salah!";
            return View();
        }

        // 3. Proses Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }

        // 4. Halaman Profile User
        public async Task<IActionResult> Profile()
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return RedirectToAction("Login");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var roles = await _userManager.GetRolesAsync(user);
            ViewBag.UserRole = roles.FirstOrDefault() ?? "No Role";

            return View(user);
        }

        // 5. Halaman Settings
        public IActionResult Settings()
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return RedirectToAction("Login");
            }
            return View();
        }

        // 6. Access Denied Page
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}