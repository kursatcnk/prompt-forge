namespace PromptForge.Api.Dtos
{
    public class AnalyzeRequest
    {
        public string Prompt { get; set; } = string.Empty;
    }

    // Tarayıcıdaki yerel analiz de aynı şekli üretiyor; arayüz ikisini aynı kartlarla çiziyor (Source ile ayrılıyor).
    public class AnalysisResult
    {
        public int Overall { get; set; } // 0-100
        public string Summary { get; set; } = string.Empty;
        public List<AnalysisCriterion> Criteria { get; set; } = new();
        public List<string> Strengths { get; set; } = new();
        public List<string> Improvements { get; set; } = new(); // en önemlisi başta, en fazla 3
        public string? Rewrite { get; set; }
        public string? Lesson { get; set; }
        public string Source { get; set; } = "ai"; // ai | local
        public string? Engine { get; set; }
        public UsageDto? Usage { get; set; }
    }

    public class AnalysisCriterion
    {
        public string Key { get; set; } = string.Empty; // clarity, context, specificity, constraints, output, efficiency
        public string Label { get; set; } = string.Empty;
        public int Score { get; set; } // 0-10
        public string Status { get; set; } = "improve"; // good | improve | missing
        public string Feedback { get; set; } = string.Empty;
        public string Fix { get; set; } = string.Empty;
    }
}
