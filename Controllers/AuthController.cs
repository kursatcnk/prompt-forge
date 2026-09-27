using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PromptForge.Api.Dtos;
using PromptForge.Api.Services;

namespace PromptForge.Api.Controllers
{
    // Giriş gerektirmeyen uçlar. Hepsi IP başına dakikada 10 istekle sınırlı (Program.cs > "auth").
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
            await _userService.RequestPasswordResetAsync(request.Email ?? "", $"{Request.Scheme}://{Request.Host}");
            // Mail kayıtlı olsa da olmasa da aynı cevap; yoksa kimin üye olduğu buradan öğrenilebilir.
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
            // 2FA adımında henüz tam giriş yok, kullanıcı bilgisini göndermiyorum.
            User = result.RequiresTwoFactor ? null : UserService.ToUserInfo(result.User!),
            RequiresTwoFactor = result.RequiresTwoFactor,
            TwoFactorTicket = result.TwoFactorTicket
        };
    }
}
