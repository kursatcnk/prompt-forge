namespace PromptForge.Api.Models
{
    /// <summary>
    /// Kullanıcının beğendiği ve kaydettiği promptlar.
    /// Kullanıcı ve PromptOptimization arasında bağlantı kurar (many-to-many ilişki aracı).
    /// Örn: Ahmet, GPT optimizasyonu #123'ü "SEO Prompts" ismiyle favoriye ekledi.
    /// </summary>
    public class FavoritePrompt
    {
        /// <summary>
        /// Favori kaydının benzersiz tanımlayıcısı.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Favoriye ekleyen kullanıcının ID'si.
        /// Foreign Key: Users(Id) tablosuna bağlıdır.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Favoriye eklenen prompt optimizasyonunun ID'si.
        /// Foreign Key: PromptOptimizations(Id) tablosuna bağlıdır.
        /// </summary>
        public Guid PromptOptimizationId { get; set; }

        /// <summary>
        /// Promptun favoriye eklenme tarihi (UTC).
        /// En yeni favoriler önde gösterilmek için kullanılabilir.
        /// </summary>
        public DateTime SavedAt { get; set; }

        /// <summary>
        /// Kullanıcının favoriye verdiği özel ad.
        /// Örn: "SEO Blog Prompts", "Code Review Templates", "Brand Voice"
        /// Favorileri kategoriyle/koleksiyonla düzenlemek için kullanılır.
        /// </summary>
        public string? CustomName { get; set; }

        // ===== Navigation Properties =====

        /// <summary>
        /// Favoriye ekleyen kullanıcı nesnesi.
        /// Foreign Key ilişkiyi sağlar (UserId -> User.Id).
        /// </summary>
        public User? User { get; set; }

        /// <summary>
        /// Favoriye eklenen prompt optimizasyonu nesnesi.
        /// Foreign Key ilişkiyi sağlar (PromptOptimizationId -> PromptOptimization.Id).
        /// </summary>
        public PromptOptimization? PromptOptimization { get; set; }
    }
}
