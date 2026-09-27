using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace PromptForge.Api.Services.Ai
{
    /// <summary>
    /// Google Gemini adaptörü ("generateContent" REST uç noktası).
    /// Gemini'de sistem talimatı ayrı bir alanda (systemInstruction) gönderilir.
    /// </summary>
    public class GeminiProvider : IAiProvider
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly AiProviderOptions _options;

        public GeminiProvider(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _options = configuration.GetSection("AI:Gemini").Get<AiProviderOptions>() ?? new();
        }

        public string Key => "gemini";
        public string DisplayName => "Gemini";
        public string Model => string.IsNullOrWhiteSpace(_options.Model) ? "gemini-3.8-flash" : _options.Model;
        public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.ApiKey);

        public async Task<AiCompletion> CompleteAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken)
        {
            var body = new JsonObject
            {
                ["systemInstruction"] = new JsonObject { ["parts"] = new JsonArray { new JsonObject { ["text"] = systemPrompt } } },
                ["contents"] = new JsonArray
                {
                    new JsonObject { ["role"] = "user", ["parts"] = new JsonArray { new JsonObject { ["text"] = userMessage } } }
                }
            };

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{Uri.EscapeDataString(Model)}:generateContent";
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(body.ToJsonString(), System.Text.Encoding.UTF8, "application/json")
            };
            // Anahtar URL'de değil başlıkta gönderilir; böylece sunucu loglarına düşmez.
            request.Headers.Add("x-goog-api-key", _options.ApiKey);

            HttpResponseMessage response;
            try
            {
                response = await _httpClientFactory.CreateClient("ai").SendAsync(request, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                throw new AiProviderException("Gemini sunucusuna bağlanılamadı.", ex);
            }

            using (response)
            {
                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                if (!response.IsSuccessStatusCode)
                    throw new AiProviderException(response.StatusCode switch
                    {
                        HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => "Gemini API anahtarı geçersiz.",
                        // Google eski modelleri zamanla kapatır; bu durumda appsettings.json → AI:Gemini:Model güncellenmeli.
                        HttpStatusCode.NotFound => $"Gemini modeli bulunamadı ({Model}). appsettings.json içindeki AI:Gemini:Model adını kontrol et.",
                        HttpStatusCode.TooManyRequests => "Gemini şu an çok yoğun veya kota doldu. Birazdan tekrar dene.",
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
                return new AiCompletion(text, Read("promptTokenCount"), Read("candidatesTokenCount"), Model);
            }
        }
    }
}
