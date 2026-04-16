using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication10.Models
{
    [Table("ChucVus")]
    public class ChucVu
    {
        [Key]
        [Column("MaChucVu")]
        public int MaChucVu { get; set; }

        [Required(ErrorMessage = "Tên chức vụ không được để trống")]
        [StringLength(50, ErrorMessage = "Tên chức vụ tối đa 50 ký tự")]
        [Display(Name = "Tên chức vụ")]
        [Column("TenChucVu")]
        public string TenChucVu { get; set; } = string.Empty;

        public virtual ICollection<NhanVien> NhanViens { get; set; } = new List<NhanVien>();
    }
}
