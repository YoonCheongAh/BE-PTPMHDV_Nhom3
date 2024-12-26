using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace HuongDichVu.Entities
{
    public class FavoriteBooks
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int UserId { get; set; }  // Khóa ngoại đến bảng người dùng
        public int BookId { get; set; }  // Khóa ngoại đến bảng sách
        public DateTime CreatedAt { get; set; }

        public virtual Auth User { get; set; }  // Quan hệ đến bảng User
        public virtual Book Book { get; set; }  // Quan hệ đến bảng Book 
    }
}
