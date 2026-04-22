using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WebApplication10.Models.UserModels;

namespace WebApplication10.Models
{
    public enum UserRole
    {
        KhachHang,
        NhanVien,
        QuanTriVien
    }
    public class Account
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int AccountID { get; set; }

        [Required(ErrorMessage = @"Tên đăng nhập không được để trống")]
        [StringLength(50)]
        [Column(TypeName = "nvarchar")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [StringLength(100)]
        [DataType(DataType.Password)]
        [Column(TypeName = "nvarchar")]
        public string Password { get; set; }
        [Required(ErrorMessage = "Quyền truy cập không được để trống")]
        [RegularExpression("^(KhachHang|NhanVien|QuanTriVien)$",
            ErrorMessage = "Role phải là 'KhachHang', 'NhanVien' hoặc 'QuanTriVien'")]
        public UserRole Role { get; set; }
        [Required(ErrorMessage = "Email không được để trống")]
        [Column(TypeName = "nvarchar")]
        [StringLength(100)]
        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@gmail\.com$",
            ErrorMessage = "Email phải có định dạng TenEmail@gmail.com và không chứa dấu cách")]
        public string Email { get; set; }

        public virtual NhanVien NhanVien { get; set; }
        public virtual BaseUser UserProfile { get; set; }
    }

}
