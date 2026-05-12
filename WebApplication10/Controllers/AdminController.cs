using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using PBL3_Hotel_System.Data;
using PBL3_Hotel_System.Models;
using PBL3_Hotel_System.Models.UserModels;
using PBL3_Hotel_System.ViewModels; // Chứa các ViewModel nếu cần
using PBL3_Hotel_System_.Services.Interfaces;
namespace PBL3_Hotel_System.Controllers
{
    [Authorize(Roles = "QuanTriVien")] // Chỉ cho phép Admin truy cập
    public class AdminController(HotelDbContext _db, IStatisticsService _statsService) : Controller
    {
        // 1. TRANG DASHBOARD ADMIN
       

        public async Task<IActionResult> Index()
        {
            ViewData["ActiveMenu"] = "Index";
            // Đếm nhân viên từ bảng kế thừa UserProfiles
            ViewBag.TotalStaff = await _db.UserProfiles.OfType<NhanVien>().CountAsync();
            ViewBag.TotalRooms = await _db.Rooms.CountAsync();
            
            // Giả sử bạn có bảng Đăng ký ca làm
            ViewBag.PendingShifts = await _db.DangKyCaLams.CountAsync(x => x.TrangThai == "Pending");
            try
            {
                // 1. Xác định mốc thời gian: 7 ngày gần nhất (tính từ hôm nay lùi lại)
                DateTime startDate = DateTime.Now.Date.AddDays(-6);

                // 2. Lấy dữ liệu Booking trong khoảng 7 ngày này
                var bookings = await _db.Bookings
                    .Where(b => b.NgayDat >= startDate)
                    .ToListAsync();

                // 3. Tạo danh sách 7 ngày đầy đủ (để những ngày không có khách vẫn hiện số 0)
                var last7Days = Enumerable.Range(0, 7)
                    .Select(i => startDate.AddDays(i))
                    .Select(date => new {
                        Ngay = date.ToString("dd/MM"), // Định dạng ngày/tháng (VD: 10/05)
                                                       // Tìm trong DB xem ngày này có tiền không, không có thì mặc định là 0
                        TongTien = bookings
                            .Where(b => b.NgayDat.Date == date)
                            .Sum(b => (double)b.GiaLucDat)
                    }).ToList();

                // 4. Truyền dữ liệu ra View
                ViewBag.ChartLabels = JsonConvert.SerializeObject(last7Days.Select(x => x.Ngay));
                ViewBag.ChartData = JsonConvert.SerializeObject(last7Days.Select(x => x.TongTien));

                // Tính doanh thu tháng hiện tại (Quick Stat)
                var monthlyRevenue = await _db.Bookings
                    .Where(b => b.NgayDat.Month == DateTime.Now.Month && b.NgayDat.Year == DateTime.Now.Year)
                    .SumAsync(b => b.GiaLucDat);
                ViewBag.MonthlyRevenue = monthlyRevenue.ToString("N0") + " VNĐ";
            }
            catch (Exception ex)
            {
                ViewBag.ChartLabels = "[]";
                ViewBag.ChartData = "[]";
            }
            return View();
        }

        // 2. QUẢN LÝ TÀI KHOẢN (Account)
        public async Task<IActionResult> QuanLyTaiKhoan(string searchString)
        {
            ViewData["ActiveMenu"] = "Accounts";
            var query = _db.Accounts.Include(a => a.UserProfile).AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.ToLower();
                query = query.Where(a => a.Username.ToLower().Contains(searchString)
                                      || (a.UserProfile != null && a.UserProfile.Hoten.ToLower().Contains(searchString)));
            }

            ViewBag.CurrentFilter = searchString;
            return View(await query.ToListAsync());
        }

        // 3. QUẢN LÝ NHÂN VIÊN (Dựa trên lớp kế thừa NhanVien)
        public async Task<IActionResult> QuanLyNhanVien(string searchString)
        {
            ViewData["ActiveMenu"] = "Staff";
            // Lấy danh sách những người là NhanVien trong bảng UserProfiles
            var query = _db.UserProfiles.OfType<NhanVien>().AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.ToLower();
                query = query.Where(nv => nv.Hoten.ToLower().Contains(searchString)
                                       || nv.UserID.ToString().Contains(searchString));
            }

            ViewBag.CurrentFilter = searchString;
            return View(await query.ToListAsync());
        }

        // 4. QUẢN LÝ HỆ THỐNG PHÒNG
        public async Task<IActionResult> QuanLyHeThong()
        {
            ViewData["ActiveMenu"] = "System";
            var phongs = await _db.Rooms.OrderBy(r => r.SoPhong).ToListAsync();
            return View(phongs);
        }

        // --- NGHIỆP VỤ PHÒNG ---

        [HttpGet]
        public IActionResult ThemPhong() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ThemPhong(Room phong)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Tự động tính giá đề xuất nếu Admin để giá bằng 0
                    if (phong.GiaPhong == 0) phong.GiaPhong = phong.TinhGiaDeXuat();

                    _db.Rooms.Add(phong);
                    await _db.SaveChangesAsync();
                    TempData["Success"] = "Đã thêm phòng thành công!";
                    return RedirectToAction("QuanLyHeThong");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Số phòng đã tồn tại hoặc lỗi dữ liệu.");
                }
            }
            return View(phong);
        }

        [HttpGet]
        public async Task<IActionResult> SuaPhong(int id) // id là SoPhong (int)
        {
            var phong = await _db.Rooms.FindAsync(id);
            if (phong == null) return NotFound();
            return View(phong);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SuaPhong(Room phong)
        {
            if (ModelState.IsValid)
            {
                _db.Entry(phong).State = EntityState.Modified;
                await _db.SaveChangesAsync();
                TempData["Success"] = $"Đã cập nhật phòng #{phong.SoPhong} thành công!";
                return RedirectToAction("QuanLyHeThong");
            }
            return View(phong);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XoaPhong(int id)
        {
            var phong = await _db.Rooms.FindAsync(id);
            if (phong != null)
            {
                // Kiểm tra xem phòng có đang được đặt không trước khi xóa
                bool hasBookings = await _db.Bookings.AnyAsync(b => b.SoPhong == id);
                if (hasBookings)
                {
                    TempData["Error"] = "Không thể xóa phòng đang có lịch đặt!";
                }
                else
                {
                    _db.Rooms.Remove(phong);
                    await _db.SaveChangesAsync();
                    TempData["Success"] = "Đã xóa phòng khỏi hệ thống.";
                }
            }
            return RedirectToAction("QuanLyHeThong");
        }

        // 5. XÓA NHÂN VIÊN (Và tài khoản đi kèm)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XoaNhanVien(int id)
        {
            var nv = await _db.UserProfiles.OfType<NhanVien>()
                             .Include(u => u.Account)
                             .FirstOrDefaultAsync(u => u.UserID == id);

            if (nv != null)
            {
                // Nếu xóa nhân viên, ta nên xóa luôn tài khoản Login của họ để tránh rác DB
                if (nv.Account != null) _db.Accounts.Remove(nv.Account);
                
                _db.NhanViens.Remove(nv);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Đã xóa nhân viên và tài khoản liên quan thành công!";
            }
            return RedirectToAction("QuanLyNhanVien");
        }
    }
}
