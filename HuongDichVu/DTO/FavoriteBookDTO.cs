namespace HuongDichVu.DTO
{
    public class FavoriteBookDTO
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int BookId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
