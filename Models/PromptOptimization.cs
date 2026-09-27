namespace PromptForge.Api.Models
{
    public class PromptOptimization
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string? OriginalContent { get; set; }
        public string? OptimizedContent { get; set; }
        public string? TargetModel { get; set; }
        public string? UseCase { get; set; }
        public string? OutputFormat { get; set; }
        public int? HealthScoreOriginal { get; set; }
        public int? HealthScoreOptimized { get; set; }
        public int? TokensOriginal { get; set; }
        public int? TokensOptimized { get; set; }
        public bool ClarifyingQuestions { get; set; }
        public bool AssumptionVisibility { get; set; }
        public string? OptimizationTarget { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public User? User { get; set; }
    }
}
