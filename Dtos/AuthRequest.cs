namespace PromptForge.Api.Dtos
{
    /// <summary>
    /// Kayıt istemi.
    /// Frontend şu JSON'u POST /api/auth/register gönderir:
    /// { "email": "user@example.com", "password": "123456", "displayName": "Ahmet" }
    /// </summary>
    public class RegisterRequest
    {
        /// <summary>Kullanıcının e-postası.</summary>
        public string? Email { get; set; }

        /// <summary>Şifre (en az 6 karakter).</summary>
        public string? Password { get; set; }

        /// <summary>Görüntü adı (opsiyonel, email'den türetilebilir).</summary>
        public string? DisplayName { get; set; }
    }

    /// <summary>
    /// Giriş istemi.
    /// Frontend şu JSON'u POST /api/auth/login gönderir:
    /// { "email": "user@example.com", "password": "123456" }
    /// </summary>
    public class LoginRequest
    {
        /// <summary>Kullanıcının e-postası.</summary>
        public string? Email { get; set; }

        /// <summary>Şifre.</summary>
        public string? Password { get; set; }
    }
}
