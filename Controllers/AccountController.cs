using Microsoft.AspNetCore.Mvc;

namespace HcPortal.Controllers
{
    public class AccountController : Controller
    {
        // 1. Tampilkan Halaman Login (GET)
        public IActionResult Login()
        {
            return View();
        }

        // 2. Proses saat tombol Login ditekan (POST)
        [HttpPost]
        public IActionResult Login(string nip, string password)
        {
            // SIMULASI LOGIN SEDERHANA
            // Di dunia nyata, ini akan cek ke Database
            
            if (!string.IsNullOrEmpty(nip) && !string.IsNullOrEmpty(password))
            {
                // Jika NIP & Password diisi, anggap sukses -> Masuk ke Homepage
                return RedirectToAction("Index", "Home");
            }

            // Jika kosong, kembalikan ke halaman login dengan pesan error
            ViewBag.Error = "NIP atau Password tidak boleh kosong!";
            return View();
        }

        // 3. Proses Logout
        public IActionResult Logout()
        {
            // Kembali ke halaman login
            return RedirectToAction("Login");
        }
    }
}