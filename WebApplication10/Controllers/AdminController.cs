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

        public IActionResult Index()
        {
            ViewBag.TotalStaff = _db.NhanViens.Count();
            ViewBag.TotalRooms = _db.Rooms.Count();
            ViewBag.PendingShifts = _db.DangKyCaLams.Count(x => x.TrangThai == "Pending");
            return View();
        }

        public IActionResult QuanLyTaiKhoan(string searchString)
        {
            // 1. Khởi tạo truy vấn, bao gồm thông tin Nhân viên
            var query = _db.Accounts.Include(a => a.NhanVien).AsQueryable();

            // 2. Kiểm tra nếu có từ khóa tìm kiếm
            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.ToLower();

                // Lọc theo Username hoặc Tên nhân viên sở hữu tài khoản đó
                query = query.Where(a => a.Username.ToLower().Contains(searchString)
                                      || (a.NhanVien != null && a.NhanVien.HoTen.ToLower().Contains(searchString)));
            }

            // 3. Lưu lại từ khóa để hiển thị ở ô tìm kiếm trên View
            ViewBag.CurrentFilter = searchString;

            var accounts = query.ToList();
            return View(accounts);
        }

        // --- SỬA LẠI ĐOẠN NÀY ---
        // Đây phải là một hàm (Action) nằm trong AdminController
        // Tìm kiếm nhân viên theo Tên, Số điện thoại hoặc Email
        public IActionResult QuanLyNhanVien(string searchString)
        {
            // 1. Khởi tạo truy vấn cơ bản (bao gồm thông tin Chức vụ)
            var query = _db.NhanViens.Include(nv => nv.ChucVu).AsQueryable();

            // 2. Nếu có nhập từ khóa tìm kiếm
            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.ToLower();
                // Lọc theo Tên, Số điện thoại hoặc Email
                query = query.Where(nv => nv.HoTen.ToLower().Contains(searchString)
                                       || (nv.MaNV != null && nv.MaNV.ToString().Contains(searchString))
                                       );

            }

            // 3. Lưu lại từ khóa tìm kiếm để hiển thị lại trên ô nhập liệu ở View
            ViewBag.CurrentFilter = searchString;

            var staff = query.ToList();
            return View(staff);
        }
        // -----------------------

        public IActionResult QuanLyHeThong()
        {
            var phongs = _db.Rooms.ToList();
            return View(phongs);
        }
        // --- QUẢN LÝ PHÒNG ---

        // 1. Chức năng THÊM PHÒNG (Giao diện)
        public IActionResult ThemPhong()
        {
            return View();
        }

        // 1. Chức năng THÊM PHÒNG (Xử lý lưu vào DB)
        [HttpPost]
        [HttpPost]
        public IActionResult ThemPhong(Room phong)
        {
            try
            {
                _db.Rooms.Add(phong);
                _db.SaveChanges();
                TempData["Success"] = "Đã thêm phòng thành công!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi: " + ex.Message;
            }
            return RedirectToAction("QuanLyHeThong");
        }

        // 2. Chức năng SỬA PHÒNG (Giao diện - Lấy dữ liệu cũ đổ vào form)
        public IActionResult SuaPhong(string id) // id ở đây chính là SoPhong
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var phong = _db.Rooms.Find(id);
            if (phong == null) return NotFound();

            return View(phong);
        }

        // 2. Chức năng SỬA PHÒNG (Xử lý cập nhật thay đổi)
        [HttpPost]
        public IActionResult SuaPhong(Room phong)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _db.Entry(phong).State = EntityState.Modified;
                    _db.SaveChanges();
                    return RedirectToAction("QuanLyHeThong");
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật!");
                }
            }
            return View(phong);
        }

        // 3. Chức năng XÓA PHÒNG (Nên có)
        [HttpPost]
        public IActionResult XoaPhong(string id)
        {
            var phong = _db.Rooms.Find(id);
            if (phong != null)
            {
                _db.Rooms.Remove(phong);
                _db.SaveChanges();
            }
            return RedirectToAction("QuanLyHeThong");
        }
        // Chức năng XÓA NHÂN VIÊN
        [HttpPost]
        public IActionResult XoaNhanVien(int id)
        {
            try
            {
                // 1. Tìm nhân viên theo mã
                var nv = _db.NhanViens.Find(id);

                if (nv != null)
                {
                    // 2. Thực hiện xóa
                    _db.NhanViens.Remove(nv);
                    _db.SaveChanges();
                    TempData["Success"] = "Đã xóa nhân viên thành công!";
                }
                else
                {
                    TempData["Error"] = "Không tìm thấy nhân viên này.";
                }
            }
            catch (Exception ex)
            {
                // Trường hợp nhân viên đang có dữ liệu ở bảng khác (ví dụ: có tài khoản)
                TempData["Error"] = "Không thể xóa nhân viên này vì có dữ liệu liên quan (tài khoản hoặc lịch làm việc).";
            }

            return RedirectToAction("QuanLyNhanVien");
        }
    }
}
