namespace PromptForge.Api.Models
{
    /// <summary>
    /// Harici AI sağlayıcılarının API anahtarlarını güvenli şekilde saklar.
    /// OpenAI, Claude, Gemini, DeepSeek gibi sağlayıcılar için şifreli anahtarlar tutar.
    ///
    /// GÜVENLIK ÖNEMLİ:
    /// - API anahtarları asla düz metin olarak log'a yazılmaz.
    /// - Frontend'e hiçbir zaman gönderilmez.
    /// - Sadece backend sunucuda kullanılır.
    /// - Şifreli olarak veritabanında tutulur.
    /// </summary>
    public class ApiKey
    {
        /// <summary>
        /// API anahtarı kaydının benzersiz tanımlayıcısı.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// AI sağlayıcısı adı.
        /// Değerler: "openai", "claude", "gemini", "deepseek"
        /// Unique constraint var - her sağlayıcının sadece bir anahtarı olabilir.
        /// </summary>
        public string? Provider { get; set; }

        /// <summary>
        /// Şifreli API anahtarı.
        ///
        /// Format: Algoritma tarafından şifreli metin.
        /// Şifreleme: ASP.NET Core'un Data Protection API (DPAPI) kullanılır.
        ///
        /// Kullanım süreci:
        /// 1. Anahtarı kaydederken: PlainKey → Encrypt → DB'ye kaydet
        /// 2. Anahtarı kullanırken: DB'den oku → Decrypt → API çağrısı yap
        ///
        /// Örn: OpenAI anahtarı "sk-..." formatında
        /// </summary>
        public string? EncryptedKey { get; set; }

        /// <summary>
        /// API anahtarının veritabanına kaydedilme tarihi (UTC).
        /// Anahtarların yaşını ve rotasyonunu takip etmek için kullanılabilir.
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }
}
