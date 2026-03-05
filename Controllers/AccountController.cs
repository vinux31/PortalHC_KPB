using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using HcPortal.Models;
using HcPortal.Services;
using Microsoft.Extensions.Configuration;

namespace HcPortal.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IAuthService _authService;
        private readonly IConfiguration _config;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IAuthService authService,
            IConfiguration config)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _authService = authService;
            _config = config;
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

            // Step 1: Authenticate via IAuthService (Local or AD per DI factory from Program.cs)
            var authResult = await _authService.AuthenticateAsync(email, password);

            if (!authResult.Success)
            {
                ViewBag.Error = authResult.ErrorMessage;
                return View();
            }

            // Step 2: Find user in DB — HC must pre-register all users
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                ViewBag.Error = "Akun Anda belum terdaftar. Hubungi HC.";
                return View();
            }

            // Step 2b: Block inactive users from logging in
            if (!user.IsActive)
            {
                ViewBag.Error = "Akun Anda tidak aktif. Hubungi HC untuk mengaktifkan kembali akun Anda.";
                return View();
            }

            // Step 3: AD mode — sync FullName and Email from AuthResult (null-safe, skip nulls)
            var useAD = _config.GetValue<bool>("Authentication:UseActiveDirectory", false);
            if (useAD)
            {
                bool profileChanged = false;

                if (!string.IsNullOrEmpty(authResult.FullName) && authResult.FullName != user.FullName)
                {
                    user.FullName = authResult.FullName;
                    profileChanged = true;
                }

                if (!string.IsNullOrEmpty(authResult.Email) && authResult.Email != user.Email)
                {
                    user.Email = authResult.Email;
                    profileChanged = true;
                }

                if (profileChanged)
                {
                    try
                    {
                        await _userManager.UpdateAsync(user);
                    }
                    catch
                    {
                        // Sync failure is non-fatal — auth succeeded, login continues
                    }
                }
            }

            // Step 4: Create session cookie
            await _signInManager.SignInAsync(user, rememberMe);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
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
                var error = result.Errors.FirstOrDefault();
                if (error != null)
                {
                    TempData["PasswordError"] = error.Code switch
                    {
                        "PasswordMismatch" => "Password lama salah.",
                        "PasswordTooShort" => "Password baru minimal 6 karakter.",
                        "PasswordRequiresUniqueChars" => "Password baru harus memiliki minimal 1 karakter unik.",
                        "PasswordRequiresNonAlphanumeric" => "Password baru harus memiliki minimal 1 karakter khusus.",
                        "PasswordRequiresDigit" => "Password baru harus memiliki minimal 1 angka.",
                        "PasswordRequiresLower" => "Password baru harus memiliki minimal 1 huruf kecil.",
                        "PasswordRequiresUpper" => "Password baru harus memiliki minimal 1 huruf besar.",
                        _ => "Terjadi kesalahan saat mengubah password. Coba lagi."
                    };
                }
                else
                {
                    TempData["PasswordError"] = "Terjadi kesalahan saat mengubah password. Coba lagi.";
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