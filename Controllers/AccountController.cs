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
        public async Task<IActionResult> Settings()
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

            var model = new SettingsViewModel
            {
                EditProfile = new EditProfileViewModel
                {
                    FullName = user.FullName,
                    Position = user.Position,
                    PhoneNumber = user.PhoneNumber
                },
                ChangePassword = new ChangePasswordViewModel(),
                NIP = user.NIP,
                Email = user.Email,
                Role = roles.FirstOrDefault() ?? "—",
                Section = user.Section,
                Directorate = user.Directorate,
                Unit = user.Unit
            };

            return View(model);
        }

        // 5a. Edit Profile POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile([Bind(Prefix = "EditProfile")] EditProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ProfileError"] = "Periksa kembali isian profil.";
                return RedirectToAction("Settings");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            user.FullName = model.FullName;
            user.Position = model.Position;
            user.PhoneNumber = model.PhoneNumber;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                TempData["ProfileSuccess"] = "Profil berhasil diperbarui.";
            }
            else
            {
                TempData["ProfileError"] = string.Join("; ", result.Errors.Select(e => e.Description));
            }

            return RedirectToAction("Settings");
        }

        // 5b. Change Password POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword([Bind(Prefix = "ChangePassword")] ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["PasswordError"] = "Periksa kembali isian password.";
                return RedirectToAction("Settings");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                TempData["PasswordSuccess"] = "Password berhasil diubah.";
            }
            else
            {
                if (result.Errors.Any(e => e.Code == "PasswordMismatch"))
                {
                    TempData["PasswordError"] = "Password lama salah.";
                }
                else
                {
                    TempData["PasswordError"] = string.Join("; ", result.Errors.Select(e => e.Description));
                }
            }

            return RedirectToAction("Settings");
        }

        // 6. Access Denied Page
        public IActionResult AccessDenied()
        {
            return View();
        }

    }
}