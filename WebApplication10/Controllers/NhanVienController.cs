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
        public IActionResult DatCaLam()
        {
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
            return View(loaiCas);
        }

        [HttpPost]
        public IActionResult DangKyCa(int maCa, DateTime ngayLam, string ghiChu)
        {
            // FR-EMP-SHIFT-06: Lưu thông tin đăng ký ca làm (Trạng thái mặc định: Chờ duyệt)
            // (Lưu ý: Bạn cần tạo bảng DangKyCaLam trong Models trước)
            return RedirectToAction("Index");
        }

        // --- CHỨC NĂNG 2: QUẢN LÝ PHÒNG (SRS 3.2.3.2) ---
        public IActionResult QuanLyPhong()
        {
            // Lấy danh sách phòng để nhân viên theo dõi trạng thái (SRS FR-RM-01)
            var danhSachPhong = _db.Phongs.ToList();
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