using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using PromptForge.Api.Data;
using PromptForge.Api.Models;

namespace PromptForge.Api.Services
{
    /// <summary>
    /// Tek kullanımlık kod üretir ve doğrular (e-posta doğrulama, şifre sıfırlama, 2 adımlı giriş bileti).
    /// Kodun kendisi saklanmaz, SHA-256 özeti saklanır. Aynı amaçla yeni kod üretilince eskisi geçersiz olur.
    /// </summary>
    public class OneTimeCodeService
    {
        public const string EmailVerify = "email-verify";
        public const string PasswordReset = "password-reset";
        public const string TwoFactor = "two-factor";

        private const int MaxFailedAttempts = 5;
        private readonly PromptForgeDbContext _context;

        public OneTimeCodeService(PromptForgeDbContext context) => _context = context;

        /// <summary>6 haneli sayısal kod (e-postaya yazılır).</summary>
        public Task<string> CreateNumericCodeAsync(Guid userId, string purpose, TimeSpan lifetime) =>
            CreateAsync(userId, purpose, lifetime, RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6"));

        /// <summary>Tahmin edilemeyen uzun anahtar (bağlantılarda ve biletlerde kullanılır).</summary>
        public Task<string> CreateSecretTokenAsync(Guid userId, string purpose, TimeSpan lifetime) =>
            CreateAsync(userId, purpose, lifetime, Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant());

        private async Task<string> CreateAsync(Guid userId, string purpose, TimeSpan lifetime, string code)
        {
            // Aynı amaçlı eski kodları iptal et: kullanıcı elinde her zaman sadece en son kod geçerli olsun.
            await _context.UserTokens.Where(t => t.UserId == userId && t.Purpose == purpose && t.UsedAt == null).ExecuteDeleteAsync();

            _context.UserTokens.Add(new UserToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Purpose = purpose,
                TokenHash = Hash(code),
                ExpiresAt = DateTime.UtcNow.Add(lifetime),
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
            return code;
        }

        /// <summary>
        /// Kodu doğrular ve başarılıysa "kullanıldı" olarak işaretler (tekrar kullanılamaz).
        /// 5 yanlış denemeden sonra kod iptal olur; böylece 6 haneli kod deneme-yanılmayla bulunamaz.
        /// </summary>
        public async Task<bool> ConsumeAsync(Guid userId, string purpose, string? code)
        {
            var token = await _context.UserTokens
                .Where(t => t.UserId == userId && t.Purpose == purpose && t.UsedAt == null)
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync();

            if (token == null || token.ExpiresAt < DateTime.UtcNow || token.FailedAttempts >= MaxFailedAttempts || string.IsNullOrWhiteSpace(code))
                return false;

            // Sabit zamanlı karşılaştırma: cevap süresinden kodun ne kadarının doğru olduğu anlaşılamaz.
            var matches = CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(token.TokenHash), Encoding.UTF8.GetBytes(Hash(code.Trim())));

            if (matches) token.UsedAt = DateTime.UtcNow;
            else token.FailedAttempts++;

            await _context.SaveChangesAsync();
            return matches;
        }

        private static string Hash(string value) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    }
}
