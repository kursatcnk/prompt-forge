namespace PromptForge.Api.Dtos
{
    // Normal giriş: { success, token, user }
    // 2FA açıksa: { success, requiresTwoFactor: true, twoFactorTicket } ve arayüz kod ekranına geçiyor.
    public class AuthResponse
    {
        public bool Success { get; set; }
        public string? Token { get; set; }
        public string? Message { get; set; }
        public UserInfo? User { get; set; }
        public bool RequiresTwoFactor { get; set; }
        public string? TwoFactorTicket { get; set; }
    }

    // Dışarıya giden kullanıcı modeli; hash ve 2FA secret burada yok.
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

    // Aylık kota dolunca 429 ile dönüyor. Code alanı, dakikalık istek sınırının 429'undan ayırt etmek için.
    public class QuotaExceededResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Code { get; set; } = "quota_exceeded";
        public UsageDto Usage { get; set; } = new();
    }

    public class MessageResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }

        public static MessageResponse Ok(string message) => new() { Success = true, Message = message };
        public static MessageResponse Fail(string message) => new() { Success = false, Message = message };
    }
}
