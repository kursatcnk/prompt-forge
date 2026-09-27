using PromptForge.Api.Dtos;
using PromptForge.Api.Models;

namespace PromptForge.Api.Services
{
    public interface IUserService
    {
        Task<AuthResult> RegisterAsync(string email, string password, string displayName);

        // 2FA açıksa token yerine 5 dakikalık bir bilet döner, girişi CompleteTwoFactorLoginAsync bitirir.
        Task<AuthResult> LoginAsync(string email, string password);

        Task<AuthResult> CompleteTwoFactorLoginAsync(string ticket, string code);

        // Mail kayıtlı değilse de sessizce dönüyor, bilerek.
        Task RequestPasswordResetAsync(string email, string appBaseUrl);

        Task<(bool success, string? error)> ResetPasswordAsync(string email, string token, string newPassword);

        // Mail gönderilemezse false; kaydı bozmasın diye exception fırlatmıyor.
        Task<bool> SendEmailVerificationAsync(User user);

        Task<List<UserInfo>> GetAllUsersAsync();
    }

    public record AuthResult(bool Success, string? Token, string? Error, User? User, bool RequiresTwoFactor = false, string? TwoFactorTicket = null)
    {
        public static AuthResult Fail(string error) => new(false, null, error, null);
    }
}
