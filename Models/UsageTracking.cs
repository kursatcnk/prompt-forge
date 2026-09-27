namespace PromptForge.Api.Models
{
    /// <summary>
    /// Kullanıcıların AI API'lerini ne kadar kullandığını ve ne kadar para harcadığını takip eder.
    /// Faturalandırma, kota yönetimi ve maliyet analizi için önemlidir.
    ///
    /// Örn: Ahmet "gpt" modelini kullanarak 5 optimizasyon yaptı, 750 token kullandı, $0.01 harcadı.
    /// </summary>
    public class UsageTracking
    {
        /// <summary>
        /// Kullanım kaydının benzersiz tanımlayıcısı.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Bu optimizasyonu yapan kullanıcının ID'si.
        /// Foreign Key: Users(Id) tablosuna bağlıdır.
        /// Hangi kullanıcı ne kadar harcadığını hesaplamak için kullanılır.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Hangi AI sağlayıcısının kullanıldığı.
        /// Değerler: "openai", "claude", "gemini", "deepseek"
        ///
        /// Farklı sağlayıcıların farklı ücretlendirmesi vardır:
        /// - OpenAI GPT-4: $0.03 per 1K input + $0.06 per 1K output
        /// - Claude 3 Opus: $15 per 1M input + $75 per 1M output
        /// - Gemini: $0.5 per 1M tokens
        ///
        /// Aylık raporlamada sağlayıcı bazında maliyet görmek için gereklidir.
        /// </summary>
        public string? Provider { get; set; }

        /// <summary>
        /// Bu API çağrısında kullanılan token sayısı.
        /// Token = dilbilimsel birim, yaklaşık 4 karakter = 1 token.
        ///
        /// Maliyet hesaplaması:
        /// TokenCount * (ProviderRate per Token) = Cost
        ///
        /// Örn: OpenAI'de 750 token = $0.01 (yaklaşık)
        /// </summary>
        public int? TokensUsed { get; set; }

        /// <summary>
        /// Kaç adet API çağrısı yapıldığı.
        /// Rate limiting ve kota kontrol için izlenir.
        /// Normalde 1'dir (bir prompt = bir çağrı).
        ///
        /// Not: Eğer retry var ise > 1 olabilir.
        /// Varsayılan: 1
        /// </summary>
        public int ApiCallCount { get; set; } = 1;

        /// <summary>
        /// Bu API çağrısının maliyeti (USD cinsinden).
        ///
        /// Hesaplanması:
        /// Cost = TokensUsed * ProviderRate
        ///
        /// Örnekler:
        /// - OpenAI, 750 token: $0.01
        /// - Claude, 1000 token: $0.015
        /// - Gemini, 500 token: $0.00025
        ///
        /// Aylık faturalandırma için toplamı hesaplamak için kullanılır.
        /// </summary>
        public decimal? Cost { get; set; }

        /// <summary>
        /// Bu API çağrısının yapıldığı tarih ve saat (UTC).
        ///
        /// Veri analizi için önemlidir:
        /// - Saatlik pik zamanları bul
        /// - Günlük/haftalık/aylık trendleri takip et
        /// - Sezonsal kullanım değişimlerini analiz et
        /// </summary>
        public DateTime Date { get; set; }

        // ===== Navigation Property =====

        /// <summary>
        /// Bu kullanım kaydının sahibi olan kullanıcı nesnesi.
        /// Foreign Key ilişkiyi sağlar (UserId -> User.Id).
        /// </summary>
        public User? User { get; set; }
    }
}
