using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace PromptForge.Api.Services.Ai
{
    /// <summary>
    /// Google Gemini adaptörü ("generateContent" REST uç noktası).
    /// Gemini'de sistem talimatı ayrı bir alanda (systemInstruction) gönderilir.
    ///
    /// MODEL YEDEKLEME: Ücretsiz kademede her modelin günlük kotası ayrıdır ve bazıları çok düşüktür (örn. günde 20 istek).
    /// Bir modelin kotası dolarsa (429) veya model yoğunsa (503), istek listedeki sıradaki modele gönderilir.
    /// </summary>
    public class GeminiProvider : IAiProvider
    {
        private static readonly string[] DefaultFallbacks = { "gemini-3.1-flash-lite", "gemini-3.8-flash" };

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly AiProviderOptions _options;
        private readonly string[] _fallbackModels;
        private readonly ILogger<GeminiProvider> _logger;

        public GeminiProvider(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<GeminiProvider> logger)
        {
            _httpClientFactory = httpClientFactory;
            _options = configuration.GetSection("AI:Gemini").Get<AiProviderOptions>() ?? new();
            // appsettings.json → AI:Gemini:FallbackModels ile değiştirilebilir.
            _fallbackModels = configuration.GetSection("AI:Gemini:FallbackModels").Get<string[]>() ?? DefaultFallbacks;
            _logger = logger;
        }

        public string Key => "gemini";
        public string DisplayName => "Gemini";
        public string Model => string.IsNullOrWhiteSpace(_options.Model) ? "gemini-3.5-flash-lite" : _options.Model;
        public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.ApiKey);

        public async Task<AiCompletion> CompleteAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken, bool jsonOutput = false)
        {
            var body = new JsonObject
            {
                ["systemInstruction"] = new JsonObject { ["parts"] = new JsonArray { new JsonObject { ["text"] = systemPrompt } } },
                ["contents"] = new JsonArray
                {
                    new JsonObject { ["role"] = "user", ["parts"] = new JsonArray { new JsonObject { ["text"] = userMessage } } }
                }
            };
            // Gemini'ye cevabı doğrudan JSON olarak üretmesini söyler; metnin içinden JSON ayıklamak gerekmez.
            if (jsonOutput)
                body["generationConfig"] = new JsonObject { ["responseMimeType"] = "application/json" };

            var models = new[] { Model }.Concat(_fallbackModels).Distinct().ToList();
            AiProviderException? lastError = null;
            foreach (var model in models)
            {
                try
                {
                    return await CallModelAsync(model, body, cancellationToken);
                }
                catch (GeminiCapacityException ex)
                {
                    // Kota dolu veya model yoğun: sıradaki modeli dene.
                    _logger.LogWarning("Gemini modeli {Model} kullanılamadı ({Status}), sıradaki modele geçiliyor.", model, ex.Status);
                    lastError = new AiProviderException(ex.Status == HttpStatusCode.TooManyRequests
                        ? "Gemini'nin ücretsiz kotası bugün için doldu. Yarın tekrar dene veya başka bir AI anahtarı ekle."
                        : "Gemini sunucuları şu an yoğun. Birazdan tekrar dene.");
                }
            }
            throw lastError ?? new AiProviderException("Gemini isteği tamamlanamadı.");
        }

        private async Task<AiCompletion> CallModelAsync(string model, JsonObject body, CancellationToken cancellationToken)
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{Uri.EscapeDataString(model)}:generateContent";
            HttpRequestMessage CreateRequest()
            {
                var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(body.ToJsonString(), System.Text.Encoding.UTF8, "application/json")
                };
                // Anahtar URL'de değil başlıkta gönderilir; böylece sunucu loglarına düşmez.
                request.Headers.Add("x-goog-api-key", _options.ApiKey);
                return request;
            }

            HttpResponseMessage response;
            try
            {
                response = await AiHttp.SendWithRetryAsync(_httpClientFactory.CreateClient("ai"), CreateRequest, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                throw new AiProviderException("Gemini sunucusuna bağlanılamadı.", ex);
            }

            using (response)
            {
                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                if (response.StatusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.ServiceUnavailable)
                    throw new GeminiCapacityException(response.StatusCode);
                if (!response.IsSuccessStatusCode)
                    throw new AiProviderException(response.StatusCode switch
                    {
                        HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => "Gemini API anahtarı geçersiz.",
                        // Google eski modelleri zamanla kapatır; bu durumda appsettings.json → AI:Gemini:Model güncellenmeli.
                        HttpStatusCode.NotFound => $"Gemini modeli bulunamadı ({model}). appsettings.json içindeki AI:Gemini:Model adını kontrol et.",
                        _ => $"Gemini isteği tamamlanamadı ({(int)response.StatusCode})."
                    });

                // Cevap: { candidates: [ { content: { parts: [ { text } ] } } ], usageMetadata: {...} }
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (!root.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
                    throw new AiProviderException("Gemini bu promptu işlemedi (güvenlik filtresi olabilir).");

                var parts = candidates[0].GetProperty("content").GetProperty("parts");
                var text = string.Join("\n", parts.EnumerateArray()
                    .Where(p => p.TryGetProperty("text", out _))
                    .Select(p => p.GetProperty("text").GetString())).Trim();
                if (text.Length == 0)
                    throw new AiProviderException("Gemini boş bir cevap döndürdü.");

                var usage = root.TryGetProperty("usageMetadata", out var u) ? u : default;
                int Read(string name) => usage.ValueKind == JsonValueKind.Object && usage.TryGetProperty(name, out var v) ? v.GetInt32() : 0;
                return new AiCompletion(text, Read("promptTokenCount"), Read("candidatesTokenCount"), model);
            }
        }

        // Kota dolu (429) veya model yoğun (503): sıradaki modele geçmek için iç kullanım.
        private sealed class GeminiCapacityException : Exception
        {
            public GeminiCapacityException(HttpStatusCode status) => Status = status;
            public HttpStatusCode Status { get; }
        }
    }
}
