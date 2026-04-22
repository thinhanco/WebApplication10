
namespace WebApplication10.Models.ViewModels
{
    public class QuanLyKhachHangViewModel

    {
        public int BookingID { get; set; }
        public string TenKhachHang { get; set; }
        public string SoDienThoai { get; set; }
        public string SoPhong { get; set; }
        public string NgayDen { get; set; }
        public string TrangThaiHienThi { get; set; }
        public string BadgeClass { get; set; } // bg-warning, bg-success...
        public bool ChoPhepDuyet { get; set; }
    }
}
