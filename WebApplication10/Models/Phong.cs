using System.ComponentModel.DataAnnotations;

namespace WebApplication10.Models
{
    public class Phong
    {
        [Key]
        public int SoPhong { get; set; }
        public string LoaiPhong { get; set; }
        public string TrangThai { get; set; }
        public double GiaPhong { get; set; }
    }
}