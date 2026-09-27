namespace PromptForge.Api.Models
{
    // Geçmişteki bir optimizasyon: önce/sonra metin, puanlar ve o anki ayarlar.
    public class PromptOptimization
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }

        public string? OriginalContent { get; set; }
        public string? OptimizedContent { get; set; }

        public string? TargetModel { get; set; }        // gpt, claude, gemini, deepseek, universal
        public string? UseCase { get; set; }            // general, coding, research, content, data
        public string? OutputFormat { get; set; }       // auto, markdown, json, table, checklist, code
        public string? OptimizationTarget { get; set; } // quality, balanced, lean
        public string? ResponseLanguage { get; set; }   // prompt, tr, en

        // Tarayıcıdaki kalite kontrolünün puanları (0-100) ve tahmini token sayıları.
        public int? HealthScoreOriginal { get; set; }
        public int? HealthScoreOptimized { get; set; }
        public int? TokensOriginal { get; set; }
        public int? TokensOptimized { get; set; }

        public bool ClarifyingQuestions { get; set; }
        public bool AssumptionVisibility { get; set; }

        // Sonucu kim üretti: model adı ya da "local".
        public string? Engine { get; set; }

        // Gereksinimler, sorunlar, değişkenler, yapı kontrolleri. Arayüz kaydı tekrar açarken kullanıyor;
        // üzerinde sorgu yapmadığım için ayrı tablo açmadım.
        public string? DetailsJson { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public User? User { get; set; }
    }
}
