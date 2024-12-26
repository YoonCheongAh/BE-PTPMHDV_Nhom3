using HuongDichVu.DTO;
using HuongDichVu.Entities;
using Microsoft.EntityFrameworkCore;

namespace HuongDichVu.Services
{
    public class RecommendationService
    {
        private readonly web_dataContext _context;

        public RecommendationService(web_dataContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }
        public async Task<List<BookDTO>> GetRecommendedBooks(int userId)
        {
            var favoriteBooks = await _context.FavoriteBooks
                .Where(fb => fb.UserId == userId)
                .Include(fb => fb.Book)
                .ToListAsync();

            List<BookDTO> recommendedBooks = new List<BookDTO>();
            if (favoriteBooks.Any())
            {
                var genres = favoriteBooks.Select(fb => fb.Book.Genre).Distinct().ToList();
                recommendedBooks = await _context.Books
                    .Where(book => genres.Contains(book.Genre))
                    .OrderByDescending(book => book.viewCount)
                    .ThenByDescending(book => book.StarRating)
                    .Select(book => new BookDTO
                    {
                        Id = book.Id,
                        Title = book.Title,
                        Author = book.Author,
                        Genre = book.Genre,
                        Price = book.Price,
                        ImgSrc = book.ImgSrc,
                        StarRating = book.StarRating,
                        Status = book.status,
                        ViewCount = book.viewCount,
                        Description = book.Description
                    })
                    .ToListAsync();
            }
            else
            {
                recommendedBooks = await _context.Books
                    .OrderByDescending(book => book.viewCount)
                    .ThenByDescending(book => book.StarRating)
                    .Select(book => new BookDTO
                    {
                        Id = book.Id,
                        Title = book.Title,
                        Author = book.Author,
                        Genre = book.Genre,
                        Price = book.Price,
                        ImgSrc = book.ImgSrc,
                        StarRating = book.StarRating,
                        Status = book.status,
                        ViewCount = book.viewCount,
                        Description = book.Description
                    })
                    .ToListAsync();
            }

            return recommendedBooks;
        }
    }
}
