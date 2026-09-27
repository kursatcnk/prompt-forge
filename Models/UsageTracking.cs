namespace PromptForge.Api.Models
{
    public class UsageTracking
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string? Provider { get; set; }
        public int? TokensUsed { get; set; }
        public int ApiCallCount { get; set; } = 1;
        public decimal? Cost { get; set; }
        public DateTime Date { get; set; }

        public User? User { get; set; }
    }
}
