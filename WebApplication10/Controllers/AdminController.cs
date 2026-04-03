using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication10.Models;

namespace WebApplication10.Controllers
{
    public class AdminController : Controller
    {
        private readonly QuanLyKhachSanDB _db;

        public AdminController(QuanLyKhachSanDB db)
        {
            _db = db;
        }

        // 1. Trang chủ Dashboard
        public IActionResult Index()
        {
            ViewBag.TotalStaff = _db.NhanViens.Count();
            ViewBag.TotalRooms = _db.Phongs.Count();
            ViewBag.PendingShifts = _db.DangKyCaLams.Count(x => x.TrangThai == "Pending");
            return View();
        }

        // 2. Quản lý tài khoản
        public IActionResult QuanLyTaiKhoan()
        {
            var accounts = _db.TaiKhoans.Include(u => u.NhanVien).ToList();
            return View(accounts);
        }

        [HttpPost]
        public IActionResult ToggleLock(string username)
        {
            var acc = _db.TaiKhoans.Find(username);
            if (acc != null)
            {
                acc.IsLocked = !acc.IsLocked;
                _db.SaveChanges();
            }
            return RedirectToAction("QuanLyTaiKhoan");
        }

        // 3. Quản lý nhân viên
        public IActionResult QuanLyNhanVien()
        {
            var staff = _db.NhanViens.Include(nv => nv.ChucVu).ToList();
            return View(staff);
        }

        // 4. QUẢN LÝ HỆ THỐNG (Hàm này đã được thêm để sửa lỗi 404)
        public IActionResult QuanLyHeThong()
        {
            var phongs = _db.Phongs.ToList();
            return View(phongs);
        }

        // 5. Phê duyệt ca làm
        public IActionResult DuyetCaLam()
        {
            var pendingList = _db.DangKyCaLams
                .Include(x => x.NhanVien)
                .Include(x => x.CaLam)
                .Where(x => x.TrangThai == "Pending").ToList();
            return View(pendingList);
        }
    }
}