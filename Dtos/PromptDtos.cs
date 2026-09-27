namespace PromptForge.Api.Dtos
{
    // Optimize ekranındaki senaryo/format/dil/politika seçimleri.
    public class PromptProfileDto
    {
        public string? UseCase { get; set; }
        public string? ResponseFormat { get; set; }
        public string? ResponseLanguage { get; set; }
        public bool AskClarifying { get; set; } = true;
        public bool ExposeAssumptions { get; set; } = true;
    }

    // LocalOptimized: tarayıcıdaki kural motorunun sonucu. AI yoksa ya da hata verirse bu dönüyor.
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

    public class OptimizeResponse
    {
        public string Optimized { get; set; } = string.Empty;
        public string Engine { get; set; } = "local"; // model adı ya da "local"
        public bool UsedAi { get; set; }
        public string? Notice { get; set; }
        public UsageDto Usage { get; set; } = new();
    }

    // Alan adları JS'teki kayıt nesnesiyle birebir aynı, frontend hiçbir dönüşüm yapmadan kullanıyor.
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
        public List<List<string>> Issues { get; set; } = new(); // [başlık, açıklama]
        public List<string> Variables { get; set; } = new();
        public Dictionary<string, bool> Checks { get; set; } = new();
        public PromptProfileDto Profile { get; set; } = new();
        public string? Engine { get; set; }
        public DateTime? SavedAt { get; set; } // sadece favori listesinde dolu
    }

    public class UsageDto
    {
        public string Plan { get; set; } = "free";
        public int Used { get; set; }
        public int Limit { get; set; }
        public DateTime ResetsAt { get; set; } // bir sonraki ayın ilk günü, UTC
    }
}
