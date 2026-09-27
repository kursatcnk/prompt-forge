namespace PromptForge.Api.Models
{
    // Kullanıcı ile geçmişteki bir kayıt arasındaki bağ. (UserId, PromptOptimizationId) unique.
    public class FavoritePrompt
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid PromptOptimizationId { get; set; }
        public DateTime SavedAt { get; set; }

        // Favorilere isim verme için ayırdım, arayüzde henüz yok.
        public string? CustomName { get; set; }

        public User? User { get; set; }
        public PromptOptimization? PromptOptimization { get; set; }
    }
}
