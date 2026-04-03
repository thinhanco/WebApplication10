using Microsoft.EntityFrameworkCore;

namespace WebApplication10.Models
{
    public class QuanLyKhachSanDB : DbContext
    {
        // Hàm khởi tạo bắt buộc cho ASP.NET Core
        public QuanLyKhachSanDB(DbContextOptions<QuanLyKhachSanDB> options) : base(options)
        {
        }

        // --- ĐĂNG KÝ CÁC BẢNG (DbSet) ---
        // Lưu ý: Tên ở đây nên khớp với tên biến bạn gọi trong Controller (_db.TaiKhoans)
        public DbSet<ChucVu> ChucVus { get; set; }
        public DbSet<NhanVien> NhanViens { get; set; }
        public DbSet<TaiKhoans> TaiKhoans { get; set; }
        public DbSet<Phong> Phongs { get; set; }
        public DbSet<CaLam> CaLams { get; set; }
        public DbSet<DangKyCaLam> DangKyCaLams { get; set; }
        public DbSet<BaoCaoPhong> BaoCaoPhongs { get; set; }

        // --- CẤU HÌNH LIÊN KẾT BẢNG (Fluent API) ---
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Ép buộc EF Core tìm đúng tên bảng đã có chữ 's' trong SQL Server
            // Nếu bạn đặt tên bảng trong SQL là gì thì điền chính xác vào phần ToTable("...")

            modelBuilder.Entity<ChucVu>().ToTable("ChucVus");
            modelBuilder.Entity<NhanVien>().ToTable("NhanViens");
            modelBuilder.Entity<TaiKhoans>().ToTable("TaiKhoans");

            modelBuilder.Entity<Phong>().ToTable("Phongs");
            modelBuilder.Entity<CaLam>().ToTable("CaLams");
            modelBuilder.Entity<DangKyCaLam>().ToTable("DangKyCaLams");
            modelBuilder.Entity<BaoCaoPhong>().ToTable("BaoCaoPhongs");

            // Nếu bạn dùng khóa chính phức tạp hoặc cần cấu hình thêm, viết ở đây
        }
    }
}