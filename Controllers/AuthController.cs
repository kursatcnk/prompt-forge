using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PromptForge.Api.Dtos;
using PromptForge.Api.Services;

namespace PromptForge.Api.Controllers
{
    /// <summary>
    /// Kimlik doğrulama (giriş yapmadan erişilebilen) endpoint'leri.
    ///
    /// POST /api/auth/register              → Kayıt (otomatik giriş + doğrulama kodu e-postası)
    /// POST /api/auth/login                 → Giriş (2FA açıksa bilet döner)
    /// POST /api/auth/login/two-factor      → 2 adımlı girişin ikinci adımı
    /// POST /api/auth/forgot-password       → Şifre sıfırlama bağlantısı gönder
    /// POST /api/auth/reset-password        → Bağlantıdaki anahtarla yeni şifre belirle
    ///
    /// [EnableRateLimiting("auth")]: aynı IP'den dakikada sınırlı deneme yapılabilir (şifre tahmin saldırısına karşı).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting("auth")]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IUserService userService, ILogger<AuthController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
        {
            var result = await _userService.RegisterAsync(request.Email ?? "", request.Password ?? "", request.DisplayName ?? "");
            if (!result.Success)
                return BadRequest(new AuthResponse { Success = false, Message = result.Error });

            _logger.LogInformation("Yeni kullanıcı kaydı: {Email}", result.User!.Email);
            return Ok(ToResponse(result, "Kayıt başarılı. Otomatik giriş yapıldı."));
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
        {
            var result = await _userService.LoginAsync(request.Email ?? "", request.Password ?? "");
            if (!result.Success)
                return Unauthorized(new AuthResponse { Success = false, Message = result.Error });

            _logger.LogInformation("Kullanıcı girişi: {Email}", result.User!.Email);
            return Ok(ToResponse(result, result.RequiresTwoFactor ? "Doğrulama kodu gerekli." : "Giriş başarılı"));
        }

        [HttpPost("login/two-factor")]
        public async Task<ActionResult<AuthResponse>> LoginTwoFactor([FromBody] TwoFactorLoginRequest request)
        {
            var result = await _userService.CompleteTwoFactorLoginAsync(request.Ticket ?? "", request.Code ?? "");
            if (!result.Success)
                return Unauthorized(new AuthResponse { Success = false, Message = result.Error });

            return Ok(ToResponse(result, "Giriş başarılı"));
        }

        [HttpPost("forgot-password")]
        public async Task<ActionResult<MessageResponse>> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            // Bağlantı bu sitenin adresiyle oluşturulur (örn. http://localhost:5299/auth/recover-password.html?...).
            await _userService.RequestPasswordResetAsync(request.Email ?? "", $"{Request.Scheme}://{Request.Host}");
            // Email kayıtlı olsun olmasın aynı cevap: dışarıdan hangi email'lerin kayıtlı olduğu öğrenilemez.
            return Ok(MessageResponse.Ok("Bu e-posta kayıtlıysa şifre sıfırlama bağlantısı gönderildi."));
        }

        [HttpPost("reset-password")]
        public async Task<ActionResult<MessageResponse>> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            var (success, error) = await _userService.ResetPasswordAsync(request.Email ?? "", request.Token ?? "", request.NewPassword ?? "");
            return success
                ? Ok(MessageResponse.Ok("Şifren güncellendi. Yeni şifrenle giriş yapabilirsin."))
                : BadRequest(MessageResponse.Fail(error!));
        }

        private static AuthResponse ToResponse(AuthResult result, string message) => new()
        {
            Success = true,
            Token = result.Token,
            Message = message,
            User = result.RequiresTwoFactor ? null : UserService.ToUserInfo(result.User!),
            RequiresTwoFactor = result.RequiresTwoFactor,
            TwoFactorTicket = result.TwoFactorTicket
        };
    }
}
