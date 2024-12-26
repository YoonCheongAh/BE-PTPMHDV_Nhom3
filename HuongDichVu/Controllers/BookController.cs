using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using HuongDichVu.DTO;
using HuongDichVu.Entities;
using HuongDichVu.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mysqlx;

namespace WebService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BookController : ControllerBase
    {
        private readonly web_dataContext web_dataContext;
        private readonly RecommendationService _recommendationService;

        public BookController(web_dataContext web_dataContext, RecommendationService recommendationService)
        {
            this.web_dataContext = web_dataContext;
            this._recommendationService = recommendationService;
        }

        private readonly ApplicationDbContext context;
        [HttpGet("GetAllBooks")]
        public async Task<ActionResult<List<BookDTO>>> Get()
        {
            var books = await web_dataContext.Books.Select(
                s => new BookDTO
                {
                    Id = s.Id,
                    Upc = s.Upc,
                    Title = s.Title,
                    Author = s.Author,
                    Genre = s.Genre,
                    Price = s.Price,
                    ImgSrc = s.ImgSrc,
                    StarRating = s.StarRating,
                    Status = s.status,
                    ViewCount = s.viewCount,
                    Description = s.Description
                }
            ).ToListAsync();

            if (books.Count == 0)
            {
                return NotFound("No books found.");
            }

            return Ok(books);
        }
        [HttpGet("GetBookById")]
        public async Task<ActionResult<BookDTO>> GetBookById(int id)
        {
            var isAdmin = HttpContext.Session.GetString("Role") == "Admin";
            // Lấy thông tin sách
            var book = await web_dataContext.Books.FindAsync(id);
            if (book == null)
            {
                return NotFound();
            }

            if (!isAdmin)
            {
                book.viewCount = (book.viewCount ?? 0) + 1;
                var today = DateTime.UtcNow.Date;

                // Cập nhật hoặc thêm lượt xem theo ngày
                var dailyView = await web_dataContext.DailyViews
                    .FirstOrDefaultAsync(v => v.BookId == id && v.ViewDate == today);

                if (dailyView != null)
                {
                    dailyView.ViewCountDay += 1;
                }
                else
                {
                    web_dataContext.DailyViews.Add(new DailyViews
                    {
                        BookId = id,
                        ViewDate = today,
                        ViewCountDay = 1
                    });
                }
                // Lưu thay đổi vào cơ sở dữ liệu
                await web_dataContext.SaveChangesAsync();
            }
            var bookDTO = new BookDTO
            {
                Id = book.Id,
                Upc = book.Upc,
                Title = book.Title,
                Author = book.Author,
                Genre = book.Genre,
                Price = book.Price,
                ImgSrc = book.ImgSrc,
                StarRating = book.StarRating,
                Status = book.status,
                ViewCount = book.viewCount, // Tổng lượt xem
                Description = book.Description
            };
            return Ok(bookDTO);
        }
        [HttpPost("InsertBook")]
        public async Task<IActionResult> InsertBook([FromForm] BookDTO book, IFormFile? file)
        {
            // Đường dẫn ảnh mặc định (lấy từ link hoặc file upload)
            string imgSrc = book.ImgSrc;

            // Nếu người dùng upload file ảnh
            if (file != null && file.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Lưu file với tên unique
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Cập nhật đường dẫn ảnh
                imgSrc = $"/uploads/{fileName}";
            }

            var entity = new Book()
            {
                Upc = book.Upc,
                Title = book.Title,
                Author = book.Author,
                Genre = book.Genre,
                Price = book.Price,
                ImgSrc = imgSrc, // Link ảnh hoặc đường dẫn ảnh upload
                StarRating = book.StarRating,
                status = book.Status,
                viewCount = 0,
                Description = book.Description
            };

            web_dataContext.Books.Add(entity);
            await web_dataContext.SaveChangesAsync();

            return CreatedAtAction(nameof(GetBookById), new { id = entity.Id }, entity);
        }

        [HttpPut("UpdateBook/{id}")]
        public async Task<IActionResult> UpdateBook(int id, [FromForm] BookDTO book, IFormFile? file)
        {
            if (id <= 0)
            {
                return BadRequest("Id không hợp lệ.");
            }

            // Tìm sách cần cập nhật
            var entity = await web_dataContext.Books.FindAsync(id);
            if (entity == null)
            {
                return NotFound("Không tìm thấy sách với Id được cung cấp.");
            }

            // Giữ link ảnh cũ nếu không có link mới hoặc file upload
            string imgSrc = book.ImgSrc ?? entity.ImgSrc;

            // Nếu người dùng upload file mới
            if (file != null && file.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Cập nhật đường dẫn ảnh mới
                imgSrc = $"/uploads/{fileName}";
            }

            // Cập nhật các thông tin sách
            entity.Upc = book.Upc;
            entity.Title = book.Title;
            entity.Author = book.Author;
            entity.Genre = book.Genre;
            entity.Price = book.Price;
            entity.StarRating = book.StarRating;
            entity.status = book.Status;
            entity.Description = book.Description;
            entity.ImgSrc = imgSrc; // Cập nhật ảnh (link hoặc đường dẫn upload)

            await web_dataContext.SaveChangesAsync();
            return Ok("Cập nhật sách thành công.");
        }

        [HttpDelete("DeleteBook/{id}")]
        public async Task<IActionResult> DeleteBook(int id)
        {
            var entity = await web_dataContext.Books.FirstOrDefaultAsync(s => s.Id == id);

            if (entity == null)
            {
                return NotFound();
            }
            // Xóa sách
            web_dataContext.Books.Attach(entity);
            web_dataContext.Books.Remove(entity);
            await web_dataContext.SaveChangesAsync();

            return NoContent();
        }
        [HttpGet("QuickSearch")]
        public async Task<ActionResult<List<BookDTO>>> QuickSearch(string keyword, string? genre)
        {
            if (string.IsNullOrEmpty(keyword))
            {
                return BadRequest("Vui lòng nhập từ khóa tìm kiếm.");
            }
            var query = web_dataContext.Books.AsQueryable();
            query = query.Where(book => book.Title.Contains(keyword));

            // Nếu có thể loại (genre), thêm điều kiện tìm kiếm theo thể loại
            if (!string.IsNullOrEmpty(genre))
            {
                query = query.Where(book => book.Genre.Contains(genre));
            }

            var results = await query
                .Where(book => book.Title.Contains(keyword)) // Tìm kiếm theo tên sách
                .Select(book => new Book
                {
                    Id = book.Id,
                    Upc = book.Upc,
                    Title = book.Title,
                    Author=book.Author,
                    Genre = book.Genre,
                    Price = book.Price,
                    ImgSrc = book.ImgSrc,
                    StarRating = book.StarRating,
                    status = book.status,
                    viewCount = book.viewCount,
                    Description = book.Description
                })
                .Take(10) // Giới hạn số lượng kết quả trả về
                .ToListAsync();

            if (results.Count == 0)
            {
                return NotFound("Không tìm thấy sách phù hợp với từ khóa.");
            }

            return Ok(results);
        }
        // API: Thêm sách vào danh sách yêu thích
        [HttpPost("AddToFavorites")]
        public async Task<IActionResult> AddToFavorites([FromBody] AddToFavoritesRequest request)
        {
            // Lấy UserId từ header
            var userIdString = HttpContext.Request.Headers["UserId"].FirstOrDefault();

            if (string.IsNullOrEmpty(userIdString))
            {
                return Unauthorized(new { message = "Bạn cần phải đăng nhập." });
            }

            var userId = int.Parse(userIdString);  // Chuyển đổi UserId từ string sang int
            var existingFavorite = await web_dataContext.FavoriteBooks
                .FirstOrDefaultAsync(fb => fb.UserId == userId && fb.BookId == request.BookId);

            if (existingFavorite != null)
            {
                return BadRequest(new { message = "Sách này đã có trong danh sách yêu thích." });
            }

            // Thêm sách vào danh sách yêu thích
            var favoriteBook = new FavoriteBooks
            {
                UserId = userId,
                BookId = request.BookId,
                CreatedAt = DateTime.UtcNow
            };

            web_dataContext.FavoriteBooks.Add(favoriteBook);
            await web_dataContext.SaveChangesAsync();

            return Ok(new { message = "Sách đã được thêm vào danh sách yêu thích." });
        }
        // API: Xóa sách khỏi danh sách yêu thích
        [HttpDelete("RemoveFromFavorites/{bookId}")]
        public async Task<IActionResult> RemoveFromFavorites(int bookId)
        {
            var userIdString = HttpContext.Request.Headers["UserId"].FirstOrDefault();

            if (string.IsNullOrEmpty(userIdString))
            {
                return Unauthorized(new
                {
                    message = "Bạn cần phải đăng nhập."
                });
            }

            int userId;
            if (!int.TryParse(userIdString, out userId))
            {
                return BadRequest(new { message = "UserId không hợp lệ." });
            }

            var favoriteBook = await web_dataContext.FavoriteBooks
                .FirstOrDefaultAsync(fb => fb.UserId == userId && fb.BookId == bookId);

            if (favoriteBook == null)
            {
                return NotFound(new { message = "Sách này không có trong danh sách yêu thích." });
            }

            web_dataContext.FavoriteBooks.Remove(favoriteBook);
            await web_dataContext.SaveChangesAsync();

            return Ok(new { message = "Sách đã được xóa khỏi danh sách yêu thích." });
        }
        [HttpGet("GetFavoriteBooks")]
        public async Task<IActionResult> GetFavoriteBooks()
        {
            // Lấy UserId từ Session
            var userIdString = HttpContext.Request.Headers["UserId"].FirstOrDefault();

            if (string.IsNullOrEmpty(userIdString))
            {
                return Unauthorized(new { message = "Bạn cần phải đăng nhập." });
            }

            int userId;
            if (!int.TryParse(userIdString, out userId))
            {
                return BadRequest(new { message = "UserId không hợp lệ." });
            }

            var favoriteBooks = await web_dataContext.FavoriteBooks
                .Where(fb => fb.UserId == userId)
                .Include(fb => fb.Book)
                .Select(fb => fb.Book)
                .ToListAsync();

            if (favoriteBooks == null || !favoriteBooks.Any())
            {
                return NotFound(new { message = "Danh sách yêu thích của bạn đang trống." });
            }

            var favoriteBooksDTO = favoriteBooks.Select(book => new
            {
                book.Id,
                book.Title,
                book.ImgSrc
            }).ToList();

            return Ok(new { favoriteBooks = favoriteBooksDTO });
        }
        [HttpGet("ColumnChart_CountByGenre")]
        public IActionResult GetBooksCountByGenre()
        {
            var genreCounts = web_dataContext.Books
                .GroupBy(b => b.Genre)
                .Select(g => new GenreCount
                {
                    Genre = g.Key,
                    Count = g.Count()
                })
                .ToList();

            return Ok(genreCounts);
        }
        [HttpGet("LineChart_ViewCountperDay")]
        public async Task<ActionResult<IEnumerable<ViewIncrementDTO>>> GetDailyViewIncrements()
        {
            var dailyViewData = await web_dataContext.DailyViews
                .GroupBy(v => v.ViewDate)
                .Select(g => new ViewIncrementDTO
                {
                    Date = g.Key,
                    TotalViewIncrement = g.Sum(v => v.ViewCountDay)
                }).OrderBy(v => v.Date)
                .ToListAsync();

            return Ok(dailyViewData);
        }
        [HttpGet("GetRecommendedBooks")]
        public async Task<ActionResult<List<BookDTO>>> GetRecommendedBooks()
        {
            var userIdString = HttpContext.Request.Headers["UserId"].FirstOrDefault();

            if (string.IsNullOrEmpty(userIdString))
            {
                return Unauthorized(new { message = "Bạn cần phải đăng nhập." });
            }

            int userId;
            if (!int.TryParse(userIdString, out userId))
            {
                return BadRequest(new { message = "UserId không hợp lệ." });
            }

            // Sử dụng RecommendationService để lấy sách gợi ý
            List<BookDTO> recommendedBooks = await _recommendationService.GetRecommendedBooks(userId);

            if (recommendedBooks == null || !recommendedBooks.Any())
            {
                return NotFound(new { message = "Không có sách đề xuất cho người dùng này." });
            }

            return Ok(new { recommendedBooks });
        }
        [HttpGet("GetFeedBack/{bookId}")]
        public async Task<IActionResult> GetFeedbacksByBook(int bookId)
        {
            var feedbacks = await web_dataContext.Feedbacks
                .Where(f => f.BookId == bookId)
                .ToListAsync();

            if (feedbacks == null || feedbacks.Count == 0)
            {
                return NotFound("Không có phản hồi cho sách này.");
            }

            var feedbackDTOs = feedbacks.Select(f => new FeedBackDTO
            {
                BookId = f.BookId,
                Comment = f.Comment
            }).ToList();

            return Ok(feedbackDTOs);
        }
        [HttpPost("AddFeedback")]
        public async Task<IActionResult> AddFeedback([FromBody] FeedBackDTO feedbackDTO)
        {
            // Lấy UserId từ header
            var userIdString = HttpContext.Request.Headers["UserId"].FirstOrDefault();
            if (string.IsNullOrEmpty(userIdString))
            {
                return Unauthorized(new { message = "Bạn cần đăng nhập để thực hiện hành động này." });
            }

            int userIdInt = int.Parse(userIdString);

            var feedback = new FeedBack
            {
                BookId = feedbackDTO.BookId,
                UserId = userIdInt,
                Comment = feedbackDTO.Comment,
                CreatedAt = DateTime.UtcNow
            };

            web_dataContext.Feedbacks.Add(feedback);
            await web_dataContext.SaveChangesAsync();

            return Ok(new { message = "Phản hồi đã được thêm thành công.", feedback });
        }
        [HttpPut("UpdateFeedBack/{bookId}/Comment/{id}")]
        public async Task<IActionResult> UpdateFeedback(int bookId, int id, [FromBody] FeedBackDTO2 feedbackDTO)
        {
            // Lấy UserId từ header
            var userIdString = HttpContext.Request.Headers["UserId"].FirstOrDefault();
            if (string.IsNullOrEmpty(userIdString))
            {
                return Unauthorized(new { message = "Bạn cần đăng nhập để thực hiện hành động này." });
            }

            int userIdInt = int.Parse(userIdString);

            // Tìm sách theo bookId
            var book = await web_dataContext.Books.Include(b => b.Feedbacks)
                               .FirstOrDefaultAsync(b => b.Id == bookId);

            if (book == null)
            {
                return NotFound(new { message = "Sách không tồn tại." });
            }

            // Tìm bình luận theo id
            var existingFeedback = book.Feedbacks.FirstOrDefault(f => f.Id == id);
            if (existingFeedback == null)
            {
                return NotFound(new { message = "Phản hồi không tồn tại." });
            }

            // Kiểm tra quyền chỉnh sửa bình luận (người dùng phải là chủ của bình luận hoặc Admin)
            if (existingFeedback.UserId != userIdInt && HttpContext.Request.Headers["Role"].FirstOrDefault() != "Admin")
            {
                return Forbid("Bạn chỉ có thể sửa phản hồi của chính mình.");
            }

            // Cập nhật nội dung bình luận
            existingFeedback.Comment = feedbackDTO.Comment;

            // Lưu thay đổi vào cơ sở dữ liệu
            web_dataContext.Entry(existingFeedback).State = EntityState.Modified;
            await web_dataContext.SaveChangesAsync();

            return NoContent();
        }
        [HttpDelete("DeleteFeedBack/{bookId}/Comment/{id}")]
        public async Task<IActionResult> DeleteFeedback(int bookId, int id)
        {
            // Lấy UserId từ header
            var userIdString = HttpContext.Request.Headers["UserId"].FirstOrDefault();
            if (string.IsNullOrEmpty(userIdString))
            {
                return Unauthorized(new { message = "Bạn cần đăng nhập để thực hiện hành động này." });
            }

            int userIdInt = int.Parse(userIdString);

            // Tìm sách theo bookId
            var book = await web_dataContext.Books.Include(b => b.Feedbacks)
                               .FirstOrDefaultAsync(b => b.Id == bookId);

            if (book == null)
            {
                return NotFound(new { message = "Sách không tồn tại." });
            }

            // Tìm bình luận theo feedbackId
            var feedback = book.Feedbacks.FirstOrDefault(f => f.Id == id);
            if (feedback == null)
            {
                return NotFound(new { message = "Phản hồi không tồn tại." });
            }

            // Kiểm tra quyền xóa (người dùng phải là chủ của bình luận hoặc Admin)
            if (feedback.UserId != userIdInt && HttpContext.Request.Headers["Role"].FirstOrDefault() != "Admin")
            {
                return Forbid("Bạn chỉ có thể xóa phản hồi của chính mình.");
            }

            // Xóa bình luận
            web_dataContext.Feedbacks.Remove(feedback);
            await web_dataContext.SaveChangesAsync();

            return NoContent();
        }
    }
}
