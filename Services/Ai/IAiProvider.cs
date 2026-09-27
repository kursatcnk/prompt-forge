namespace PromptForge.Api.Services.Ai
{
    /// <summary>
    /// Tüm AI sağlayıcılarının uyduğu ortak sözleşme (adaptör deseni).
    /// Optimize servisi sadece bu arayüzü bilir; Claude mı GPT mi olduğuyla ilgilenmez.
    /// </summary>
    public interface IAiProvider
    {
        /// <summary>Ayar anahtarı ve kayıt adı: "anthropic", "openai", "gemini", "deepseek".</summary>
        string Key { get; }

        /// <summary>Kullanıcıya gösterilecek ad: "Claude", "GPT"...</summary>
        string DisplayName { get; }

        /// <summary>Kullanılacak model adı (appsettings veya user secrets'tan).</summary>
        string Model { get; }

        /// <summary>API anahtarı tanımlı mı? Tanımlı değilse bu sağlayıcı hiç çağrılmaz.</summary>
        bool IsConfigured { get; }

        /// <summary>
        /// Sistem talimatı + kullanıcı mesajı gönderir, modelin metin cevabını döner.
        /// jsonOutput: true ise sağlayıcı destekliyorsa cevabın geçerli JSON olması zorunlu tutulur (analiz gibi yapılandırılmış sonuçlar için).
        /// </summary>
        Task<AiCompletion> CompleteAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken, bool jsonOutput = false);
    }

    /// <summary>Bir AI çağrısının sonucu: üretilen metin ve harcanan token sayıları.</summary>
    public record AiCompletion(string Text, int InputTokens, int OutputTokens, string Model);

    /// <summary>
    /// Sağlayıcı hatası. Mesaj kullanıcıya gösterilebilecek şekilde Türkçe ve güvenlidir
    /// (API anahtarı veya ham hata detayı içermez).
    /// </summary>
    public class AiProviderException : Exception
    {
        public AiProviderException(string message, Exception? inner = null) : base(message, inner) { }
    }

    /// <summary>appsettings.json → "AI:{Sağlayıcı}" bölümü.</summary>
    public class AiProviderOptions
    {
        public string? ApiKey { get; set; }
        public string? Model { get; set; }
    }
}
