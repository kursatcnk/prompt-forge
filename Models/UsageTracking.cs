namespace PromptForge.Api.Models
{
    // Her optimizasyon/analiz bir satır. Aylık kota bu ayki satır sayısı.
    public class UsageTracking
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }

        // İstek başlarken "pending" yazılıyor (hak ayrıldı), bitince modelin adı ya da "local" oluyor.
        // Yarım kalan pending satırları CleanupService siliyor.
        public string? Provider { get; set; }

        public int? TokensUsed { get; set; }
        public int ApiCallCount { get; set; } = 1;

        // Maliyet hesabı için yer ayırdım; fiyatlandırma gelince doldurulacak.
        public decimal? Cost { get; set; }

        public DateTime Date { get; set; }

        public User? User { get; set; }
    }
}
