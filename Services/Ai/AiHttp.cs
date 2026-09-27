using System.Net;

namespace PromptForge.Api.Services.Ai
{
    /// <summary>
    /// AI sağlayıcılarına HTTP isteği gönderir; geçici hatalarda kısa bekleyip tekrar dener.
    ///
    /// Geçici hata: 500, 502, 503 (sunucu yoğun), 504 ve bağlantı kopmaları.
    /// Bunlar genelde birkaç saniye içinde düzelir; ilk hatada vazgeçmek kullanıcıyı gereksiz yere yerel motora düşürür.
    /// 429 (kota doldu) burada tekrar denenmez: dolmuş kotayı hemen tekrar zorlamak sadece yeni bir 429 üretir.
    /// Kalıcı hatalar (401 geçersiz anahtar, 404 model yok) da tekrar denenmez, hemen döner.
    /// </summary>
    public static class AiHttp
    {
        // 1. tekrar 1 sn, 2. tekrar 3 sn sonra. Toplam en fazla ~4 sn ek bekleme.
        private static readonly TimeSpan[] RetryDelays = { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3) };

        private static bool IsTransient(HttpStatusCode status) =>
            status is HttpStatusCode.InternalServerError or HttpStatusCode.BadGateway
                or HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout;

        /// <summary>
        /// Aynı HttpRequestMessage iki kez gönderilemediği için her denemede createRequest ile yenisi oluşturulur.
        /// </summary>
        public static async Task<HttpResponseMessage> SendWithRetryAsync(HttpClient client, Func<HttpRequestMessage> createRequest, CancellationToken cancellationToken)
        {
            for (var attempt = 0; ; attempt++)
            {
                var lastAttempt = attempt == RetryDelays.Length;
                try
                {
                    using var request = createRequest();
                    var response = await client.SendAsync(request, cancellationToken);
                    if (lastAttempt || !IsTransient(response.StatusCode)) return response;
                    response.Dispose();
                }
                catch (HttpRequestException) when (!lastAttempt)
                {
                    // Bağlantı koptu: aşağıda bekleyip tekrar dene.
                }
                await Task.Delay(RetryDelays[attempt], cancellationToken);
            }
        }
    }
}
