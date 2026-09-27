using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using OtpNet;
using QRCoder;

namespace PromptForge.Api.Services
{
    // Authenticator uygulamasıyla 2FA (TOTP). Telefon ve sunucu aynı secret'la her 30 sn'de aynı 6 haneli kodu üretiyor.
    public class TwoFactorService
    {
        private const string Issuer = "PromptForge";
        private readonly IDataProtector _protector;
        private readonly ILogger<TwoFactorService> _logger;

        public TwoFactorService(IDataProtectionProvider dataProtectionProvider, ILogger<TwoFactorService> logger)
        {
            // Secret DB'ye şifreli yazılıyor; DB sızsa bile kod üretilemesin.
            _protector = dataProtectionProvider.CreateProtector("PromptForge.TwoFactorSecret");
            _logger = logger;
        }

        public string GenerateSecret() => Base32Encoding.ToString(KeyGeneration.GenerateRandomKey(20));

        public string Protect(string secret) => _protector.Protect(secret);

        public string BuildOtpAuthUri(string email, string secret) =>
            $"otpauth://totp/{Uri.EscapeDataString($"{Issuer}:{email}")}?secret={secret}&issuer={Uri.EscapeDataString(Issuer)}&digits=6&period=30";

        // PNG'yi data URL olarak dönüyorum, arayüz direkt <img src> ile basıyor.
        public string BuildQrDataUrl(string otpAuthUri)
        {
            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(otpAuthUri, QRCodeGenerator.ECCLevel.M);
            var png = new PngByteQRCode(data).GetGraphic(6);
            return $"data:image/png;base64,{Convert.ToBase64String(png)}";
        }

        // DB'deki şifreli secret'la kodu doğrular.
        // Data Protection anahtarları kaybolursa secret çözülemiyor; o durumda 500 yerine "kod hatalı" dönsün.
        public bool VerifyProtected(string protectedSecret, string? code)
        {
            string secret;
            try
            {
                secret = _protector.Unprotect(protectedSecret);
            }
            catch (CryptographicException ex)
            {
                _logger.LogError(ex, "2FA secret çözülemedi; Data Protection anahtarları değişmiş olabilir.");
                return false;
            }

            var clean = new string((code ?? string.Empty).Where(char.IsDigit).ToArray());
            if (clean.Length != 6) return false;

            // Telefon saati birkaç saniye kaymış olabilir, bir önceki ve sonraki 30 sn'lik dilimi de kabul ediyorum.
            var totp = new Totp(Base32Encoding.ToBytes(secret));
            return totp.VerifyTotp(clean, out _, new VerificationWindow(previous: 1, future: 1));
        }
    }
}
