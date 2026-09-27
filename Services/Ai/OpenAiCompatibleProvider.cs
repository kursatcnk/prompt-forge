using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace PromptForge.Api.Services.Ai
{
    /// <summary>
    /// "Chat Completions" formatını kullanan sağlayıcıların ortak kodu.
    /// OpenAI ve DeepSeek aynı istek/cevap yapısını kullandığı için sadece adres ve model farklıdır.
    /// </summary>
    public abstract class OpenAiCompatibleProvider : IAiProvider
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly AiProviderOptions _options;

        protected OpenAiCompatibleProvider(IHttpClientFactory httpClientFactory, IConfiguration configuration, string sectionName)
        {
            _httpClientFactory = httpClientFactory;
            _options = configuration.GetSection(sectionName).Get<AiProviderOptions>() ?? new();
        }

        public abstract string Key { get; }
        public abstract string DisplayName { get; }
        protected abstract string Endpoint { get; }
        protected abstract string DefaultModel { get; }

        public string Model => string.IsNullOrWhiteSpace(_options.Model) ? DefaultModel : _options.Model;
        public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.ApiKey);

        public async Task<AiCompletion> CompleteAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken)
        {
            var body = new JsonObject
            {
                ["model"] = Model,
                ["messages"] = new JsonArray
                {
                    new JsonObject { ["role"] = "system", ["content"] = systemPrompt },
                    new JsonObject { ["role"] = "user", ["content"] = userMessage }
                }
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint)
            {
                Content = new StringContent(body.ToJsonString(), System.Text.Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

            HttpResponseMessage response;
            try
            {
                response = await _httpClientFactory.CreateClient("ai").SendAsync(request, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                throw new AiProviderException($"{DisplayName} sunucusuna bağlanılamadı.", ex);
            }

            using (response)
            {
                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                if (!response.IsSuccessStatusCode)
                    throw new AiProviderException(response.StatusCode switch
                    {
                        HttpStatusCode.Unauthorized => $"{DisplayName} API anahtarı geçersiz.",
                        HttpStatusCode.NotFound => $"{DisplayName} modeli bulunamadı ({Model}). appsettings.json içindeki model adını kontrol et.",
                        HttpStatusCode.TooManyRequests => $"{DisplayName} şu an çok yoğun veya kota doldu. Birazdan tekrar dene.",
                        _ => $"{DisplayName} isteği tamamlanamadı ({(int)response.StatusCode})."
                    });

                // Cevap: { choices: [ { message: { content } } ], usage: { prompt_tokens, completion_tokens } }
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                var text = root.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString()?.Trim();
                if (string.IsNullOrEmpty(text))
                    throw new AiProviderException($"{DisplayName} boş bir cevap döndürdü.");

                var usage = root.TryGetProperty("usage", out var u) ? u : default;
                int Read(string name) => usage.ValueKind == JsonValueKind.Object && usage.TryGetProperty(name, out var v) ? v.GetInt32() : 0;
                return new AiCompletion(text, Read("prompt_tokens"), Read("completion_tokens"), Model);
            }
        }
    }

    /// <summary>OpenAI GPT adaptörü. Model appsettings'ten değiştirilebilir.</summary>
    public class OpenAiProvider : OpenAiCompatibleProvider
    {
        public OpenAiProvider(IHttpClientFactory f, IConfiguration c) : base(f, c, "AI:OpenAI") { }
        public override string Key => "openai";
        public override string DisplayName => "GPT";
        protected override string Endpoint => "https://api.openai.com/v1/chat/completions";
        protected override string DefaultModel => "gpt-4o";
    }

    /// <summary>DeepSeek adaptörü (OpenAI ile aynı format).</summary>
    public class DeepSeekProvider : OpenAiCompatibleProvider
    {
        public DeepSeekProvider(IHttpClientFactory f, IConfiguration c) : base(f, c, "AI:DeepSeek") { }
        public override string Key => "deepseek";
        public override string DisplayName => "DeepSeek";
        protected override string Endpoint => "https://api.deepseek.com/chat/completions";
        protected override string DefaultModel => "deepseek-chat";
    }
}
