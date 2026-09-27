namespace PromptForge.Api.Models
{
    // Kullanıcı başına tek satır (UserId unique). Arayüz açılınca en son kaldığı ayarlarla devam ediyor.
    public class UserSettings
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }

        public string? DefaultModel { get; set; } = "gpt";                 // gpt, claude, gemini, deepseek, universal
        public string? DefaultOptimizationTarget { get; set; } = "balanced"; // quality, balanced, lean
        public string? Theme { get; set; } = "light";                      // light, dark, system
        public string? Density { get; set; } = "comfortable";              // comfortable, compact
        public bool MotionEnabled { get; set; } = true;
        public string? Language { get; set; } = "tr";

        // Optimize ekranındaki son seçimler
        public string? UseCase { get; set; } = "general";        // general, coding, research, content, data
        public string? ResponseFormat { get; set; } = "auto";    // auto, markdown, json, table, checklist, code
        public string? ResponseLanguage { get; set; } = "prompt"; // prompt, tr, en
        public bool AskClarifying { get; set; } = true;
        public bool ExposeAssumptions { get; set; } = true;

        public User? User { get; set; }
    }
}
