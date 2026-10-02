using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PromptForge.Api.Dtos;
using PromptForge.Api.Services;

namespace PromptForge.Api.Controllers
{
    // Giriş yapmış kullanıcının kendi hesabıyla ilgili her şey.
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AccountController : ControllerBase
    {
        private readonly AccountService _account;

        public AccountController(AccountService account) => _account = account;

        [HttpGet("me")]
        public async Task<ActionResult<MeResponse>> Me()
        {
            var me = await _account.GetMeAsync(User.GetUserId());
            // Token hâlâ geçerli ama hesap silinmiş olabilir (başka sekmeden silindiyse). 401 dönünce arayüz girişe atıyor.
            return me == null ? Unauthorized() : Ok(me);
        }

        [HttpPut("settings")]
        public async Task<ActionResult<SettingsDto>> UpdateSettings([FromBody] SettingsDto settings) =>
            Ok(await _account.UpdateSettingsAsync(User.GetUserId(), settings));

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            var (success, error, user) = await _account.UpdateProfileAsync(User.GetUserId(), request.DisplayName);
            return success ? Ok(user) : BadRequest(MessageResponse.Fail(error!));
        }

        // Şifre soran uçlarda da deneme sınırı var; çalınan bir oturumla şifre tahmin edilemesin.
        [HttpPost("change-password")]
        [EnableRateLimiting("auth")]
        public async Task<ActionResult<MessageResponse>> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var (success, error) = await _account.ChangePasswordAsync(User.GetUserId(), request.CurrentPassword, request.NewPassword);
            return success ? Ok(MessageResponse.Ok("Şifren güncellendi.")) : BadRequest(MessageResponse.Fail(error!));
        }

        [HttpPost("verify-email")]
        [EnableRateLimiting("auth")]
        public async Task<ActionResult<MessageResponse>> VerifyEmail([FromBody] CodeRequest request)
        {
            var (success, error) = await _account.VerifyEmailAsync(User.GetUserId(), request.Code);
            return success ? Ok(MessageResponse.Ok("E-posta adresin doğrulandı.")) : BadRequest(MessageResponse.Fail(error!));
        }

        [HttpPost("resend-verification")]
        [EnableRateLimiting("auth")]
        public async Task<ActionResult<MessageResponse>> ResendVerification()
        {
            return await _account.ResendVerificationAsync(User.GetUserId())
                ? Ok(MessageResponse.Ok("Yeni doğrulama kodu e-postana gönderildi."))
                : StatusCode(StatusCodes.Status502BadGateway, MessageResponse.Fail("E-posta şu an gönderilemedi. Birkaç dakika sonra tekrar dene."));
        }

        [HttpPost("two-factor/setup")]
        public async Task<ActionResult<TwoFactorSetupResponse>> BeginTwoFactorSetup()
        {
            var setup = await _account.BeginTwoFactorSetupAsync(User.GetUserId());
            return setup == null ? BadRequest(MessageResponse.Fail("2 adımlı doğrulama zaten açık.")) : Ok(setup);
        }

        [HttpPost("two-factor/enable")]
        [EnableRateLimiting("auth")]
        public async Task<ActionResult<MessageResponse>> EnableTwoFactor([FromBody] CodeRequest request)
        {
            var (success, error) = await _account.EnableTwoFactorAsync(User.GetUserId(), request.Code);
            return success ? Ok(MessageResponse.Ok("2 adımlı doğrulama açıldı.")) : BadRequest(MessageResponse.Fail(error!));
        }

        [HttpPost("two-factor/disable")]
        [EnableRateLimiting("auth")]
        public async Task<ActionResult<MessageResponse>> DisableTwoFactor([FromBody] PasswordConfirmRequest request)
        {
            var (success, error) = await _account.DisableTwoFactorAsync(User.GetUserId(), request.Password);
            return success ? Ok(MessageResponse.Ok("2 adımlı doğrulama kapatıldı.")) : BadRequest(MessageResponse.Fail(error!));
        }

        [HttpPost("delete")]
        [EnableRateLimiting("auth")]
        public async Task<ActionResult<MessageResponse>> DeleteAccount([FromBody] PasswordConfirmRequest request)
        {
            var (success, error) = await _account.DeleteAccountAsync(User.GetUserId(), request.Password);
            if (!success) return BadRequest(MessageResponse.Fail(error!));

            AuthCookie.Delete(Response);
            return Ok(MessageResponse.Ok("Hesabın ve tüm verilerin silindi."));
        }
    }
}
