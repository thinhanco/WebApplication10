using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using WebApplication10.Models;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace WebApplication10.Controllers
{
    [Authorize(Roles = "NhanVien")]
    public class NhanVienController : Controller
    {
        private readonly QuanLyKhachSanDB _db;

        public NhanVienController(QuanLyKhachSanDB db)
        {
            _db = db;
        }

        private int GetMaNV()
        {
            // Lấy Username của người đang đăng nhập từ Cookie
            var userName = User.Identity?.Name;
            if (string.IsNullOrEmpty(userName)) return 0;

            // 1. Tìm Account dựa trên Username
            var account = _db.Accounts.FirstOrDefault(a => a.Username == userName);
            if (account == null) return 0;

            // 2. Tìm Nhân viên dựa trên AccountID vừa lấy được
            var nhanVien = _db.NhanViens.FirstOrDefault(nv => nv.AccountID == account.AccountID);

            // 3. Trả về đúng MaNV của bảng NhanViens
            return nhanVien?.MaNV ?? 0;
        }

        public IActionResult Index()
        {
            ViewBag.HoTen = User.Identity?.Name;
            return View();
        }

        public IActionResult DatCaLam()
        {
            int maNV = GetMaNV();

            if (maNV == 0)
            {
                return RedirectToAction("Login", "Account");
            }

            var loaiCas = _db.CaLams.ToList();

            DateTime today = DateTime.Today;
            int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
            DateTime startOfWeek = today.AddDays(-1 * diff);
            List<DateTime> weekDays = new List<DateTime>();
            for (int i = 0; i < 7; i++) { weekDays.Add(startOfWeek.AddDays(i)); }
            ViewBag.WeekDays = weekDays;

            var registered = _db.DangKyCaLams
                .Where(x => x.MaNV == maNV
                         && x.NgayLam >= startOfWeek
                         && x.NgayLam <= weekDays.Last())
                .Select(x => new { MaCa = x.MaCa, NgayLam = x.NgayLam.ToString("yyyy-MM-dd") })
                .ToList();

            ViewBag.RegisteredShifts = registered;
            // Đếm số người đã đăng ký cho từng ca trong tuần hiện tại
            var shiftCounts = _db.DangKyCaLams
                .Where(x => x.NgayLam >= startOfWeek && x.NgayLam <= weekDays.Last())
                // Nhóm theo Mã Ca và Ngày Làm
                .GroupBy(x => new { x.MaCa, x.NgayLam.Date })
                .Select(g => new {
                    // Tạo một Key độc nhất (Ví dụ: "1_2024-04-20")
                    Key = g.Key.MaCa + "_" + g.Key.Date.ToString("yyyy-MM-dd"),
                    Count = g.Count()
                })
                // Chuyển thành Dictionary để ngoài View tra cứu cho nhanh
                .ToDictionary(x => x.Key, x => x.Count);

            ViewBag.ShiftCounts = shiftCounts;
            return View(loaiCas);
        }

        // ==========================================
        // ĐÃ CẬP NHẬT LOGIC ĐĂNG KÝ CA LÀM
        // ==========================================
        [HttpPost]
        public IActionResult DangKyCa(List<string> selectedShifts)
        {
            int maNV = GetMaNV();
            if (maNV == 0) return RedirectToAction("Login", "Account");

            if (selectedShifts == null || selectedShifts.Count == 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn ít nhất một ca làm!";
                return RedirectToAction("DatCaLam");
            }

            List<string> thongBaoLoi = new List<string>();
            List<DangKyCaLam> validNewShifts = new List<DangKyCaLam>(); // Chứa các ca hợp lệ chuẩn bị lưu
            Dictionary<DateTime, int> shiftsPerDay = new Dictionary<DateTime, int>(); // Đếm số ca theo ngày

            // Xác định Tuần hiện tại (dựa vào ca đầu tiên user gửi lên)
            var firstShiftDate = DateTime.Parse(selectedShifts.First().Split('|')[1]).Date;
            int diff = (7 + (firstShiftDate.DayOfWeek - DayOfWeek.Monday)) % 7;
            DateTime startOfWeek = firstShiftDate.AddDays(-1 * diff);
            DateTime endOfWeek = startOfWeek.AddDays(6);

            foreach (var item in selectedShifts)
            {
                var parts = item.Split('|');
                int maCa = int.Parse(parts[0]);
                DateTime ngayLam = DateTime.Parse(parts[1]).Date;

                var caLam = _db.CaLams.Find(maCa);
                if (caLam == null) continue;

                // 1. Kiểm tra NV này đã đăng ký ca này vào ngày này chưa?
                bool daTonTai = _db.DangKyCaLams.Any(x => x.MaNV == maNV && x.MaCa == maCa && x.NgayLam.Date == ngayLam);
                if (daTonTai) continue;

                // 2. Kiểm tra giới hạn: 1 ngày KHÔNG ĐƯỢC QUÁ 2 CA
                if (!shiftsPerDay.ContainsKey(ngayLam))
                {
                    // Lấy số ca đã đăng ký trước đó trong DB của ngày này
                    shiftsPerDay[ngayLam] = _db.DangKyCaLams.Count(x => x.MaNV == maNV && x.NgayLam.Date == ngayLam);
                }

                if (shiftsPerDay[ngayLam] >= 2)
                {
                    thongBaoLoi.Add($"Ngày {ngayLam:dd/MM} không được đăng ký quá 2 ca.");
                    continue; // Bỏ qua không lưu ca này
                }

                // 3. Kiểm tra Full Slot
                int soNguoiDaDangKy = _db.DangKyCaLams.Count(x => x.MaCa == maCa && x.NgayLam.Date == ngayLam);
                if (soNguoiDaDangKy >= caLam.SoLuongToiDa)
                {
                    thongBaoLoi.Add($"Ca {caLam.TenCa} ngày {ngayLam:dd/MM} đã đủ người.");
                    continue;
                }

                // Nếu thỏa mãn tất cả -> Thêm vào danh sách chờ lưu và tăng biến đếm ngày lên 1
                var dangKy = new DangKyCaLam
                {
                    MaNV = maNV,
                    MaCa = maCa,
                    NgayLam = ngayLam,
                    TrangThai = "Pending"
                };
                validNewShifts.Add(dangKy);
                shiftsPerDay[ngayLam]++;
            }

            // 4. KIỂM TRA ĐIỀU KIỆN TUẦN: PHẢI CÓ ÍT NHẤT 7 CA / TUẦN
            // Tổng số ca = Số ca đã có sẵn trong tuần + Số ca hợp lệ vừa chọn
            int soCaDaCoTrongTuan = _db.DangKyCaLams.Count(x => x.MaNV == maNV && x.NgayLam >= startOfWeek && x.NgayLam <= endOfWeek);
            int tongSoCaTuanNay = soCaDaCoTrongTuan + validNewShifts.Count;

            if (tongSoCaTuanNay < 7)
            {
                // Báo lỗi và HUỶ BỎ TOÀN BỘ thao tác (không SaveChanges)
                string errorDetail = thongBaoLoi.Any() ? " (Một số ca bạn chọn bị loại vì: " + string.Join(", ", thongBaoLoi) + ")" : "";
                TempData["ErrorMessage"] = $"Đăng ký thất bại! Yêu cầu đăng ký tối thiểu 7 ca/tuần. Tổng số ca hiện tại của bạn trong tuần mới đạt {tongSoCaTuanNay}/7 ca.{errorDetail}";

                return RedirectToAction("DatCaLam");
            }

            // 5. Nếu đủ số ca (>= 7), tiến hành lưu các ca hợp lệ vào Database
            if (validNewShifts.Any())
            {
                _db.DangKyCaLams.AddRange(validNewShifts);
                _db.SaveChanges();
            }

            // Trả thông báo
            if (thongBaoLoi.Any())
            {
                TempData["SuccessMessage"] = "Đăng ký thành công! Nhưng có vài cảnh báo: " + string.Join(", ", thongBaoLoi);
            }
            else
            {
                TempData["SuccessMessage"] = "Đăng ký thành công!";
            }

            return RedirectToAction("DatCaLam");
        }

        public IActionResult QuanLyPhong(string searchString, RoomStatus? statusFilter)
        {
            var rooms = _db.Rooms.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                rooms = rooms.Where(r => r.SoPhong.ToString().Contains(searchString));
            }

            if (statusFilter.HasValue)
            {
                rooms = rooms.Where(r => r.TrangThai == statusFilter.Value);
            }

            ViewBag.CurrentSearch = searchString;
            ViewBag.CurrentStatus = statusFilter;

            return View(rooms.ToList());
        }

        [HttpPost]
        public IActionResult CapNhatTrangThaiPhong(int soPhong, string trangThaiMoi)
        {
            var phong = _db.Rooms.Find(soPhong);
            if (phong != null)
            {
                phong.TrangThai = trangThaiMoi switch
                {
                    "Trống" => RoomStatus.Available,
                    "Đang ở" => RoomStatus.Occupied,
                    "Đang dọn dẹp" => RoomStatus.Cleaning,
                    "Bảo trì" => RoomStatus.Maintenance,
                    _ => phong.TrangThai
                };

                _db.SaveChanges();
            }
            return RedirectToAction("QuanLyPhong");
        }

        public async Task<IActionResult> QuanLyKhachHang()
        {
            var bookings = await _db.Bookings
                .Include(b => b.KhachHang)
                .Include(b => b.Room)
                .OrderByDescending(b => b.NgayDat)
                .ToListAsync();

            return View(bookings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DuyetBooking(int id)
        {
            var booking = await _db.Bookings.FindAsync(id);
            if (booking == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn đặt phòng!";
                return RedirectToAction("QuanLyKhachHang");
            }

            if (booking.TrangThaiDat == BookingStatus.ChoXacNhan)
            {
                booking.TrangThaiDat = BookingStatus.DaXacNhan;
                _db.Update(booking);
                await _db.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã duyệt thành công đơn hàng #{id}!";
            }

            return RedirectToAction("QuanLyKhachHang");
        }
    }
}
