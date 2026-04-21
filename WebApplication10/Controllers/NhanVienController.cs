using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using WebApplication10.Models;
using System.Linq;
using Microsoft.AspNetCore.Authorization; // Phải có thư viện này

namespace WebApplication10.Controllers
{
    // 1. Thêm cái này để ASP.NET tự bảo vệ trang, không cần dùng if(Session...) nữa
    [Authorize(Roles = "NhanVien")]
    public class NhanVienController : Controller
    {
        private readonly QuanLyKhachSanDB _db;

        public NhanVienController(QuanLyKhachSanDB db)
        {
            _db = db;
        }

        // Hàm phụ để lấy MaNV từ Username trong Cookie (Viết 1 lần dùng cho nhiều action)
        private int GetMaNV()
        {
            var userName = User.Identity?.Name;
            // Tìm nhân viên trong DB dựa trên username
            var nv = _db.Accounts.Include(a => a.UserProfile)
                         .FirstOrDefault(a => a.Username == userName);
            return nv?.AccountID ?? 0; // Trả về Id của Account hoặc Profile tùy DB bạn thiết kế
        }

        public IActionResult Index()
        {
            ViewBag.HoTen = User.Identity?.Name;
            return View();
        }

        public IActionResult DatCaLam()
        {
            // THAY THẾ: Không dùng Session nữa
            int maNV = GetMaNV();

            if (maNV == 0)
            {
                return RedirectToAction("Login", "Account");
            }

            var loaiCas = _db.CaLams.ToList();
            // ... (Giữ nguyên đoạn tính toán ngày tháng bên dưới)

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
            return View(loaiCas);
        }

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

            List<string> thongBaoLoi = new List<string>(); // Lưu danh sách các ca bị đầy

            foreach (var item in selectedShifts)
            {
                var parts = item.Split('|');
                int maCa = int.Parse(parts[0]);
                DateTime ngayLam = DateTime.Parse(parts[1]);

                // 1. LẤY THÔNG TIN CA LÀM (Để biết số lượng tối đa)
                var caLam = _db.CaLams.Find(maCa);
                if (caLam == null) continue;

                // 2. ĐẾM SỐ NGƯỜI ĐÃ ĐĂNG KÝ TRONG NGÀY ĐÓ CHO CA ĐÓ
                int soNguoiDaDangKy = _db.DangKyCaLams.Count(x => x.MaCa == maCa && x.NgayLam.Date == ngayLam.Date);

                // 3. KIỂM TRA ĐÃ ĐĂNG KÝ CHƯA (Tránh trùng lặp cho chính NV đó)
                bool daTonTai = _db.DangKyCaLams.Any(x => x.MaNV == maNV && x.MaCa == maCa && x.NgayLam.Date == ngayLam.Date);

                if (daTonTai)
                {
                    continue; // Nếu NV này đã đăng ký rồi thì bỏ qua ca này
                }

                // 4. KIỂM TRA FULL SLOT
                if (soNguoiDaDangKy >= caLam.SoLuongToiDa)
                {
                    // Nếu đã đầy, thêm vào danh sách lỗi để thông báo sau
                    thongBaoLoi.Add($"Ca {caLam.TenCa} ngày {ngayLam.ToString("dd/MM")} đã đủ người ({caLam.SoLuongToiDa}/{caLam.SoLuongToiDa})");
                    continue; // Bỏ qua không lưu ca này
                }

                // 5. NẾU CÒN CHỖ THÌ LƯU
                var dangKy = new DangKyCaLam
                {
                    MaNV = maNV,
                    MaCa = maCa,
                    NgayLam = ngayLam.Date,
                    TrangThai = "Pending"
                };
                _db.DangKyCaLams.Add(dangKy);
            }

            _db.SaveChanges();

            // Xử lý thông báo trả về
            if (thongBaoLoi.Any())
            {
                TempData["ErrorMessage"] = "Một số ca không thể đăng ký: " + string.Join(", ", thongBaoLoi);
            }
            else
            {
                TempData["SuccessMessage"] = "Đăng ký thành công!";
            }

            return RedirectToAction("DatCaLam");
        }
        public IActionResult QuanLyPhong(string searchString, RoomStatus? statusFilter)
        {
            // 1. Lấy tất cả danh sách dưới dạng IQueryable để lọc dần dần
            var rooms = _db.Rooms.AsQueryable();

            // 2. Tìm kiếm theo Số phòng (nếu người dùng nhập)
            if (!string.IsNullOrEmpty(searchString))
            {
                // Vì SoPhong là kiểu int, ta chuyển về string để tìm kiếm chứa (Contains)
                // hoặc dùng == nếu muốn tìm chính xác số phòng
                rooms = rooms.Where(r => r.SoPhong.ToString().Contains(searchString));
            }

            // 3. Lọc theo Trạng thái (nếu người dùng chọn)
            if (statusFilter.HasValue)
            {
                rooms = rooms.Where(r => r.TrangThai == statusFilter.Value);
            }

            // Gửi lại giá trị tìm kiếm để hiển thị trên input sau khi Load lại trang
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
                // Ánh xạ từ chuỗi tiếng Việt nhận được từ giao diện sang Enum tiếng Anh trong Model
                phong.TrangThai = trangThaiMoi switch
                {
                    "Trống" => RoomStatus.Available,
                    "Đang ở" => RoomStatus.Occupied,
                    "Đang dọn dẹp" => RoomStatus.Cleaning,
                    "Bảo trì" => RoomStatus.Maintenance,
                    _ => phong.TrangThai // Nếu không khớp cái nào thì giữ nguyên trạng thái cũ
                };

                _db.SaveChanges();
            }
            return RedirectToAction("QuanLyPhong");
        }

        // Action 1: Trang duyệt đơn đặt phòng
        public async Task<IActionResult> QuanLyKhachHang()
        {
            // Phải có .Include để lấy thông tin khách hàng và phòng, nếu không KhachHang và Room sẽ bị NULL
            var bookings = await _db.Bookings
                .Include(b => b.KhachHang)
                .Include(b => b.Room)
                .OrderByDescending(b => b.NgayDat)
                .ToListAsync();

            return View(bookings); // <--- ĐẢM BẢO CÓ BIẾN bookings Ở ĐÂY
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


        // Action 2: Trang quản lý phòng

    }
}
