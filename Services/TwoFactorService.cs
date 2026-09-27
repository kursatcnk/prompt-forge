using Microsoft.AspNetCore.DataProtection;
using OtpNet;
using QRCoder;

namespace PromptForge.Api.Services
{
    /// <summary>
    /// Authenticator uygulamasıyla (Google Authenticator, Microsoft Authenticator...) 2 adımlı doğrulama.
    ///
    /// NASIL ÇALIŞIR (TOTP):
    /// - Sunucu ve telefon aynı gizli anahtarı paylaşır (QR kodu okutulunca).
    /// - İkisi de "anahtar + şu anki 30 saniyelik zaman dilimi" ile aynı 6 haneli kodu üretir.
    /// - İnternet veya SMS gerekmez; kodlar zamanla değiştiği için çalınan kod kısa sürede işe yaramaz hale gelir.
    /// </summary>
    public class TwoFactorService
    {
        private const string Issuer = "PromptForge";
        private readonly IDataProtector _protector;

        public TwoFactorService(IDataProtectionProvider dataProtectionProvider)
        {
            // Gizli anahtar veritabanına şifrelenerek yazılır; veritabanı sızsa bile kod üretilemez.
            _protector = dataProtectionProvider.CreateProtector("PromptForge.TwoFactorSecret");
        }

        public string GenerateSecret() => Base32Encoding.ToString(KeyGeneration.GenerateRandomKey(20));

        public string Protect(string secret) => _protector.Protect(secret);

        public string Unprotect(string protectedSecret) => _protector.Unprotect(protectedSecret);

        /// <summary>Authenticator uygulamasının okuyacağı adres (otpauth://...).</summary>
        public string BuildOtpAuthUri(string email, string secret) =>
            $"otpauth://totp/{Uri.EscapeDataString($"{Issuer}:{email}")}?secret={secret}&issuer={Uri.EscapeDataString(Issuer)}&digits=6&period=30";

        /// <summary>otpauth adresinin QR kodunu PNG olarak üretir; tarayıcı doğrudan &lt;img src&gt; ile gösterir.</summary>
        public string BuildQrDataUrl(string otpAuthUri)
        {
            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(otpAuthUri, QRCodeGenerator.ECCLevel.M);
            var png = new PngByteQRCode(data).GetGraphic(6);
            return $"data:image/png;base64,{Convert.ToBase64String(png)}";
        }

        /// <summary>
        /// Kodu doğrular. Telefon saati birkaç saniye kaymış olabileceği için bir önceki ve sonraki 30 saniyelik dilim de kabul edilir.
        /// </summary>
        public bool VerifyCode(string secret, string? code)
        {
            var clean = new string((code ?? string.Empty).Where(char.IsDigit).ToArray());
            if (clean.Length != 6) return false;
            var totp = new Totp(Base32Encoding.ToBytes(secret));
            return totp.VerifyTotp(clean, out _, new VerificationWindow(previous: 1, future: 1));
        }
    }
}
