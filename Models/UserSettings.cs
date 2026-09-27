namespace PromptForge.Api.Models
{
    /// <summary>
    /// Kullanıcının uygulama tercihleri ve özelleştirmelerini içerir.
    /// Her kullanıcının tam olarak bir Settings kaydı vardır (1:1 ilişki).
    /// </summary>
    public class UserSettings
    {
        /// <summary>
        /// Ayar kaydının benzersiz tanımlayıcısı.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Bu ayarların sahibi olan kullanıcının ID'si.
        /// Foreign Key: Users(Id) tablosuna bağlıdır. Unique constraint var (1:1).
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Kullanıcının varsayılan tercih ettiği AI modeli.
        /// Değerler: "gpt", "claude", "gemini", "deepseek", "universal"
        /// Yeni optimizasyon yapıldığında bu model otomatik seçilir.
        /// Varsayılan: "gpt"
        /// </summary>
        public string? DefaultModel { get; set; } = "gpt";

        /// <summary>
        /// Kullanıcının varsayılan optimizasyon hedefi.
        /// Değerler: "best", "balanced", "minimal"
        /// best: Maksimum kalite (daha uzun)
        /// balanced: Denge (varsayılan)
        /// minimal: En kısa format
        /// Varsayılan: "balanced"
        /// </summary>
        public string? DefaultOptimizationTarget { get; set; } = "balanced";

        /// <summary>
        /// Arayüz teması.
        /// Değerler: "light", "dark", "system"
        /// system: İşletim sisteminin ayarına bağlı olarak oto-değişir.
        /// Varsayılan: "light"
        /// </summary>
        public string? Theme { get; set; } = "light";

        /// <summary>
        /// Arayüz yoğunluğu/boşluk.
        /// Değerler: "comfortable", "tight"
        /// comfortable: Daha geniş boşluklar, rahat okuma
        /// tight: Kompakt görünüş, daha fazla içerik
        /// Varsayılan: "comfortable"
        /// </summary>
        public string? Density { get; set; } = "comfortable";

        /// <summary>
        /// Animasyon ve geçişlerin etkinliği.
        /// True: Pürüzsüz animasyonlar etkin.
        /// False: Minimal hareketler (erişilebilirlik/performans).
        /// Varsayılan: true
        /// </summary>
        public bool MotionEnabled { get; set; } = true;

        /// <summary>
        /// Arayüz dili.
        /// Değerler: "tr" (Türkçe), "en" (English), vb.
        /// Backend'in cevapları ve error mesajları bu dilde iletilir.
        /// Varsayılan: "tr"
        /// </summary>
        public string? Language { get; set; } = "tr";

        // ===== Navigation Property =====

        /// <summary>
        /// Bu ayarların sahibi olan kullanıcı nesnesi.
        /// Foreign Key ilişkiyi sağlar (UserId -> User.Id).
        /// </summary>
        public User? User { get; set; }
    }
}
