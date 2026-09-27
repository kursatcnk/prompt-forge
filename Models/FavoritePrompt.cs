namespace PromptForge.Api.Models
{
    public class FavoritePrompt
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid PromptOptimizationId { get; set; }
        public DateTime SavedAt { get; set; }
        public string? CustomName { get; set; }

        public User? User { get; set; }
        public PromptOptimization? PromptOptimization { get; set; }
    }
}
