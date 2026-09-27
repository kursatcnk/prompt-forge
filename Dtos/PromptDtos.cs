namespace PromptForge.Api.Dtos
{
    /// <summary>Optimize ekranındaki derleme profili (senaryo, format, dil, politikalar).</summary>
    public class PromptProfileDto
    {
        public string? UseCase { get; set; }
        public string? ResponseFormat { get; set; }
        public string? ResponseLanguage { get; set; }
        public bool AskClarifying { get; set; } = true;
        public bool ExposeAssumptions { get; set; } = true;
    }

    /// <summary>
    /// POST /api/prompts/optimize isteği.
    /// LocalOptimized: tarayıcıdaki kural motorunun ürettiği sürüm. AI anahtarı yoksa veya AI hata verirse bu kullanılır.
    /// </summary>
    public class OptimizeRequest
    {
        public string Original { get; set; } = string.Empty;
        public string Model { get; set; } = "universal";
        public string Goal { get; set; } = "balanced";
        public PromptProfileDto Profile { get; set; } = new();
        public string LocalOptimized { get; set; } = string.Empty;
        public List<string> Requirements { get; set; } = new();
        public List<string> Variables { get; set; } = new();
    }

    /// <summary>Optimize cevabı: yeni prompt, hangi motorun ürettiği ve güncel kota durumu.</summary>
    public class OptimizeResponse
    {
        public string Optimized { get; set; } = string.Empty;
        /// <summary>Üreten motor: AI model adı (örn. "claude-opus-5") veya "local".</summary>
        public string Engine { get; set; } = "local";
        public bool UsedAi { get; set; }
        /// <summary>Kullanıcıya gösterilecek bilgi notu (örn. "AI anahtarı yok, yerel motor kullanıldı").</summary>
        public string? Notice { get; set; }
        public UsageDto Usage { get; set; } = new();
    }

    /// <summary>
    /// Geçmiş/favori kaydı. Alan adları arayüzdeki kayıt nesnesiyle birebir aynıdır;
    /// böylece JavaScript tarafı hiçbir dönüştürme yapmadan kullanabilir.
    /// </summary>
    public class PromptRecordDto
    {
        public string? Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Model { get; set; } = "universal";
        public string Goal { get; set; } = "balanced";
        public string Original { get; set; } = string.Empty;
        public string Optimized { get; set; } = string.Empty;
        public int BeforeTokens { get; set; }
        public int AfterTokens { get; set; }
        public int BeforeHealth { get; set; }
        public int AfterHealth { get; set; }
        public List<string> Requirements { get; set; } = new();
        /// <summary>Her sorun [başlık, açıklama] şeklinde iki elemanlı liste.</summary>
        public List<List<string>> Issues { get; set; } = new();
        public List<string> Variables { get; set; } = new();
        public Dictionary<string, bool> Checks { get; set; } = new();
        public PromptProfileDto Profile { get; set; } = new();
        public string? Engine { get; set; }
        /// <summary>Favorilere eklenme zamanı (sadece favori listesinde dolu gelir).</summary>
        public DateTime? SavedAt { get; set; }
    }

    /// <summary>Aylık kullanım ve plan kotası.</summary>
    public class UsageDto
    {
        public string Plan { get; set; } = "free";
        public int Used { get; set; }
        public int Limit { get; set; }
        /// <summary>Kotanın sıfırlanacağı tarih (bir sonraki ayın ilk günü, UTC).</summary>
        public DateTime ResetsAt { get; set; }
    }
}
