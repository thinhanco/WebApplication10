using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using WebApplication10.Controllers;

namespace WebApplication10.Models
{
    public class ChucVu
    {
        [Key]
        public int MaChucVu { get; set; }
        public string TenChucVu { get; set; } // "Admin" hoặc "NhanVien"

        // Liên kết với bảng NhanVien
        public virtual ICollection<NhanVien> NhanViens { get; set; }
    }
}