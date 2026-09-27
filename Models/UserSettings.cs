namespace PromptForge.Api.Models
{
    public class UserSettings
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string? DefaultModel { get; set; } = "gpt";
        public string? DefaultOptimizationTarget { get; set; } = "balanced";
        public string? Theme { get; set; } = "light";
        public string? Density { get; set; } = "comfortable";
        public bool MotionEnabled { get; set; } = true;
        public string? Language { get; set; } = "tr";

        public User? User { get; set; }
    }
}
