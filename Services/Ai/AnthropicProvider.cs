using Anthropic;
using Anthropic.Exceptions;
using Anthropic.Models.Beta.Messages;

namespace PromptForge.Api.Services.Ai
{
    /// <summary>
    /// Anthropic Claude adaptörü. Resmi Anthropic C# SDK'sını kullanır.
    ///
    /// Model varsayılanı: claude-opus-5.
    /// Güvenlik sınıflandırıcısı bir isteği reddederse, sunucu tarafı "fallback" özelliği
    /// aynı isteği otomatik olarak uygun bir yedek modelle tekrar dener (beta özellik).
    /// </summary>
    public class AnthropicProvider : IAiProvider
    {
        private readonly AiProviderOptions _options;

        public AnthropicProvider(IConfiguration configuration)
        {
            _options = configuration.GetSection("AI:Anthropic").Get<AiProviderOptions>() ?? new();
        }

        public string Key => "anthropic";
        public string DisplayName => "Claude";
        public string Model => string.IsNullOrWhiteSpace(_options.Model) ? "claude-opus-5" : _options.Model;
        public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.ApiKey);

        // jsonOutput: Claude'da talimatla sağlanır (sistem promptu "sadece JSON döndür" der); çağıran taraf metinden JSON'u ayıklar.
        public async Task<AiCompletion> CompleteAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken, bool jsonOutput = false)
        {
            var client = new AnthropicClient { ApiKey = _options.ApiKey };

            try
            {
                BetaMessage response = await client.Beta.Messages.Create(new MessageCreateParams
                {
                    Model = Model,
                    MaxTokens = 16000,
                    System = systemPrompt,
                    // "default": reddedilirse yedek modeli Anthropic ret sebebine göre kendisi seçer.
                    Betas = ["server-side-fallback-2026-07-01"],
                    Fallbacks = new Default(),
                    Messages = [new() { Role = Role.User, Content = userMessage }],
                }, cancellationToken);

                // Reddedilen isteklerde içerik okunmadan önce durma sebebi kontrol edilmeli.
                if (response.StopReason == "refusal")
                    throw new AiProviderException("Claude bu promptu işlemeyi reddetti. Promptu düzenleyip tekrar deneyebilirsin.");

                var text = string.Join("\n", response.Content
                    .Select(block => block.Value)
                    .OfType<BetaTextBlock>()
                    .Select(block => block.Text)).Trim();

                if (text.Length == 0)
                    throw new AiProviderException("Claude boş bir cevap döndürdü.");

                return new AiCompletion(text, (int)response.Usage.InputTokens, (int)response.Usage.OutputTokens, response.Model.ToString());
            }
            // En özel hatadan en genele doğru yakalıyoruz: her birinin kullanıcıya söyleyeceği şey farklı.
            catch (AnthropicUnauthorizedException ex)
            {
                throw new AiProviderException("Claude API anahtarı geçersiz.", ex);
            }
            catch (AnthropicRateLimitException ex)
            {
                throw new AiProviderException("Claude şu an çok yoğun (rate limit). Birazdan tekrar dene.", ex);
            }
            catch (AnthropicApiException ex)
            {
                throw new AiProviderException("Claude isteği tamamlanamadı.", ex);
            }
            catch (AnthropicIOException ex)
            {
                throw new AiProviderException("Claude sunucusuna bağlanılamadı.", ex);
            }
        }
    }
}
