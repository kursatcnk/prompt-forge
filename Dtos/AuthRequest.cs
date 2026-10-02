namespace PromptForge.Api.Dtos
{
    public class RegisterRequest
    {
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? DisplayName { get; set; }
    }

    public class LoginRequest
    {
        public string? Email { get; set; }
        public string? Password { get; set; }
        // Beni hatırla: kalıcı çerez mi, tarayıcı kapanınca giden oturum çerezi mi.
        public bool? RememberMe { get; set; }
    }

    // Ticket: şifre doğru girilince verilen 5 dakikalık bilet. Code: authenticator'daki 6 hane.
    public class TwoFactorLoginRequest
    {
        public string? Ticket { get; set; }
        public string? Code { get; set; }
        public bool? RememberMe { get; set; }
    }

    public class ForgotPasswordRequest
    {
        public string? Email { get; set; }
    }

    // Maildeki linkten geliyor.
    public class ResetPasswordRequest
    {
        public string? Email { get; set; }
        public string? Token { get; set; }
        public string? NewPassword { get; set; }
    }
}
