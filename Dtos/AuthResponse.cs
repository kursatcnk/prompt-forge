namespace PromptForge.Api.Dtos
{
    /// <summary>
    /// Giriş/Kayıt yanıtı.
    /// Normal giriş: { success: true, token, user }
    /// 2 adımlı doğrulama açıksa: { success: true, requiresTwoFactor: true, twoFactorTicket } → kod ekranına geçilir.
    /// </summary>
    public class AuthResponse
    {
        public bool Success { get; set; }
        public string? Token { get; set; }
        public string? Message { get; set; }
        public UserInfo? User { get; set; }
        public bool RequiresTwoFactor { get; set; }
        public string? TwoFactorTicket { get; set; }
    }

    /// <summary>Dışarıya gösterilebilecek kullanıcı bilgisi (şifre hash'i ve 2FA anahtarı asla burada olmaz).</summary>
    public class UserInfo
    {
        public Guid Id { get; set; }
        public string? Email { get; set; }
        public string? DisplayName { get; set; }
        public string? Avatar { get; set; }
        public bool EmailConfirmed { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public string Plan { get; set; } = "free";
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>Kota dolduğunda dönen cevap: mesaj + güncel kullanım (arayüz geri sayımı buradan gösterir).</summary>
    public class QuotaExceededResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Code { get; set; } = "quota_exceeded";
        public UsageDto Usage { get; set; } = new();
    }

    /// <summary>Basit başarı/hata cevabı: { success, message }</summary>
    public class MessageResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }

        public static MessageResponse Ok(string message) => new() { Success = true, Message = message };
        public static MessageResponse Fail(string message) => new() { Success = false, Message = message };
    }
}
