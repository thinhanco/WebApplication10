using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication10.Models
{
    [Table("DangKyCaLams")]
    public class DangKyCaLam
    {
        [Key]
        [Column("MaDK")]
        public int MaDK { get; set; }

        [Required]
        [Column("MaNV")]
        public int MaNV { get; set; }

        [Required]
        [Column("MaCa")]
        public int MaCa { get; set; }

        [Required]
        [Column("NgayLam")]
        [Display(Name = "Ngày làm")]
        [DataType(DataType.Date)]
        public DateTime NgayLam { get; set; }

        [Column("TrangThai")]
        [StringLength(20)]
        public string TrangThai { get; set; } = "Pending";

        [StringLength(200)]
        [Display(Name = "Ghi chú")]
        public string? GhiChu { get; set; }

        [ForeignKey("MaNV")]
        public virtual NhanVien? NhanVien { get; set; }

        [ForeignKey("MaCa")]
        public virtual CaLam? CaLam { get; set; }
    }
}
