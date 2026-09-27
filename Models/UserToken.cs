namespace PromptForge.Api.Models
{
    /// <summary>
    /// Tek kullanımlık, süreli kodlar ve bağlantılar.
    ///
    /// Kullanım yerleri (Purpose):
    /// - "email-verify":  Kayıttan sonra e-postaya giden 6 haneli doğrulama kodu
    /// - "password-reset": "Şifremi unuttum" bağlantısındaki gizli anahtar
    /// - "two-factor":    Şifre doğru girildikten sonra 2 adımlı kod ekranına geçiş bileti
    ///
    /// GÜVENLİK: Kodun kendisi değil, SHA-256 özeti (hash) saklanır.
    /// Veritabanı ele geçirilse bile geçerli bir kod elde edilemez.
    /// </summary>
    public class UserToken
    {
        public Guid Id { get; set; }

        /// <summary>Kodun sahibi olan kullanıcı.</summary>
        public Guid UserId { get; set; }

        /// <summary>Kodun amacı: email-verify, password-reset, two-factor.</summary>
        public string Purpose { get; set; } = string.Empty;

        /// <summary>Kodun SHA-256 özeti.</summary>
        public string TokenHash { get; set; } = string.Empty;

        /// <summary>Bu tarihten sonra kod geçersizdir (UTC).</summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>Kod kullanıldıysa kullanım zamanı; tekrar kullanılamaz.</summary>
        public DateTime? UsedAt { get; set; }

        /// <summary>Yanlış deneme sayısı. Çok fazla denenirse kod iptal edilir (tahmin saldırısına karşı).</summary>
        public int FailedAttempts { get; set; }

        public DateTime CreatedAt { get; set; }

        public User? User { get; set; }
    }
}
