using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using WebApplication10.Models; // Đảm bảo đúng namespace Models của bạn
using System.Linq;

namespace WebApplication10.Controllers
{
    public class NhanVienController : Controller
    {
        private readonly QuanLyKhachSanDB _db;

        // Tiêm Database vào Controller
        public NhanVienController(QuanLyKhachSanDB db)
        {
            _db = db;
        }

        // --- TRANG CHỦ NHÂN VIÊN ---
        public IActionResult Index()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserName")))
            {
                return RedirectToAction("Login", "Account");
            }
            ViewBag.HoTen = HttpContext.Session.GetString("UserName");
            return View();
        }

        // --- CHỨC NĂNG 1: ĐẶT CA LÀM (SRS 3.2.3.1) ---
        // --- CHỨC NĂNG 1: ĐẶT CA LÀM (SRS 3.2.3.1) ---
        public IActionResult DatCaLam()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserName")))
            {
                return RedirectToAction("Login", "Account");
            }

            int maNV = HttpContext.Session.GetInt32("MaNV") ?? 0;

            // 1. Lấy danh sách các loại ca (Sáng, Chiều, Tối)
            var loaiCas = _db.CaLams.ToList();

            // 2. Tính toán các ngày trong tuần hiện tại (Từ Thứ 2 đến Chủ Nhật)
            DateTime today = DateTime.Today;
            int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
            DateTime startOfWeek = today.AddDays(-1 * diff);

            List<DateTime> weekDays = new List<DateTime>();
            for (int i = 0; i < 7; i++)
            {
                weekDays.Add(startOfWeek.AddDays(i));
            }

            ViewBag.WeekDays = weekDays;

            // Lấy danh sách ca đã đăng ký của nhân viên trong tuần này (để tô màu)
            var registered = _db.DangKyCaLams
                .Where(x => x.MaNV == maNV
                         && x.NgayLam >= startOfWeek
                         && x.NgayLam <= weekDays.Last())
                .Select(x => new
                {
                    MaCa = x.MaCa,
                    NgayLam = x.NgayLam.ToString("yyyy-MM-dd")
                })
                .ToList();

            ViewBag.RegisteredShifts = registered;   // Dùng để tô màu trong View (giữ nguyên HTML cũ)

            return View(loaiCas);
        }

        [HttpPost]
        public IActionResult DangKyCa(List<string> selectedShifts)
        {
            int? maNV = HttpContext.Session.GetInt32("MaNV");
            if (maNV == null) return RedirectToAction("Login", "Account");

            if (selectedShifts == null || selectedShifts.Count == 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn ít nhất một ca làm!";
                return RedirectToAction("DatCaLam");
            }

            foreach (var item in selectedShifts)
            {
                // item có dạng "1|2026-04-06"
                var parts = item.Split('|');
                int maCa = int.Parse(parts[0]);
                DateTime ngayLam = DateTime.Parse(parts[1]);

                // Kiểm tra xem đã tồn tại chưa để tránh lỗi DB
                bool daTonTai = _db.DangKyCaLams.Any(x => x.MaNV == maNV && x.MaCa == maCa && x.NgayLam.Date == ngayLam.Date);

                if (!daTonTai)
                {
                    var dangKy = new DangKyCaLam
                    {
                        MaNV = maNV.Value,
                        MaCa = maCa,
                        NgayLam = ngayLam.Date,
                        TrangThai = "Pending"
                    };
                    _db.DangKyCaLams.Add(dangKy);
                }
            }

            _db.SaveChanges();
            TempData["SuccessMessage"] = "Đăng ký thành công các ca đã chọn!";
            return RedirectToAction("DatCaLam");
        }
        // Thêm hàm này vào Controller
        public IActionResult QuanLyPhong()
        {
            // Kiểm tra đăng nhập (giống các hàm khác)
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserName")))
            {
                return RedirectToAction("Login", "Account");
            }

            // LẤY DỮ LIỆU TỪ SQL: Truy vấn danh sách tất cả các phòng
            var danhSachPhong = _db.Phongs.ToList();

            // TRẢ VỀ VIEW: Gửi danh sách phòng qua file QuanLyPhong.cshtml
            return View(danhSachPhong);
        }
        [HttpPost]
        public IActionResult CapNhatTrangThaiPhong(int soPhong, string trangThaiMoi)
        {
            var phong = _db.Phongs.Find(soPhong);
            if (phong != null)
            {
                phong.TrangThai = trangThaiMoi; // Ví dụ: Trống -> Đang dọn (SRS FR-RM-10)
                _db.SaveChanges();
            }
            return RedirectToAction("QuanLyPhong");
        }

        // --- CHỨC NĂNG 3: QUẢN LÝ KHÁCH HÀNG (SRS 3.2.3.3) ---
        public IActionResult QuanLyKhachHang()
        {
            // Hiển thị danh sách khách hàng đang chờ nhận phòng (Check-in)
            // (Lưu ý: Cần bảng DatPhong trong Models)
            return View();
        }
    }
}
