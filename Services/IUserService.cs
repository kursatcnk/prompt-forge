using PromptForge.Api.Dtos;
using PromptForge.Api.Models;

namespace PromptForge.Api.Services
{
    /// <summary>
    /// Kimlik doğrulama işlemleri için servis arayüzü (sözleşme).
    ///
    /// NEDEN INTERFACE?
    /// - Controller'lar concrete class yerine interface'e bağımlı olur.
    /// - Unit test'te fake implementation kullanabiliriz.
    /// - Implementation değişse, Controller kodu değişmez.
    /// </summary>
    public interface IUserService
    {
        /// <summary>Yeni kullanıcı kaydı. Başarılıysa token döner ve e-postaya doğrulama kodu gönderilir.</summary>
        Task<AuthResult> RegisterAsync(string email, string password, string displayName);

        /// <summary>Giriş. 2 adımlı doğrulama açıksa token yerine kısa ömürlü bir bilet döner.</summary>
        Task<AuthResult> LoginAsync(string email, string password);

        /// <summary>2 adımlı girişin ikinci adımı: bilet + authenticator kodu → token.</summary>
        Task<AuthResult> CompleteTwoFactorLoginAsync(string ticket, string code);

        /// <summary>Şifre sıfırlama bağlantısını e-postayla gönderir. Email kayıtlı olmasa da aynı şekilde davranır.</summary>
        Task RequestPasswordResetAsync(string email, string appBaseUrl);

        /// <summary>E-postadaki bağlantıdan gelen anahtarla yeni şifre belirler.</summary>
        Task<(bool success, string? error)> ResetPasswordAsync(string email, string token, string newPassword);

        /// <summary>6 haneli e-posta doğrulama kodunu üretip gönderir. Gönderim başarısızsa false döner.</summary>
        Task<bool> SendEmailVerificationAsync(User user);

        /// <summary>Tüm kullanıcıları listeler (şifre hash'i dönmez).</summary>
        Task<List<UserInfo>> GetAllUsersAsync();
    }

    /// <summary>Giriş/kayıt işleminin sonucu.</summary>
    public record AuthResult(bool Success, string? Token, string? Error, User? User, bool RequiresTwoFactor = false, string? TwoFactorTicket = null)
    {
        public static AuthResult Fail(string error) => new(false, null, error, null);
    }
}
