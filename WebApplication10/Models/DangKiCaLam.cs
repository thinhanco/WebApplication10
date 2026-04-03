using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WebApplication10.Models;

public class DangKyCaLam
{
    [Key]
    public int MaDK { get; set; }
    public int MaNV { get; set; }
    public int MaCa { get; set; }
    public DateTime NgayLam { get; set; }

    public string TrangThai { get; set; } = "Pending"; // Gán giá trị mặc định
    public string? GhiChu { get; set; } // Thêm dấu ?

    [ForeignKey("MaNV")]
    public virtual NhanVien? NhanVien { get; set; } // Thêm dấu ?
    [ForeignKey("MaCa")]
    public virtual CaLam? CaLam { get; set; } // Thêm dấu ?
}