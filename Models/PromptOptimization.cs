namespace PromptForge.Api.Models
{
    /// <summary>
    /// Prompt optimizasyonu kaydı. Kullanıcının ham promptunu optimize ettikten sonra oluşan verinin kaydıdır.
    /// Orijinal metin, optimize edilmiş metin, kalite metrikleri ve işlem ayarlarını içerir.
    /// </summary>
    public class PromptOptimization
    {
        /// <summary>
        /// Optimizasyon kaydının benzersiz tanımlayıcısı.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Bu optimizasyonun sahibi olan kullanıcının ID'si.
        /// Foreign Key: Users(Id) tablosuna bağlıdır.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Kullanıcının optimize etmeden önce yazdığı ham prompt metni.
        /// Orijinal durumu korur; optimize edilmemiş halidir.
        /// </summary>
        public string? OriginalContent { get; set; }

        /// <summary>
        /// AI tarafından optimize edilen prompt metni.
        /// Yapı, açıklık ve model uyumluluğu açısından iyileştirilmiş halidir.
        /// </summary>
        public string? OptimizedContent { get; set; }

        /// <summary>
        /// Hedef AI modeli: "gpt", "claude", "gemini", "deepseek" veya "universal".
        /// Optimizasyon bu modele göre uyarlanır.
        /// Örn: GPT daha yapılandırılmış prompt ister, Claude daha açık bağlam ister.
        /// </summary>
        public string? TargetModel { get; set; }

        /// <summary>
        /// Kullanım senaryosu: "general", "coding", "research", "content", "data".
        /// Optimizasyon stratejisini senaryoya göre ayarlar.
        /// Kod: yapılabilirlik ve doğruluk vurgular.
        /// Araştırma: kanıt ayrılması ve belirsizlik gösterimine dikkat.
        /// </summary>
        public string? UseCase { get; set; }

        /// <summary>
        /// İstenen çıktı formatı: "auto", "markdown", "json", "table", "checklist", "code".
        /// Optimize edilen prompta çıktı format talimatı eklenir.
        /// </summary>
        public string? OutputFormat { get; set; }

        /// <summary>
        /// Orijinal promptun sağlık skoru (0-100).
        /// Netlik, yapı, bütünlük ve spesifiklik açısından değerlendirilir.
        /// </summary>
        public int? HealthScoreOriginal { get; set; }

        /// <summary>
        /// Optimize edilen promptun sağlık skoru (0-100).
        /// Optimizasyonun iyileştirme yüzdesini gösterir.
        /// </summary>
        public int? HealthScoreOptimized { get; set; }

        /// <summary>
        /// Orijinal promptun tahmini token sayısı.
        /// Token = kelime parçası. AI maliyeti token sayısıyla hesaplanır.
        /// </summary>
        public int? TokensOriginal { get; set; }

        /// <summary>
        /// Optimize edilen promptun tahmini token sayısı.
        /// Token tasarrufu gösterir (OriginalTokens - OptimizedTokens).
        /// </summary>
        public int? TokensOptimized { get; set; }

        /// <summary>
        /// Kritik bilgi eksikse AI'ın optimize etmeden önce sorular sorup sormayacağı.
        /// True: Netleştirme soruları sor. False: Var olan bilgiyle işlem yap.
        /// </summary>
        public bool ClarifyingQuestions { get; set; }

        /// <summary>
        /// Optimize edilen promptta yapılan varsayımları göster mi?
        /// True: Varsayımları highlight et. False: Sessiz çalış.
        /// </summary>
        public bool AssumptionVisibility { get; set; }

        /// <summary>
        /// Optimizasyon hedefi: "best", "balanced", "minimal".
        /// best: Maksimum kalite, daha uzun prompt.
        /// balanced: Kalite ve uzunluğun dengesi (varsayılan).
        /// minimal: En kısa prompt, yine de işlevsel.
        /// </summary>
        public string? OptimizationTarget { get; set; }

        /// <summary>
        /// Optimizasyon işleminin yapıldığı tarih ve saat (UTC).
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Kaydın son güncellenme tarihi (değiştirilir mi?).
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        // ===== Navigation Property =====

        /// <summary>
        /// Bu optimizasyonun sahibi olan kullanıcı nesnesi.
        /// Foreign Key ilişkiyi sağlar (UserId -> User.Id).
        /// </summary>
        public User? User { get; set; }
    }
}
