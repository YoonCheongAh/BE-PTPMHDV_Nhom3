using System.Net.Mail;
using System.Net;
using System.Threading.Tasks;
using HuongDichVu.DTO;
using HuongDichVu.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MailKit.Net.Smtp;
using MimeKit;
using BCrypt.Net;
namespace WebService.Controllers
{
    public static class EncryptionHelper
    {
        // Hàm mã hóa mật khẩu
        public static string HashPassword(string plainText)
        {
            return BCrypt.Net.BCrypt.HashPassword(plainText);
        }

        // Hàm kiểm tra mật khẩu
        public static bool VerifyPassword(string plainText, string hashedPassword)
        {
            return BCrypt.Net.BCrypt.Verify(plainText, hashedPassword);
        }
    }
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext context;

        public AuthController(ApplicationDbContext context)
        {
            this.context = context;
        }
        // API Đăng ký
        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] Register auth)
        {
            // Kiểm tra xem auth có hợp lệ không
            if (auth == null)
                return BadRequest("Dữ liệu không được để trống");

            // Kiểm tra các trường dữ liệu
            if (string.IsNullOrWhiteSpace(auth.Username))
                return BadRequest("Username không được để trống");

            if (string.IsNullOrWhiteSpace(auth.Password))
                return BadRequest("Password không được để trống");

            if (string.IsNullOrWhiteSpace(auth.Email))
                return BadRequest("Email không được để trống");

            // Kiểm tra username và email đã tồn tại
            if (await context.Auths.AnyAsync(u => u.Username == auth.Username))
                return BadRequest("Username đã tồn tại");

            if (await context.Auths.AnyAsync(u => u.Email == auth.Email))
                return BadRequest("Email đã tồn tại");

            // Tạo người dùng mới
            var user = new Auth
            {
                Username = auth.Username,
                Password = EncryptionHelper.HashPassword(auth.Password),
                Email = auth.Email,
                Role = "User"
            };

            // Lưu dữ liệu vào cơ sở dữ liệu
            context.Auths.Add(user);
            await context.SaveChangesAsync();

            return Ok("Đăng ký thành công");
        }

        // API Đăng nhập
        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] Login loginUser)
        {
            // Tìm người dùng theo username
            var user = await context.Auths.FirstOrDefaultAsync(u => u.Username == loginUser.Username);

            // Nếu không tìm thấy người dùng, trả về lỗi
            if (user == null)
            {
                return Unauthorized("Sai tên đăng nhập hoặc mật khẩu");
            }

            // Nếu vai trò là Admin, bỏ qua bước kiểm tra mật khẩu
            if (user.Role == "Admin")
            {
                HttpContext.Session.SetString("UserId", user.Id.ToString());
                HttpContext.Session.SetString("Username", user.Username);
                HttpContext.Session.SetString("Role", user.Role);

                return Ok(new { message = "Đăng nhập thành công (Admin)", username = user.Username, role = user.Role });
            }

            if (!EncryptionHelper.VerifyPassword(loginUser.Password, user.Password))
            {
                return Unauthorized("Sai tên đăng nhập hoặc mật khẩu");
            }

            HttpContext.Session.SetString("UserId", user.Id.ToString());
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("Role", user.Role);

            return Ok(new { message = "Đăng nhập thành công", username = user.Username, role = user.Role , id=user.Id });
        }

        [HttpGet("GetAllUser")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await context.Auths
                .Select(u => new AuthDTO
                {
                    Id = u.Id,
                    Email = u.Email,
                    Username = u.Username,
                    Password= u.Password,
                    Role = u.Role
                })
                .ToListAsync();

            if (!users.Any())
                return NotFound("Không có người dùng nào.");

            return Ok(users);
        }

        // API Lấy người dùng theo ID
        [HttpGet("GetUser/{id}")]
        public async Task<IActionResult> GetAuthById(int id)
        {
            var user = await context.Auths.FindAsync(id);
            if (user == null)
                return NotFound("Người dùng không tồn tại");

            return Ok(new AuthDTO
            {
                Id = user.Id,
                Email = user.Email,
                Username = user.Username,
                Password = user.Password,
                Role = user.Role
            });
        }

        // API Cập nhật thông tin người dùng
        [HttpPut("UpdateAccount/{id}")]
        public async Task<IActionResult> UpdateAuth(int id, [FromBody] Register auth)
        {
            var user = await context.Auths.FindAsync(id);
            if (user == null)
                return NotFound("Người dùng không tồn tại");

            user.Username = auth.Username;
            user.Email = auth.Email;

            if (!string.IsNullOrEmpty(auth.Password))
            {
                user.Password = EncryptionHelper.HashPassword(auth.Password);
            }

            await context.SaveChangesAsync();
            return Ok("Cập nhật thông tin thành công");
        }

        // API Xóa người dùng
        [HttpDelete("DeleteAccount/{id}")]
        public async Task<IActionResult> DeleteAuth(int id)
        {
            var user = await context.Auths.FindAsync(id);
            if (user == null)
                return NotFound("Người dùng không tồn tại");

            context.Auths.Remove(user);
            await context.SaveChangesAsync();

            return Ok("Xóa người dùng thành công");
        }

        [HttpPost("ForgotPassword")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            var user = await context.Auths.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
            {
                return NotFound("Tài khoản không tồn tại");
            }

            // Tạo resetToken ngắn với 5 ký tự gồm chữ cái in hoa và số
            string resetToken = GenerateResetToken(5);
            user.ResetToken = resetToken;
            await context.SaveChangesAsync();

            string resetLink = $"{resetToken}";
            await SendResetPasswordEmailAsync(user.Email, resetLink);

            return Ok("Yêu cầu đặt lại mật khẩu đã được gửi đến email của bạn");
        }
        private string GenerateResetToken(int length)
        {
            const string validChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            Random random = new Random();
            char[] token = new char[length];

            for (int i = 0; i < length; i++)
            {
                token[i] = validChars[random.Next(validChars.Length)];
            }

            return new string(token);
        }

        // Hàm gửi email
        private async Task SendResetPasswordEmailAsync(string toEmail, string resetLink)
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress("Welcome to BookWorld", "bookworldwithuser@gmail.com")); // Sửa tên hiển thị rõ ràng hơn
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = "Đặt lại mật khẩu";

            // Nội dung email
            email.Body = new TextPart("html")
            {
            Text = $@"
            <html>
            <head>
                <style>
                    body {{
                        font-family: 'Arial', sans-serif;
                        margin: 0;
                        padding: 0;
                        background-color: #f4f4f4;
                        color: #333;
                    }}
                    .email-container {{
                        width: 100%;
                        max-width: 600px;
                        margin: 0 auto;
                        background-color: #ffffff;
                        padding: 20px;
                        box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);
                    }}
                    .email-header {{
                        text-align: center;
                        width: 100%;
                        max-width: 600px;
                        height: auto;
                    }}
                    .email-header img {{
                        width: 100px;
                    height: auto;
                    }}
                    .email-body {{
                        padding: 20px;
                        text-align: left;
                    }}
                    .otp-code {{
                        display: inline-block;
                        padding: 10px 20px;
                        background-color: #f0f0f0;
                        border-radius: 5px;
                        font-weight: bold;
                        font-size: 18px;
                        margin-top: 10px;
                    }}
                    .footer {{
                        text-align: center;
                        font-size: 12px;
                        color: #888;
                        margin-top: 30px;
                    }}
                    .footer a {{
                        color: #007bff;
                        text-decoration: none;
                    }}
                </style>
            </head>
            <body>
                <div class='email-container'>
                    <div class='email-header'>
                        <img src='https://i.imgur.com/fqm9KU6.png' alt='Book World' style='width: 400px; height: auto;'/>
                    </div>
                    <div class='email-body'>
                        <p><strong>Xin chào Người dùng,</strong></p>
                        <p>Sử dụng mã OTP sau đây để xác minh địa chỉ email của bạn. Bạn có thể sử dụng địa chỉ email này để đăng nhập hoặc khôi phục tài khoản của bạn.</p>
                        <div class='otp-code'>{resetLink}</div>
                        <p>Vui lòng liên hệ với <a href='https://www.bookworld.com'>Bộ Phận Hỗ Trợ Book World</a> nếu bạn có bất kỳ thắc mắc nào.</p>
                    </div>
                    <div class='footer'>
                        <p>Xin cảm ơn,<br/>Đội Ngũ Book World</p>
                        <p><a href='https://www.bookworld.com'>www.bookworld.com</a></p>
                    </div>
                </div>
            </body>
            </html>"
            };

            using var smtp = new MailKit.Net.Smtp.SmtpClient();
            try
            {
                // Kết nối SMTP server
                await smtp.ConnectAsync("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);

                // Xác thực
                await smtp.AuthenticateAsync("bookworldwithuser@gmail.com", "jzga lfal jmap vhnb");

                // Gửi email
                await smtp.SendAsync(email);
            }
            catch (Exception ex)
            {
                // Xử lý lỗi và ghi log
                Console.WriteLine($"Lỗi khi gửi email: {ex.Message}");
                throw;
            }
            finally
            {
                // Ngắt kết nối SMTP server
                await smtp.DisconnectAsync(true);
            }
        }
        [HttpPost("ConfirmToken")]
        public async Task<IActionResult> ConfirmToken([FromBody] string token)
        {
            var user = await context.Auths.FirstOrDefaultAsync(u => u.ResetToken == token);
            if (user == null)
            {
                return BadRequest("Token không hợp lệ");
            }
            user.ResetToken = "accessToChangePassword";
            await context.SaveChangesAsync();
            return Ok("Token hợp lệ!");
        }
        [HttpPost("ResetPassword")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            var user = await context.Auths.FirstOrDefaultAsync(u => u.ResetToken == "accessToChangePassword");
            if (user == null)
            {
                return BadRequest("Token không hợp lệ");
            }

            if (request.NewPassword != request.ConfirmPassword)
            {
                return BadRequest("Mật khẩu không khớp!");
            }

            user.Password = EncryptionHelper.HashPassword(request.NewPassword);
            user.ResetToken = null; // Xóa token sau khi sử dụng
            await context.SaveChangesAsync();
            return Ok("Đặt lại mật khẩu thành công");
        }
        [HttpPost("Logout")]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear(); // Xóa toàn bộ session
            return Ok("Đăng xuất thành công");
        }
    }
}