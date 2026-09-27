namespace PromptForge.Api.Models
{
    /// <summary>
    /// Kullanıcı modeli. Uygulamaya kaydolan her kullanıcıyı temsil eder.
    /// Giriş bilgilerini, profil verilerini ve ilişkili verileri içerir.
    /// </summary>
    public class User
    {
        /// <summary>
        /// Kullanıcının benzersiz tanımlayıcısı (GUID). Veritabanında primary key olarak kullanılır.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Kullanıcının e-posta adresi. Giriş ve iletişim için kullanılır. Unique constraint var.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Şifreli parola hash'i. Asla düz metin olarak saklanmaz.
        /// Bcrypt veya PBKDF2 gibi güvenli algoritmalarla şifrelenir.
        /// </summary>
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>
        /// Kullanıcının görüntü adı. Profil ve arayüzde kullanıcıyı temsil eder.
        /// </summary>
        public string? DisplayName { get; set; }

        /// <summary>
        /// Kullanıcının profil resmi URL'si. Base64 veya CDN bağlantısı olabilir.
        /// </summary>
        public string? Avatar { get; set; }

        /// <summary>
        /// Kullanıcı hesabının oluşturulma tarihi. UTC formatında tutulur.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Kullanıcı profilinin son güncellenme tarihi. Profil değişikliklerini takip eder.
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Hesabın aktif durumu. İnaktif hesaplar giriş yapamaz.
        /// Kullanıcı silme yerine deaktive edilebilir (soft delete).
        /// </summary>
        public bool IsActive { get; set; }

        // ===== Navigation Properties (İlişkiler) =====

        /// <summary>
        /// Kullanıcının tüm prompt optimizasyonlarının geçmişi.
        /// Bir kullanıcının birçok prompt optimizasyonu olabilir (1:N ilişki).
        /// </summary>
        public ICollection<PromptOptimization> PromptOptimizations { get; set; } = new List<PromptOptimization>();

        /// <summary>
        /// Kullanıcının beğendiği ve favoriye eklediği promptlar.
        /// Bir kullanıcının birçok favorisi olabilir (1:N ilişki).
        /// </summary>
        public ICollection<FavoritePrompt> FavoritePrompts { get; set; } = new List<FavoritePrompt>();

        /// <summary>
        /// Kullanıcının özelleştirilmiş ayarları (tema, dil, varsayılan model vb).
        /// Bir kullanıcının sadece bir Settings nesnesi vardır (1:1 ilişki).
        /// </summary>
        public UserSettings? Settings { get; set; }
    }
}
