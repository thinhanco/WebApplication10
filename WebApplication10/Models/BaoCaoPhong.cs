using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication10.Models
{
    [Table("BaoCaoPhong")]
    public class BaoCaoPhong
    {
        [Key]
        public int MaBaoCao { get; set; }
        public int SoPhong { get; set; }
        public string NoiDungBaoCao { get; set; }
        public DateTime NgayBaoCao { get; set; }
        public string TinhTrangSauQuanLy { get; set; }
    }
}