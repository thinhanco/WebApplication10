using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication10.Models
{
    [Table("TaiKhoans")]
    public class TaiKhoans
    {
        [Key]
        public string TenDangNhap { get; set; }
        public string MatKhau { get; set; }
        public int MaNV { get; set; }

        // Dòng này để sửa lỗi Error 1 & 2
        public bool IsLocked { get; set; } = false;

        [ForeignKey("MaNV")]
        public virtual NhanVien NhanVien { get; set; }
    }
}