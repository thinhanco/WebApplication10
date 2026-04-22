using Microsoft.EntityFrameworkCore;
using WebApplication10.Models;
// 1. THÊM DÒNG NÀY
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Thêm dịch vụ MVC
builder.Services.AddControllersWithViews();

// 2. THÊM CẤU HÌNH AUTHENTICATION TẠI ĐÂY
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login"; // Chưa đăng nhập thì vào đây (Lỗi 401)
        options.AccessDeniedPath = "/Account/AccessDenied"; // Đăng nhập rồi nhưng sai quyền thì vào đây (Lỗi 403)
    });

// Cấu hình Database
builder.Services.AddDbContext<QuanLyKhachSanDB>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSession();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Auto-seed sample accounts if none exist (useful for development)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var db = services.GetRequiredService<QuanLyKhachSanDB>();
        if (!db.Accounts.Any())
        {
            db.Accounts.AddRange(
                new WebApplication10.Models.Account { Username = "admin", Password = "123", Role = WebApplication10.Models.UserRole.QuanTriVien, Email = "admin@gmail.com" },
                new WebApplication10.Models.Account { Username = "khach1", Password = "123", Role = WebApplication10.Models.UserRole.KhachHang, Email = "khachhang@gmail.com" }
            );
            db.SaveChanges();
        }
    }
    catch (Exception ex)
    {
        // swallow exceptions here for dev convenience; in production log properly
        Console.WriteLine($"Seeding error: {ex.Message}");
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// 3. THÊM DÒNG NÀY (BẮT BUỘC PHẢI NẰM TRÊN UseAuthorization)
app.UseAuthentication();
app.UseAuthorization();

app.UseSession(); // Đưa UseSession xuống dưới cũng được

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
