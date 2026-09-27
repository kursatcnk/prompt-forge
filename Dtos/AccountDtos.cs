namespace PromptForge.Api.Dtos
{
    // Alan adları JS tarafındaki PF.state ile aynı, arada dönüştürme yok.
    public class SettingsDto
    {
        public string Model { get; set; } = "gpt";
        public string Goal { get; set; } = "balanced";
        public string Theme { get; set; } = "light";
        public string Density { get; set; } = "comfortable";
        public string Motion { get; set; } = "on"; // on | off
        public string UseCase { get; set; } = "general";
        public string ResponseFormat { get; set; } = "auto";
        public string ResponseLanguage { get; set; } = "prompt";
        public bool AskClarifying { get; set; } = true;
        public bool ExposeAssumptions { get; set; } = true;
    }

    public class AiStatusDto
    {
        public bool Enabled { get; set; }
        public string? Provider { get; set; }
        public string? Model { get; set; }
    }

    public class PlanDto
    {
        public string Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int MonthlyLimit { get; set; }
        public string Price { get; set; } = string.Empty;
        public string[] Features { get; set; } = Array.Empty<string>();
    }

    // GET /api/account/me: arayüz açılışta ihtiyaç duyduğu her şeyi tek istekte alıyor.
    public class MeResponse
    {
        public UserInfo User { get; set; } = new();
        public SettingsDto Settings { get; set; } = new();
        public UsageDto Usage { get; set; } = new();
        public AiStatusDto Ai { get; set; } = new();
        public List<PlanDto> Plans { get; set; } = new();
    }

    public class UpdateProfileRequest
    {
        public string? DisplayName { get; set; }
    }

    public class ChangePasswordRequest
    {
        public string? CurrentPassword { get; set; }
        public string? NewPassword { get; set; }
    }

    public class CodeRequest
    {
        public string? Code { get; set; }
    }

    // 2FA kapatma ve hesap silme gibi işlemlerde şifre tekrar soruluyor.
    public class PasswordConfirmRequest
    {
        public string? Password { get; set; }
    }

    public class TwoFactorSetupResponse
    {
        public string Secret { get; set; } = string.Empty; // QR okutulamazsa elle girilecek
        public string QrDataUrl { get; set; } = string.Empty;
        public string OtpAuthUri { get; set; } = string.Empty;
    }
}
