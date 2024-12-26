using System;
using Microsoft.EntityFrameworkCore;

namespace HuongDichVu.Entities
{
    public partial class web_dataContext : DbContext
    {
        public web_dataContext()
        {
        }

        public web_dataContext(DbContextOptions<web_dataContext> options)
            : base(options)
        {
        }

        // DbSets cho các bảng
        public virtual DbSet<Book> Books { get; set; } = null!;
        public DbSet<DailyViews> DailyViews { get; set; }
        public DbSet<FavoriteBooks> FavoriteBooks { get; set; }

        public DbSet<FeedBack> Feedbacks { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseMySQL("server=localhost;port=3306;user=root;password=Duong1997@;database=web_data");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Auth>(entity =>
            {
                entity.ToTable("auths");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.Password)
                    .HasMaxLength(255)
                    .HasColumnName("password");

                entity.Property(e => e.Role)
                    .HasMaxLength(255)
                    .HasColumnName("role");

                entity.Property(e => e.Username)
                    .HasMaxLength(255)
                    .HasColumnName("username");
            });

            modelBuilder.Entity<Book>(entity =>
            {
                entity.ToTable("books");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.Description)
                    .HasColumnType("text")
                    .HasColumnName("description");

                entity.Property(e => e.Genre)
                    .HasMaxLength(255)
                    .HasColumnName("genre");

                entity.Property(e => e.ImgSrc)
                    .HasMaxLength(255)
                    .HasColumnName("img_src");

                entity.Property(e => e.status)
                    .HasMaxLength(255)
                    .HasColumnName("status");

                entity.Property(e => e.viewCount).HasColumnName("viewCount");

                entity.Property(e => e.Price)
                    .HasPrecision(10)
                    .HasColumnName("price");

                entity.Property(e => e.StarRating)
                    .HasMaxLength(20)
                    .HasColumnName("star_rating");

                entity.Property(e => e.Title)
                    .HasMaxLength(255)
                    .HasColumnName("title");

                entity.Property(e => e.Upc)
                    .HasMaxLength(255)
                    .HasColumnName("upc");

                entity.Property(e => e.Author)
                    .HasMaxLength(255)
                    .HasColumnName("author");
            });

            // Quan hệ giữa FavoriteBooks, Book và User
            modelBuilder.Entity<FavoriteBooks>()
                .HasOne(fb => fb.Book)
                .WithMany(b => b.FavoriteBooks)  // Một sách có thể có nhiều người dùng yêu thích
                .HasForeignKey(fb => fb.BookId);

            modelBuilder.Entity<FavoriteBooks>()
                .HasOne(fb => fb.User)
                .WithMany(u => u.FavoriteBooks)  // Một người dùng có thể yêu thích nhiều sách
                .HasForeignKey(fb => fb.UserId);
            modelBuilder.Entity<FeedBack>(entity =>
            {
                entity.ToTable("feedbacks");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.BookId).HasColumnName("bookId");

                entity.Property(e => e.UserId).HasColumnName("userId");

                entity.Property(e => e.Comment)
                    .HasColumnName("comment")
                    .HasColumnType("text");

                entity.Property(e => e.CreatedAt)
                    .HasColumnName("createdAt")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                // Ràng buộc khóa ngoại và khóa duy nhất
                entity.HasOne(f => f.Book)
                    .WithMany(b => b.Feedbacks)  // Một sách có thể có nhiều feedback
                    .HasForeignKey(f => f.BookId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(f => f.User)
                    .WithMany(u => u.Feedbacks)  // Một người dùng có thể có nhiều feedback
                    .HasForeignKey(f => f.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Ràng buộc duy nhất cho cặp BookId và UserId
                entity.HasIndex(f => new { f.BookId, f.UserId })
                    .IsUnique();
            });
            // Phần mở rộng cho các cấu hình model khác
            OnModelCreatingPartial(modelBuilder);
        }
        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
