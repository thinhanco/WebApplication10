using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication10.Models
{
    [Table("NhanViens")]
    public class NhanVien
    {
        [Key]
        public int MaNV { get; set; }
        public string HoTen { get; set; } = string.Empty;
        public int? MaChucVu { get; set; }

        [ForeignKey("MaChucVu")]
        public virtual ChucVu? ChucVu { get; set; }

        // Sửa dòng này để khớp với tên lớp ở Bước 1
        public virtual TaiKhoans? TaiKhoan { get; set; }
    }
}