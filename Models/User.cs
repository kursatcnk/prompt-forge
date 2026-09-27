namespace PromptForge.Api.Models
{
    public class User
    {
        public Guid Id { get; set; }
        public string? Email { get; set; }
        public string? PasswordHash { get; set; }
        public string? DisplayName { get; set; }
        public string? Avatar { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsActive { get; set; }

        public ICollection<PromptOptimization> PromptOptimizations { get; set; } = new List<PromptOptimization>();
        public ICollection<FavoritePrompt> FavoritePrompts { get; set; } = new List<FavoritePrompt>();
        public UserSettings? Settings { get; set; }
    }
}
