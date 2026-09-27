namespace PromptForge.Api.Dtos
{
    /// <summary>POST /api/prompts/analyze → { "prompt": "..." }</summary>
    public class AnalyzeRequest
    {
        public string Prompt { get; set; } = string.Empty;
    }

    /// <summary>
    /// Öğretici prompt analizi. Arayüz bu yapıyı doğrudan kartlara çevirir.
    /// Aynı yapı tarayıcıdaki yerel analizde de kullanılır; kaynak "ai" veya "local" ile ayrılır.
    /// </summary>
    public class AnalysisResult
    {
        /// <summary>0-100 genel puan.</summary>
        public int Overall { get; set; }

        /// <summary>Genel değerlendirme (1-2 cümle).</summary>
        public string Summary { get; set; } = string.Empty;

        public List<AnalysisCriterion> Criteria { get; set; } = new();

        /// <summary>Promptun iyi yaptığı şeyler.</summary>
        public List<string> Strengths { get; set; } = new();

        /// <summary>Önem sırasına göre en fazla 3 iyileştirme.</summary>
        public List<string> Improvements { get; set; } = new();

        /// <summary>Önerilerin uygulandığı örnek yeni hâl.</summary>
        public string? Rewrite { get; set; }

        /// <summary>Bu prompttan öğrenilecek tek ilke.</summary>
        public string? Lesson { get; set; }

        /// <summary>"ai" veya "local".</summary>
        public string Source { get; set; } = "ai";

        public string? Engine { get; set; }

        public UsageDto? Usage { get; set; }
    }

    public class AnalysisCriterion
    {
        /// <summary>clarity, context, specificity, constraints, output, efficiency</summary>
        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        /// <summary>0-10</summary>
        public int Score { get; set; }
        /// <summary>good, improve, missing</summary>
        public string Status { get; set; } = "improve";
        /// <summary>Neden bu puan: promptun içinden somut atıfla.</summary>
        public string Feedback { get; set; } = string.Empty;
        /// <summary>Nasıl düzeltilir: uygulanabilir tek öneri.</summary>
        public string Fix { get; set; } = string.Empty;
    }
}
