using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication10.Models
{
    [Table("CaLams")]
    public class CaLam
    {
        [Key]
        [Column("MaCa")]
        public int MaCa { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Tên ca")]
        [Column("TenCa")]
        public string TenCa { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Giờ bắt đầu")]
        [Column("GioBatDau")]
        public TimeSpan GioBatDau { get; set; }

        [Required]
        [Display(Name = "Giờ kết thúc")]
        [Column("GioKetThuc")]
        public TimeSpan GioKetThuc { get; set; }

        [Required]
        [Range(1, 50)]
        [Display(Name = "Số lượng tối đa")]
        [Column("SoLuongToiDa")]
        public int SoLuongToiDa { get; set; }
    }
}
