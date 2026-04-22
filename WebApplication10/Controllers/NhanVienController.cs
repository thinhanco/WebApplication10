using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApplication10.Models;
using WebApplication10.Models.ViewModels;
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
            if (maNV == 0) return RedirectToAction("Login", "Account");

            // 1. Tính toán ngày trong tuần
            DateTime today = DateTime.Today;
            int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
            DateTime startOfWeek = today.AddDays(-1 * diff);

            var loaiCas = _db.CaLams.ToList();
            var registered = _db.DangKyCaLams
                .Where(x => x.MaNV == maNV && x.NgayLam >= startOfWeek && x.NgayLam <= startOfWeek.AddDays(6))
                .ToList();

            var allShiftCounts = _db.DangKyCaLams
                .Where(x => x.NgayLam >= startOfWeek && x.NgayLam <= startOfWeek.AddDays(6))
                .GroupBy(x => new { x.MaCa, x.NgayLam.Date })
                .ToDictionary(g => g.Key.MaCa + "_" + g.Key.Date.ToString("yyyy-MM-dd"), g => g.Count());

            // 2. TÁCH LOGIC: Tạo danh sách ViewModel
            var viewModel = new List<DatCaLamViewModel>();

            for (int i = 0; i < 7; i++)
            {
                var currentDay = startOfWeek.AddDays(i);
                var dayModel = new DatCaLamViewModel
                {
                    Date = currentDay,
                    DayOfWeekName = currentDay.ToString("dddd"),
                    Shifts = new List<ShiftItemViewModel>()
                };

                foreach (var shift in loaiCas)
                {
                    string key = $"{shift.MaCa}_{currentDay:yyyy-MM-dd}";
                    int count = allShiftCounts.ContainsKey(key) ? allShiftCounts[key] : 0;

                    dayModel.Shifts.Add(new ShiftItemViewModel
                    {
                        MaCa = shift.MaCa,
                        TenCa = shift.TenCa,
                        GioLam = $"{shift.GioBatDau:hh\\:mm} - {shift.GioKetThuc:hh\\:mm}",
                        NgayLamStr = currentDay.ToString("yyyy-MM-dd"),
                        IsRegistered = registered.Any(r => r.MaCa == shift.MaCa && r.NgayLam.Date == currentDay.Date),
                        IsFull = count >= shift.SoLuongToiDa,
                        CurrentCount = count,
                        MaxCount = shift.SoLuongToiDa
                    });
                }
                viewModel.Add(dayModel);
            }

            return View(viewModel); // Truyền trực tiếp ViewModel vào View
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
            // 1. Lấy dữ liệu dưới dạng IQueryable để lọc
            var roomsQuery = _db.Rooms.AsQueryable();

            // 2. Thực hiện lọc (Giữ nguyên logic cũ)
            if (!string.IsNullOrEmpty(searchString))
            {
                roomsQuery = roomsQuery.Where(r => r.SoPhong.ToString().Contains(searchString));
            }

            if (statusFilter.HasValue)
            {
                roomsQuery = roomsQuery.Where(r => r.TrangThai == statusFilter.Value);
            }

            // 3. TÁCH LOGIC: Chuyển đổi sang ViewModel ngay tại đây
            var viewModel = roomsQuery.Select(p => new QuanLyPhongViewModel
            {
                SoPhong = p.SoPhong,
                LoaiPhong = p.LoaiPhong.ToString(),

                // Chuyển Enum sang Tiếng Việt để hiển thị
                TrangThaiDisplay = p.TrangThai == RoomStatus.Available ? "Trống" :
                                   p.TrangThai == RoomStatus.Occupied ? "Đang ở" :
                                   p.TrangThai == RoomStatus.Cleaning ? "Đang dọn dẹp" : "Bảo trì",

                // Quyết định màu sắc Badge ngay tại Controller
                BadgeClass = p.TrangThai == RoomStatus.Available ? "bg-success text-white" :
                             p.TrangThai == RoomStatus.Occupied ? "bg-danger text-white" :
                             p.TrangThai == RoomStatus.Cleaning ? "bg-warning text-dark" : "bg-secondary text-white"
            }).ToList();

            // 4. Gửi dữ liệu tìm kiếm cũ về View để hiển thị lại trên thanh search
            ViewBag.CurrentSearch = searchString;
            ViewBag.CurrentStatus = statusFilter;

            // 5. Trả về View với danh sách ViewModel thay vì danh sách Room
            return View(viewModel);
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
                .OrderByDescending(b => b.NgayDat)
                .ToListAsync();

            // CHUYỂN ĐỔI SANG VIEWMODEL TẠI ĐÂY
            var viewModel = bookings.Select(b => new QuanLyKhachHangViewModel
            {
                BookingID = b.BookingID,
                TenKhachHang = b.KhachHang?.Hoten ?? "N/A",
                SoDienThoai = b.KhachHang?.sđt ?? "",
                SoPhong = b.SoPhong.ToString(),
                NgayDen = b.CheckIn.ToString("dd/MM/yyyy"),
                ChoPhepDuyet = (b.TrangThaiDat == BookingStatus.ChoXacNhan),

                // Logic hiển thị badge tách khỏi View
                TrangThaiHienThi = b.TrangThaiDat == BookingStatus.ChoXacNhan ? "Chờ nhận phòng" :
                                   b.TrangThaiDat == BookingStatus.DaXacNhan ? "Đã nhận phòng" : "Khác",
                BadgeClass = b.TrangThaiDat == BookingStatus.ChoXacNhan ? "bg-warning text-dark" :
                             b.TrangThaiDat == BookingStatus.DaXacNhan ? "bg-success" : "bg-secondary"
            }).ToList();

            return View(viewModel);
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
