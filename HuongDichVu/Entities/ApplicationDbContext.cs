using Microsoft.EntityFrameworkCore;

namespace HuongDichVu.Entities
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext()
        {
        }
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public virtual DbSet<Auth> Auths { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseMySQL("server=localhost;port=3306;user=root;password=Duong1997@;database=web_data");
            }
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Cấu hình cho các thực thể
            modelBuilder.Entity<Auth>().ToTable("Auths"); // Đặt tên bảng tương ứng
            modelBuilder.Entity<Auth>().HasKey(a => a.Id); // Khóa chính

            // Cấu hình các thuộc tính khác của Auth
            modelBuilder.Entity<Auth>().Property(a => a.Username).IsRequired().HasMaxLength(50);
            modelBuilder.Entity<Auth>().Property(a => a.Password).IsRequired();

        }

    }
}
