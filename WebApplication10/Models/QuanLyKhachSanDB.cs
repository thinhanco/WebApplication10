using Microsoft.EntityFrameworkCore;
using WebApplication10.Models;
using WebApplication10.Models.UserModels;

namespace WebApplication10.Models
{
    public class QuanLyKhachSanDB : DbContext
    {
        public QuanLyKhachSanDB(DbContextOptions<QuanLyKhachSanDB> options) : base(options) { }

        // Models từ phần Nhân viên & Admin
        public DbSet<ChucVu> ChucVus { get; set; }
        public DbSet<NhanVien> NhanViens { get; set; }
        public DbSet<CaLam> CaLams { get; set; }
        public DbSet<DangKyCaLam> DangKyCaLams { get; set; }
        public DbSet<BaoCaoPhong> BaoCaoPhongs { get; set; }

        // Models từ PBL3 (Khách hàng, Đặt phòng)
        public DbSet<Account> Accounts { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<BaseUser> UserProfiles { get; set; }
        public DbSet<KhachHang> KhachHangs { get; set; }
        public DbSet<Booking> Bookings { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. Đặt tên bảng
            modelBuilder.Entity<Account>().ToTable("Accounts");
            modelBuilder.Entity<Room>().ToTable("Rooms");
            modelBuilder.Entity<Booking>().ToTable("Bookings");
            modelBuilder.Entity<NhanVien>().ToTable("NhanViens");
            modelBuilder.Entity<ChucVu>().ToTable("ChucVus");
            modelBuilder.Entity<CaLam>().ToTable("CaLams");
            modelBuilder.Entity<DangKyCaLam>().ToTable("DangKyCaLams");
            modelBuilder.Entity<BaoCaoPhong>().ToTable("BaoCaoPhong");

            // Cấu hình kế thừa cho UserProfiles (TPH)
            modelBuilder.Entity<BaseUser>().ToTable("UserProfiles");
            // KhachHang kế thừa từ BaseUser nên không cần gọi .ToTable nữa, EF sẽ tự hiểu

            // 2. Cấu hình quan hệ Account - NhanVien (1-1)
            modelBuilder.Entity<NhanVien>()
                .HasOne(nv => nv.Account)
                .WithOne(a => a.NhanVien)
                .HasForeignKey<NhanVien>(nv => nv.AccountID); // Dùng AccountID trong bảng NhanVien làm khóa ngoại

            // 3. Cấu hình quan hệ Account - UserProfile (1-1)
            modelBuilder.Entity<BaseUser>()
                .HasOne(up => up.Account)
                .WithOne(a => a.UserProfile)
                .HasForeignKey<BaseUser>(up => up.AccountID); // Dùng AccountID trong bảng UserProfile làm khóa ngoại

            // 4. Cấu hình Booking - KhachHang
            // 4. Cấu hình Booking - KhachHang
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.KhachHang)
                .WithMany()
                .HasForeignKey(b => b.MaKhachHang)  // ✅ Đúng tên property
                .OnDelete(DeleteBehavior.Restrict);

            // 5. Chuyển Enum thành string
            modelBuilder.Entity<Account>().Property(a => a.Role).HasConversion<string>();
            modelBuilder.Entity<Room>().Property(r => r.LoaiPhong).HasConversion<string>();
            modelBuilder.Entity<Room>().Property(r => r.TrangThai).HasConversion<string>();
            modelBuilder.Entity<Booking>().Property(b => b.TrangThaiDat).HasConversion<string>();
        }
    }
}
