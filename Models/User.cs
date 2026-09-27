namespace PromptForge.Api.Models
{
    public class User
    {
        public Guid Id { get; set; }

        // Hep küçük harfe çevrilmiş hâliyle saklanıyor (UserService.NormalizeEmail), unique index var.
        public string Email { get; set; } = string.Empty;

        // BCrypt hash; düz şifre hiçbir yerde tutulmuyor.
        public string PasswordHash { get; set; } = string.Empty;

        public string? DisplayName { get; set; }
        public string? Avatar { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // false olan hesap giriş yapamıyor. Şimdilik elle kapatmak için, arayüzde karşılığı yok.
        public bool IsActive { get; set; }

        public bool EmailConfirmed { get; set; }

        public bool TwoFactorEnabled { get; set; }
        // Data Protection ile şifreli. Kurulum yarıda kalırsa dolu olup TwoFactorEnabled false olabilir.
        public string? TwoFactorSecret { get; set; }

        // free | pro; aylık kotayı belirliyor (UsageService.Plans).
        public string Plan { get; set; } = "free";

        public ICollection<PromptOptimization> PromptOptimizations { get; set; } = new List<PromptOptimization>();
        public ICollection<FavoritePrompt> FavoritePrompts { get; set; } = new List<FavoritePrompt>();
        public UserSettings? Settings { get; set; }
    }
}
