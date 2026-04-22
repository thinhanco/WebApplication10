using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication10.Models
{
    [Table("NhanViens")]
    public class NhanVien
    {
        [Key]
        [Column("MaNV")]
        public int MaNV { get; set; }
        public int AccountID { get; set; }
        [Required(ErrorMessage = "Họ tên không được để trống")]
        [StringLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự")]
        [Display(Name = "Họ và tên")]
        [Column("HoTen")]
        public string HoTen { get; set; } = string.Empty;

        [Display(Name = "Chức vụ")]
        [Column("MaChucVu")]
        public int? MaChucVu { get; set; }

        [ForeignKey("MaChucVu")]
        [Display(Name = "Chức vụ")]
        public virtual ChucVu? ChucVu { get; set; }
        public virtual Account Account { get; set; }
    }
}
