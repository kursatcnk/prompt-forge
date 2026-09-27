using Microsoft.AspNetCore.Mvc;
using PromptForge.Api.Dtos;
using PromptForge.Api.Models;
using PromptForge.Api.Services;

namespace PromptForge.Api.Controllers
{
    /// <summary>
    /// Kimlik doğrulama (Authentication) controller'ı.
    ///
    /// ENDPOINT'LER:
    /// POST /api/auth/register  → Yeni kullanıcı kaydı
    /// POST /api/auth/login     → Giriş (token dön)
    ///
    /// FLOW:
    /// 1. Frontend kullanıcı bilgisini JSON olarak gönder
    /// 2. Backend UserService çağır
    /// 3. Başarılı ise token dön
    /// 4. Frontend token'ı lokale kaydet
    /// 5. Sonraki istek'lerde Authorization header'ına ekle
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IUserService userService, ILogger<AuthController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        /// <summary>
        /// Yeni kullanıcı kaydı.
        ///
        /// ÖRNEK ISTEK:
        /// POST /api/auth/register
        /// {
        ///   "email": "ahmet@example.com",
        ///   "password": "securepass123",
        ///   "displayName": "Ahmet Yilmaz"
        /// }
        ///
        /// BAŞARILI YANIT (200):
        /// {
        ///   "success": true,
        ///   "token": "eyJhbGciOiJIUzI1NiIs...",
        ///   "message": "Kaydı başarılı",
        ///   "user": {
        ///     "id": "550e8400-e29b-41d4-a716-446655440000",
        ///     "email": "ahmet@example.com",
        ///     "displayName": "Ahmet Yilmaz"
        ///   }
        /// }
        ///
        /// HATA YANIT (400):
        /// {
        ///   "success": false,
        ///   "token": null,
        ///   "message": "Bu email zaten kayıtlı."
        /// }
        /// </summary>
        [HttpPost("register")]
        public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
        {
            // Validation
            if (request == null || string.IsNullOrWhiteSpace(request.Email))
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = "Email boş olamaz."
                });

            try
            {
                // UserService'i çağır (kayıt işlemi).
                var (success, token, error, user) = await _userService.RegisterAsync(
                    request.Email,
                    request.Password ?? "",
                    request.DisplayName ?? ""
                );

                if (!success)
                    return BadRequest(new AuthResponse
                    {
                        Success = false,
                        Message = error
                    });

                // Başarılı: token ve kullanıcı bilgisini dön.
                _logger.LogInformation("Yeni kullanıcı kaydı: {Email}", user!.Email);

                return Ok(new AuthResponse
                {
                    Success = true,
                    Token = token,
                    Message = "Kayıt başarılı. Otomatik giriş yapıldı.",
                    User = ToUserInfo(user)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Register hatası: {ex.Message}");
                return StatusCode(500, new AuthResponse
                {
                    Success = false,
                    Message = "Bir hata oluştu. Lütfen daha sonra tekrar deneyin."
                });
            }
        }

        /// <summary>
        /// Kullanıcı girişi.
        ///
        /// ÖRNEK ISTEK:
        /// POST /api/auth/login
        /// {
        ///   "email": "ahmet@example.com",
        ///   "password": "securepass123"
        /// }
        ///
        /// BAŞARILI YANIT (200):
        /// {
        ///   "success": true,
        ///   "token": "eyJhbGciOiJIUzI1NiIs...",
        ///   "message": "Giriş başarılı"
        /// }
        ///
        /// HATA YANIT (401):
        /// {
        ///   "success": false,
        ///   "token": null,
        ///   "message": "Email veya şifre yanlış."
        /// }
        /// </summary>
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
        {
            // Validation
            if (request == null || string.IsNullOrWhiteSpace(request.Email))
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = "Email ve şifre gerekli."
                });

            try
            {
                // UserService'i çağır (giriş işlemi).
                var (success, token, error, user) = await _userService.LoginAsync(
                    request.Email,
                    request.Password ?? ""
                );

                if (!success)
                    return Unauthorized(new AuthResponse
                    {
                        Success = false,
                        Message = error
                    });

                // Başarılı: token ve kullanıcı bilgisini dön (arayüz adını gösterebilsin).
                _logger.LogInformation("Kullanıcı girişi: {Email}", user!.Email);

                return Ok(new AuthResponse
                {
                    Success = true,
                    Token = token,
                    Message = "Giriş başarılı",
                    User = ToUserInfo(user)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Login hatası: {ex.Message}");
                return StatusCode(500, new AuthResponse
                {
                    Success = false,
                    Message = "Bir hata oluştu. Lütfen daha sonra tekrar deneyin."
                });
            }
        }

        // Veritabanı modelini dışarıya güvenli DTO'ya çevirir; şifre hash'i asla dışarı çıkmaz.
        private static UserInfo ToUserInfo(User user) => new()
        {
            Id = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            Avatar = user.Avatar
        };
    }
}
