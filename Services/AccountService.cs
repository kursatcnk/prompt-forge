using Microsoft.EntityFrameworkCore;
using PromptForge.Api.Data;
using PromptForge.Api.Dtos;
using PromptForge.Api.Models;
using PromptForge.Api.Services.Ai;

namespace PromptForge.Api.Services
{
    /// <summary>
    /// Giriş yapmış kullanıcının kendi hesabıyla ilgili işlemler.
    /// Tüm metodlar userId'yi token'dan alır; kullanıcı sadece kendi hesabını değiştirebilir.
    /// </summary>
    public class AccountService
    {
        // Ayarlarda sadece arayüzün tanıdığı değerler kabul edilir; rastgele metin veritabanına yazılamaz.
        private static readonly HashSet<string> Models = new() { "gpt", "claude", "gemini", "deepseek", "universal" };
        private static readonly HashSet<string> Goals = new() { "quality", "balanced", "lean" };
        private static readonly HashSet<string> Themes = new() { "light", "dark", "system" };
        private static readonly HashSet<string> Densities = new() { "comfortable", "compact" };
        private static readonly HashSet<string> UseCases = new() { "general", "coding", "research", "content", "data" };
        private static readonly HashSet<string> Formats = new() { "auto", "markdown", "json", "table", "checklist", "code" };
        private static readonly HashSet<string> Languages = new() { "prompt", "tr", "en" };

        private readonly PromptForgeDbContext _context;
        private readonly UsageService _usage;
        private readonly PromptOptimizerService _optimizer;
        private readonly TwoFactorService _twoFactor;
        private readonly OneTimeCodeService _codes;
        private readonly IUserService _users;

        public AccountService(PromptForgeDbContext context, UsageService usage, PromptOptimizerService optimizer,
            TwoFactorService twoFactor, OneTimeCodeService codes, IUserService users)
        {
            _context = context;
            _usage = usage;
            _optimizer = optimizer;
            _twoFactor = twoFactor;
            _codes = codes;
            _users = users;
        }

        public async Task<MeResponse?> GetMeAsync(Guid userId)
        {
            var user = await _context.Users.Include(u => u.Settings).AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return null;

            var provider = _optimizer.ResolveProvider(user.Settings?.DefaultModel);
            return new MeResponse
            {
                User = UserService.ToUserInfo(user),
                Settings = ToSettingsDto(user.Settings ?? new UserSettings()),
                Usage = await _usage.GetUsageAsync(user.Id, user.Plan),
                Ai = new AiStatusDto { Enabled = provider != null, Provider = provider?.DisplayName, Model = provider?.Model },
                Plans = UsageService.Plans.Select(p => new PlanDto
                {
                    Key = p.Key, Name = p.Name, MonthlyLimit = p.MonthlyLimit, Price = p.Price, Features = p.Features
                }).ToList()
            };
        }

        public async Task<SettingsDto> UpdateSettingsAsync(Guid userId, SettingsDto dto)
        {
            var settings = await _context.UserSettings.FirstOrDefaultAsync(s => s.UserId == userId);
            if (settings == null)
            {
                settings = new UserSettings { Id = Guid.NewGuid(), UserId = userId };
                _context.UserSettings.Add(settings);
            }

            // Geçersiz değer gelirse eski değer korunur.
            if (Models.Contains(dto.Model)) settings.DefaultModel = dto.Model;
            if (Goals.Contains(dto.Goal)) settings.DefaultOptimizationTarget = dto.Goal;
            if (Themes.Contains(dto.Theme)) settings.Theme = dto.Theme;
            if (Densities.Contains(dto.Density)) settings.Density = dto.Density;
            if (dto.Motion is "on" or "off") settings.MotionEnabled = dto.Motion == "on";
            if (UseCases.Contains(dto.UseCase)) settings.UseCase = dto.UseCase;
            if (Formats.Contains(dto.ResponseFormat)) settings.ResponseFormat = dto.ResponseFormat;
            if (Languages.Contains(dto.ResponseLanguage)) settings.ResponseLanguage = dto.ResponseLanguage;
            settings.AskClarifying = dto.AskClarifying;
            settings.ExposeAssumptions = dto.ExposeAssumptions;

            await _context.SaveChangesAsync();
            return ToSettingsDto(settings);
        }

        public async Task<(bool success, string? error, UserInfo? user)> UpdateProfileAsync(Guid userId, string? displayName)
        {
            var name = displayName?.Trim();
            if (string.IsNullOrEmpty(name) || name.Length > 100)
                return (false, "Ad 1-100 karakter arasında olmalı.", null);

            var user = await _context.Users.FirstAsync(u => u.Id == userId);
            user.DisplayName = name;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return (true, null, UserService.ToUserInfo(user));
        }

        public async Task<(bool success, string? error)> ChangePasswordAsync(Guid userId, string? currentPassword, string? newPassword)
        {
            var user = await _context.Users.FirstAsync(u => u.Id == userId);
            if (string.IsNullOrEmpty(currentPassword) || !BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
                return (false, "Mevcut şifre yanlış.");
            if (UserService.ValidatePassword(newPassword) is { } error)
                return (false, error);

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool success, string? error)> VerifyEmailAsync(Guid userId, string? code)
        {
            var user = await _context.Users.FirstAsync(u => u.Id == userId);
            if (user.EmailConfirmed) return (true, null);
            if (!await _codes.ConsumeAsync(userId, OneTimeCodeService.EmailVerify, code))
                return (false, "Kod hatalı veya süresi dolmuş. Yeni kod isteyebilirsin.");

            user.EmailConfirmed = true;
            await _context.SaveChangesAsync();
            return (true, null);
        }

        /// <summary>Yeni doğrulama kodu gönderir. E-posta zaten doğrulandıysa gönderim yapmaz. Gönderim başarısızsa false.</summary>
        public async Task<bool> ResendVerificationAsync(Guid userId)
        {
            var user = await _context.Users.FirstAsync(u => u.Id == userId);
            return user.EmailConfirmed || await _users.SendEmailVerificationAsync(user);
        }

        /// <summary>
        /// 2FA kurulumunun ilk adımı: yeni gizli anahtar üretir ve QR kodunu döner.
        /// Kullanıcı uygulamadan ilk kodu doğrulayana kadar 2FA açılmaz (yanlış kurulumla hesaptan kilitlenmesin).
        /// </summary>
        public async Task<TwoFactorSetupResponse?> BeginTwoFactorSetupAsync(Guid userId)
        {
            var user = await _context.Users.FirstAsync(u => u.Id == userId);
            if (user.TwoFactorEnabled) return null;

            var secret = _twoFactor.GenerateSecret();
            user.TwoFactorSecret = _twoFactor.Protect(secret);
            await _context.SaveChangesAsync();

            var uri = _twoFactor.BuildOtpAuthUri(user.Email, secret);
            return new TwoFactorSetupResponse { Secret = secret, OtpAuthUri = uri, QrDataUrl = _twoFactor.BuildQrDataUrl(uri) };
        }

        public async Task<(bool success, string? error)> EnableTwoFactorAsync(Guid userId, string? code)
        {
            var user = await _context.Users.FirstAsync(u => u.Id == userId);
            if (user.TwoFactorEnabled) return (true, null);
            if (string.IsNullOrEmpty(user.TwoFactorSecret))
                return (false, "Önce kurulumu başlat.");
            if (!_twoFactor.VerifyCode(_twoFactor.Unprotect(user.TwoFactorSecret), code))
                return (false, "Kod hatalı. Uygulamadaki güncel kodu gir.");

            user.TwoFactorEnabled = true;
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool success, string? error)> DisableTwoFactorAsync(Guid userId, string? password)
        {
            var user = await _context.Users.FirstAsync(u => u.Id == userId);
            if (string.IsNullOrEmpty(password) || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                return (false, "Şifre yanlış.");

            user.TwoFactorEnabled = false;
            user.TwoFactorSecret = null;
            await _context.SaveChangesAsync();
            return (true, null);
        }

        /// <summary>
        /// Hesabı ve tüm verilerini kalıcı olarak siler.
        /// Favoriler önce silinir (prompt→favori bağlantısı otomatik silinmiyor), gerisi kullanıcıyla birlikte cascade silinir.
        /// </summary>
        public async Task<(bool success, string? error)> DeleteAccountAsync(Guid userId, string? password)
        {
            var user = await _context.Users.AsNoTracking().FirstAsync(u => u.Id == userId);
            if (string.IsNullOrEmpty(password) || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                return (false, "Şifre yanlış.");

            await _context.FavoritePrompts.Where(f => f.UserId == userId).ExecuteDeleteAsync();
            await _context.Users.Where(u => u.Id == userId).ExecuteDeleteAsync();
            return (true, null);
        }

        private static SettingsDto ToSettingsDto(UserSettings s) => new()
        {
            Model = s.DefaultModel ?? "gpt",
            Goal = s.DefaultOptimizationTarget ?? "balanced",
            Theme = s.Theme ?? "light",
            Density = s.Density ?? "comfortable",
            Motion = s.MotionEnabled ? "on" : "off",
            UseCase = s.UseCase ?? "general",
            ResponseFormat = s.ResponseFormat ?? "auto",
            ResponseLanguage = s.ResponseLanguage ?? "prompt",
            AskClarifying = s.AskClarifying,
            ExposeAssumptions = s.ExposeAssumptions
        };
    }
}
