using Microsoft.EntityFrameworkCore;
using PromptForge.Api.Data;
using PromptForge.Api.Dtos;
using PromptForge.Api.Models;

namespace PromptForge.Api.Services
{
    /// <summary>
    /// Plan ve aylık kota yönetimi.
    /// Her optimizasyon UsageTracking tablosuna bir satır yazar; kota bu ayki satır sayısıdır.
    /// </summary>
    public class UsageService
    {
        /// <summary>Plan tanımları. Ödeme sistemi eklendiğinde fiyatlar da buradan yönetilebilir.</summary>
        public static readonly IReadOnlyList<PlanDefinition> Plans = new[]
        {
            new PlanDefinition("free", "Ücretsiz", 50, "0 ₺", new[] { "Aylık 50 optimizasyon", "Tüm hedef modeller", "Geçmiş ve favoriler", "Prompt analizi" }),
            new PlanDefinition("pro", "Pro", 1000, "Yakında", new[] { "Aylık 1000 optimizasyon", "Öncelikli AI motoru", "Sınırsız geçmiş", "Ekip paylaşımı (yakında)" })
        };

        private readonly PromptForgeDbContext _context;

        public UsageService(PromptForgeDbContext context) => _context = context;

        public static PlanDefinition GetPlan(string? plan) => Plans.FirstOrDefault(p => p.Key == plan) ?? Plans[0];

        public async Task<UsageDto> GetUsageAsync(Guid userId, string plan)
        {
            var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var used = await _context.UsageTracking.CountAsync(t => t.UserId == userId && t.Date >= monthStart);
            return new UsageDto
            {
                Plan = GetPlan(plan).Key,
                Used = used,
                Limit = GetPlan(plan).MonthlyLimit,
                ResetsAt = monthStart.AddMonths(1)
            };
        }

        /// <summary>Bir optimizasyonu kotaya işler. Token bilgisi sadece AI kullanıldıysa dolu olur.</summary>
        public async Task RecordAsync(Guid userId, string provider, int? tokens)
        {
            _context.UsageTracking.Add(new UsageTracking
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Provider = provider,
                TokensUsed = tokens,
                ApiCallCount = 1,
                Date = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }
    }

    public record PlanDefinition(string Key, string Name, int MonthlyLimit, string Price, string[] Features);
}
