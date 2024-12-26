using HuongDichVu.Entities;
using HuongDichVu.Services; // Ensure this is included
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Configuration;
using MySql.EntityFrameworkCore.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Cấu hình Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Cấu hình DbContext (MySQL)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseMySQL(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// Cấu hình DbContext cho `web_dataContext`
builder.Services.AddEntityFrameworkMySQL()
    .AddDbContext<web_dataContext>(options =>
    {
        options.UseMySQL(builder.Configuration.GetConnectionString("DefaultConnection"));
    });

// Register RecommendationService
builder.Services.AddScoped<RecommendationService>();

// Cấu hình CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", builder =>
    {
        builder.WithOrigins(
                "http://localhost:5174",
                "http://localhost:5173",
                "http://localhost:5175",
                "http://localhost:5176"
            )
            .AllowAnyHeader()
            .AllowAnyMethod() 
            .AllowCredentials(); 
    });
});

// Thêm các dịch vụ cần thiết khác
builder.Services.AddControllers();

// Cấu hình Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllersWithViews();

// Cấu hình CookieAuthentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
    });

var app = builder.Build();

// Cấu hình CORS cho ứng dụng
app.UseCors("AllowSpecificOrigin");

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    if (!context.Auths.Any(u => u.Username == "admin"))
    {
        var admin = new Auth
        {
            Email = "adminsever@gmail.com",
            Username = "admin",
            Password = "Admin123",
            Role = "Admin"
        };

        context.Auths.Add(admin);
        context.SaveChanges();
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseSession();
app.UseRouting();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

// Cấu hình các API
app.MapControllers();

app.Run();