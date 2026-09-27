using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PromptForge.Api.Data;
using PromptForge.Api.Dtos;
using PromptForge.Api.Models;

namespace PromptForge.Api.Services
{
    /// <summary>
    /// IUserService'i implement eder: kayıt, giriş, 2 adımlı giriş, şifre sıfırlama, e-posta doğrulama ve JWT üretimi.
    /// </summary>
    public class UserService : IUserService
    {
        public const int MinPasswordLength = 8;

        private readonly PromptForgeDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly OneTimeCodeService _codes;
        private readonly TwoFactorService _twoFactor;
        private readonly IEmailSender _email;

        public UserService(PromptForgeDbContext context, IConfiguration configuration, OneTimeCodeService codes,
            TwoFactorService twoFactor, IEmailSender email)
        {
            _context = context;
            _configuration = configuration;
            _codes = codes;
            _twoFactor = twoFactor;
            _email = email;
        }

        /// <summary>Şifre kuralı tek yerde: başka yerler de (şifre değiştirme, sıfırlama) bunu kullanır.</summary>
        public static string? ValidatePassword(string? password) =>
            string.IsNullOrWhiteSpace(password) || password.Length < MinPasswordLength
                ? $"Şifre en az {MinPasswordLength} karakter olmalıdır."
                : null;

        public async Task<AuthResult> RegisterAsync(string email, string password, string displayName)
        {
            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
                return AuthResult.Fail("Geçerli bir e-posta adresi gir.");
            if (ValidatePassword(password) is { } passwordError)
                return AuthResult.Fail(passwordError);

            // Email'i standart hale getir: "Ahmet@X.com" ile "ahmet@x.com" aynı hesap sayılır.
            email = NormalizeEmail(email);
            if (await _context.Users.AnyAsync(u => u.Email == email))
                return AuthResult.Fail("Bu email zaten kayıtlı.");

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                // BCrypt: şifre geri döndürülemez şekilde saklanır.
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                // Görünen ad girilmediyse email'in @ öncesini kullan (ahmet@x.com → ahmet).
                DisplayName = string.IsNullOrWhiteSpace(displayName) ? email.Split('@')[0] : displayName.Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = true,
                // Varsayılan ayarlar kullanıcıyla birlikte tek SaveChanges ile kaydolur.
                Settings = new UserSettings { Id = Guid.NewGuid() }
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            await SendEmailVerificationAsync(user);

            return new AuthResult(true, GenerateJwtToken(user), null, user);
        }

        public async Task<AuthResult> LoginAsync(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                return AuthResult.Fail("Email ve şifre gerekli.");

            email = NormalizeEmail(email);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            // Kullanıcı yoksa da şifre yanlışsa da aynı mesaj: dışarıdan "bu email kayıtlı mı?" öğrenilemez.
            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                return AuthResult.Fail("Email veya şifre yanlış.");
            if (!user.IsActive)
                return AuthResult.Fail("Hesap deaktive edilmiş.");

            if (user.TwoFactorEnabled)
            {
                // Şifre doğru ama iş bitmedi: 5 dakika geçerli bir bilet ver, kod ekranında bununla devam edilir.
                var secret = await _codes.CreateSecretTokenAsync(user.Id, OneTimeCodeService.TwoFactor, TimeSpan.FromMinutes(5));
                return new AuthResult(true, null, null, user, RequiresTwoFactor: true, TwoFactorTicket: $"{user.Id:N}.{secret}");
            }

            return new AuthResult(true, GenerateJwtToken(user), null, user);
        }

        public async Task<AuthResult> CompleteTwoFactorLoginAsync(string ticket, string code)
        {
            // Bilet formatı: "{kullanıcıId}.{gizli anahtar}"
            var parts = (ticket ?? string.Empty).Split('.', 2);
            if (parts.Length != 2 || !Guid.TryParseExact(parts[0], "N", out var userId))
                return AuthResult.Fail("Oturum süresi doldu. Lütfen tekrar giriş yap.");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null || !user.TwoFactorEnabled || string.IsNullOrEmpty(user.TwoFactorSecret))
                return AuthResult.Fail("Oturum süresi doldu. Lütfen tekrar giriş yap.");

            // Önce kodu kontrol et, bileti sadece kod doğruysa harca: yanlış kodda kullanıcı tekrar deneyebilsin.
            if (!_twoFactor.VerifyCode(_twoFactor.Unprotect(user.TwoFactorSecret), code))
                return AuthResult.Fail("Doğrulama kodu hatalı.");
            if (!await _codes.ConsumeAsync(user.Id, OneTimeCodeService.TwoFactor, parts[1]))
                return AuthResult.Fail("Oturum süresi doldu. Lütfen tekrar giriş yap.");

            return new AuthResult(true, GenerateJwtToken(user), null, user);
        }

        public async Task RequestPasswordResetAsync(string email, string appBaseUrl)
        {
            if (string.IsNullOrWhiteSpace(email)) return;
            email = NormalizeEmail(email);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.IsActive);
            if (user == null) return; // Bilerek sessiz: kayıtlı olmayan email'ler de "gönderildi" cevabı alır.

            var token = await _codes.CreateSecretTokenAsync(user.Id, OneTimeCodeService.PasswordReset, TimeSpan.FromMinutes(30));
            var link = $"{appBaseUrl}/auth/recover-password.html?email={Uri.EscapeDataString(email)}&token={token}";
            await _email.SendAsync(email, "PromptForge şifre sıfırlama",
                $"Merhaba {user.DisplayName},\n\nŞifreni sıfırlamak için bu bağlantıyı aç (30 dakika geçerli):\n{link}\n\nBu isteği sen yapmadıysan bu e-postayı yok sayabilirsin.");
        }

        public async Task<(bool success, string? error)> ResetPasswordAsync(string email, string token, string newPassword)
        {
            if (ValidatePassword(newPassword) is { } passwordError) return (false, passwordError);

            email = NormalizeEmail(email ?? string.Empty);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null || !await _codes.ConsumeAsync(user.Id, OneTimeCodeService.PasswordReset, token))
                return (false, "Sıfırlama bağlantısı geçersiz veya süresi dolmuş. Yeni bir bağlantı iste.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.UpdatedAt = DateTime.UtcNow;
            // Bağlantı e-postaya geldiğine göre e-posta adresi de doğrulanmış sayılır.
            user.EmailConfirmed = true;
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task SendEmailVerificationAsync(User user)
        {
            var code = await _codes.CreateNumericCodeAsync(user.Id, OneTimeCodeService.EmailVerify, TimeSpan.FromMinutes(30));
            await _email.SendAsync(user.Email, "PromptForge e-posta doğrulama kodu",
                $"Merhaba {user.DisplayName},\n\nE-posta doğrulama kodun: {code}\n\nKod 30 dakika geçerlidir.");
        }

        public async Task<List<UserInfo>> GetAllUsersAsync()
        {
            var users = await _context.Users
                // Sadece okuyacağız, değiştirmeyeceğiz: EF'in değişiklik takibini kapatmak daha hızlıdır.
                .AsNoTracking()
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();
            return users.Select(ToUserInfo).ToList();
        }

        /// <summary>Veritabanı modelini dışarıya güvenli DTO'ya çevirir; şifre hash'i ve 2FA anahtarı asla dışarı çıkmaz.</summary>
        public static UserInfo ToUserInfo(User user) => new()
        {
            Id = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            Avatar = user.Avatar,
            EmailConfirmed = user.EmailConfirmed,
            TwoFactorEnabled = user.TwoFactorEnabled,
            Plan = user.Plan,
            CreatedAt = DateTime.SpecifyKind(user.CreatedAt, DateTimeKind.Utc)
        };

        // Email karşılaştırmaları büyük/küçük harfe takılmasın diye tek formata çeviririz.
        private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

        /// <summary>
        /// JWT token'ı oluşturur.
        /// İçerik: kullanıcı id'si ve email; imza: appsettings'teki gizli anahtar; süre: Jwt:ExpiryMinutes.
        /// </summary>
        private string GenerateJwtToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Secret"] ?? ""));
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email)
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(int.Parse(_configuration["Jwt:ExpiryMinutes"] ?? "60")),
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
