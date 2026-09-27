namespace PromptForge.Api.Dtos
{
    /// <summary>POST /api/auth/register → { "email", "password", "displayName" }</summary>
    public class RegisterRequest
    {
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? DisplayName { get; set; }
    }

    /// <summary>POST /api/auth/login → { "email", "password" }</summary>
    public class LoginRequest
    {
        public string? Email { get; set; }
        public string? Password { get; set; }
    }

    /// <summary>
    /// POST /api/auth/login/two-factor → { "ticket", "code" }
    /// Ticket: şifre doğru girilince verilen kısa ömürlü bilet. Code: authenticator uygulamasındaki 6 haneli kod.
    /// </summary>
    public class TwoFactorLoginRequest
    {
        public string? Ticket { get; set; }
        public string? Code { get; set; }
        public bool Remember { get; set; } = true;
    }

    /// <summary>POST /api/auth/forgot-password → { "email" }</summary>
    public class ForgotPasswordRequest
    {
        public string? Email { get; set; }
    }

    /// <summary>POST /api/auth/reset-password → { "email", "token", "newPassword" } (e-postadaki bağlantıdan gelir)</summary>
    public class ResetPasswordRequest
    {
        public string? Email { get; set; }
        public string? Token { get; set; }
        public string? NewPassword { get; set; }
    }
}
