using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication10.Models;
using Microsoft.AspNetCore.Http;

namespace WebApplication10.Controllers
{
    public class AccountController : Controller
    {
        private readonly QuanLyKhachSanDB _db;

        public AccountController(QuanLyKhachSanDB db)
        {
            _db = db;
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            var user = _db.TaiKhoans
                .Include(u => u.NhanVien)
                .ThenInclude(nv => nv.ChucVu)
                .FirstOrDefault(u => u.TenDangNhap == username && u.MatKhau == password);

            if (user != null)
            {
                string role = user.NhanVien.ChucVu.TenChucVu;
                HttpContext.Session.SetString("UserName", user.NhanVien.HoTen);
                HttpContext.Session.SetString("UserRole", role);

                if (role == "Admin")
                    return RedirectToAction("Index", "Admin");
                else
                    return RedirectToAction("Index", "NhanVien");
            }

            ViewBag.Error = "Sai tài khoản hoặc mật khẩu";
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}