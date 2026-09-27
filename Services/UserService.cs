using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using PromptForge.Api.Data;
using PromptForge.Api.Dtos;
using PromptForge.Api.Models;

namespace PromptForge.Api.Services
{
    /// <summary>
    /// IUserService'i implement eder. Gerçek iş mantığını burada yazarız.
    ///
    /// SORUMLULUKLAR:
    /// - Kullanıcı kaydı ve girişi
    /// - Şifre güvenliği (hashing)
    /// - JWT token oluşturma ve doğrulama
    /// </summary>
    public class UserService : IUserService
    {
        private readonly PromptForgeDbContext _context;
        private readonly IConfiguration _configuration;

        public UserService(PromptForgeDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        /// <summary>
        /// Yeni kullanıcıyı kaydeder.
        /// </summary>
        public async Task<(bool success, string? token, string? error, User? user)> RegisterAsync(
            string email,
            string password,
            string displayName)
        {
            // Validation: Email boş mı?
            if (string.IsNullOrWhiteSpace(email))
                return (false, null, "Email boş olamaz.", null);

            // Validation: Şifre boş mı?
            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
                return (false, null, "Şifre en az 6 karakter olmalıdır.", null);

            // Email'i standart hale getir: boşlukları sil, küçük harfe çevir.
            // Böylece "Ahmet@X.com" ile "ahmet@x.com" aynı hesap sayılır.
            email = NormalizeEmail(email);

            // Kontrol: Email zaten kayıtlı mı?
            var emailTaken = await _context.Users.AnyAsync(u => u.Email == email);
            if (emailTaken)
                return (false, null, "Bu email zaten kayıtlı.", null);

            // Şifreyi BCrypt ile hash'le. Veritabanına asla düz şifre yazılmaz.
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

            // Yeni kullanıcı oluştur.
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                PasswordHash = passwordHash,
                // Görünen ad girilmediyse email'in @ öncesini kullan (ahmet@x.com → ahmet).
                DisplayName = string.IsNullOrWhiteSpace(displayName) ? email.Split('@')[0] : displayName.Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = true,
                // Varsayılan ayarları kullanıcıyla birlikte oluştur; tek SaveChanges ile ikisi birden kaydolur.
                Settings = new UserSettings { Id = Guid.NewGuid() }
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Kayıt sonrası otomatik giriş: token üret ve dön.
            var token = GenerateJwtToken(user.Id.ToString(), user.Email);
            return (true, token, null, user);
        }

        /// <summary>
        /// Kullanıcı girişi (login).
        /// </summary>
        public async Task<(bool success, string? token, string? error, User? user)> LoginAsync(
            string email,
            string password)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                return (false, null, "Email ve şifre gerekli.", null);

            email = NormalizeEmail(email);

            // Email ile kullanıcıyı bul.
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            // Kullanıcı yoksa da, şifre yanlışsa da aynı mesajı veriyoruz.
            // Böylece kötü niyetli biri "bu email kayıtlı mı?" bilgisini öğrenemez.
            if (user == null)
                return (false, null, "Email veya şifre yanlış.", null);

            // Hesap aktif mi?
            if (!user.IsActive)
                return (false, null, "Hesap deaktive edilmiş.", null);

            // Girilen şifreyi, kayıtlı hash ile karşılaştır.
            var isPasswordValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
            if (!isPasswordValid)
                return (false, null, "Email veya şifre yanlış.", null);

            // Şifre doğruysa token oluştur.
            var token = GenerateJwtToken(user.Id.ToString(), user.Email);
            return (true, token, null, user);
        }

        /// <summary>
        /// JWT token'ı doğrulama ve içeriğini çıkarma.
        /// Controller'lar her istek'te bunu çağırır.
        /// </summary>
        public async Task<(bool valid, string? userId, string? error)> ValidateTokenAsync(string token)
        {
            try
            {
                // JWT konfigürasyonunu oluştur.
                var key = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(_configuration["Jwt:Secret"] ?? ""));

                var tokenHandler = new JwtSecurityTokenHandler();

                // Token'ı parse et ve doğrula.
                var principal = tokenHandler.ValidateToken(token,
                    new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = key,
                        ValidateIssuer = true,
                        ValidIssuer = _configuration["Jwt:Issuer"],
                        ValidateAudience = true,
                        ValidAudience = _configuration["Jwt:Audience"],
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero
                    }, out SecurityToken validatedToken);

                // Token geçerli. User ID'yi çıkar.
                var userId = principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                    return (false, null, "Token içerisinde user ID bulunamadı.");

                // User ID'nin veritabanında olup olmadığını kontrol et.
                var userExists = await _context.Users.AnyAsync(u => u.Id == Guid.Parse(userId));
                if (!userExists)
                    return (false, null, "Kullanıcı bulunamadı.");

                return (true, userId, null);
            }
            catch (Exception ex)
            {
                return (false, null, $"Token doğrulama başarısız: {ex.Message}");
            }
        }

        /// <summary>
        /// Kullanıcıyı ID'sine göre getir.
        /// </summary>
        public async Task<dynamic?> GetUserByIdAsync(string userId)
        {
            if (!Guid.TryParse(userId, out var guidId))
                return null;

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == guidId);

            // Şifre hash'i döndürme (güvenlik).
            if (user != null)
                return new
                {
                    user.Id,
                    user.Email,
                    user.DisplayName,
                    user.Avatar,
                    user.CreatedAt
                };

            return null;
        }

        /// <summary>
        /// Tüm kullanıcıları listeler.
        /// </summary>
        public async Task<List<UserInfo>> GetAllUsersAsync()
        {
            return await _context.Users
                // Sadece okuyacağız, değiştirmeyeceğiz: EF'in değişiklik takibini kapatmak daha hızlıdır.
                .AsNoTracking()
                .OrderByDescending(u => u.CreatedAt)
                // Select ile sadece gereken sütunları çekiyoruz; PasswordHash veritabanından hiç okunmaz.
                .Select(u => new UserInfo
                {
                    Id = u.Id,
                    Email = u.Email,
                    DisplayName = u.DisplayName,
                    Avatar = u.Avatar
                })
                .ToListAsync();
        }

        // Email karşılaştırmaları büyük/küçük harfe takılmasın diye tek formata çeviririz.
        private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

        /// <summary>
        /// JWT token'ı oluşturur.
        ///
        /// YAPISI:
        /// {
        ///   header: { alg: "HS256", typ: "JWT" },
        ///   payload: { sub: userId, email, iat, exp },
        ///   signature: HMAC-SHA256(header.payload, secret)
        /// }
        ///
        /// Token'ın geçerlilik süresi: 60 dakika (appsettings.json'dan oku)
        /// </summary>
        private string GenerateJwtToken(string userId, string email)
        {
            // Gizli anahtarı oku.
            var secret = _configuration["Jwt:Secret"] ?? "";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

            // İmzalama kimliğini oluştur.
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Token içeriğini oluştur (claims = veriler).
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Email, email),
                new Claim("userId", userId) // Extra field
            };

            // Geçerlilik süresini oku.
            var expiryMinutes = int.Parse(_configuration["Jwt:ExpiryMinutes"] ?? "60");

            // Token'ı oluştur.
            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: creds
            );

            // Token'ı string'e çevir.
            var tokenHandler = new JwtSecurityTokenHandler();
            return tokenHandler.WriteToken(token);
        }
    }
}
