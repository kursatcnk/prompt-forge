namespace PromptForge.Api.Dtos
{
    /// <summary>
    /// Kullanıcının kayıtlı tercihleri. Alan adları arayüzdeki state (PF.state) ile birebir aynıdır.
    /// </summary>
    public class SettingsDto
    {
        public string Model { get; set; } = "gpt";
        public string Goal { get; set; } = "balanced";
        public string Theme { get; set; } = "light";
        public string Density { get; set; } = "comfortable";
        /// <summary>"on" veya "off"</summary>
        public string Motion { get; set; } = "on";
        public string UseCase { get; set; } = "general";
        public string ResponseFormat { get; set; } = "auto";
        public string ResponseLanguage { get; set; } = "prompt";
        public bool AskClarifying { get; set; } = true;
        public bool ExposeAssumptions { get; set; } = true;
    }

    /// <summary>Optimize işini hangi AI yapacak? Arayüz bunu "AI bağlı / Yerel mod" olarak gösterir.</summary>
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

    /// <summary>GET /api/account/me → arayüzün açılışta ihtiyaç duyduğu her şey tek istekte.</summary>
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

    /// <summary>Hassas işlemlerde (2FA kapatma, hesap silme) şifre tekrar istenir.</summary>
    public class PasswordConfirmRequest
    {
        public string? Password { get; set; }
    }

    public class TwoFactorSetupResponse
    {
        /// <summary>QR okutulamazsa elle girilecek anahtar.</summary>
        public string Secret { get; set; } = string.Empty;
        public string QrDataUrl { get; set; } = string.Empty;
        public string OtpAuthUri { get; set; } = string.Empty;
    }
}
