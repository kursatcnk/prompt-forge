namespace PromptForge.Api.Dtos
{
    /// <summary>
    /// Giriş/Kayıt yanıtı.
    /// Backend şu JSON'u döner:
    /// { "success": true, "token": "eyJhbGc...", "message": "Başarılı" }
    /// </summary>
    public class AuthResponse
    {
        /// <summary>İşlem başarılı mı?</summary>
        public bool Success { get; set; }

        /// <summary>JWT token. Frontend bunu her istek'te gönderir.</summary>
        public string? Token { get; set; }

        /// <summary>Mesaj (hata veya başarı).</summary>
        public string? Message { get; set; }

        /// <summary>Kullanıcı bilgisi (login başarılı ise).</summary>
        public UserInfo? User { get; set; }
    }

    /// <summary>
    /// Kullanıcı bilgisi.
    /// </summary>
    public class UserInfo
    {
        /// <summary>Kullanıcı ID'si.</summary>
        public Guid Id { get; set; }

        /// <summary>E-posta.</summary>
        public string? Email { get; set; }

        /// <summary>Görüntü adı.</summary>
        public string? DisplayName { get; set; }

        /// <summary>Profil resmi URL'si.</summary>
        public string? Avatar { get; set; }
    }
}
