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

        /// <summary>
        /// İşe başlamadan önce kotadan bir hak ayırır. Kota doluysa null döner.
        ///
        /// YARIŞ DURUMU: Kotanın son hakkında aynı anda gelen iki istek, ikisi de "yer var" görüp geçebilir.
        /// Bunu önlemek için SQL Server'ın uygulama kilidi (sp_getapplock) ile aynı kullanıcının istekleri
        /// sıraya sokulur: kilit al → say → yer varsa hakkı ekle → kilidi bırak. Farklı kullanıcılar birbirini beklemez.
        /// </summary>
        public async Task<Guid?> TryReserveAsync(Guid userId, string plan)
        {
            var limit = GetPlan(plan).MonthlyLimit;
            var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            // Bağlantı hatasında tekrar deneme (EnableRetryOnFailure) açık olduğu için transaction bu strateji içinde çalışmalı.
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                var lockName = $"quota:{userId}";
                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $"EXEC sp_getapplock @Resource = {lockName}, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 10000");

                var used = await _context.UsageTracking.CountAsync(t => t.UserId == userId && t.Date >= monthStart);
                if (used >= limit)
                {
                    await transaction.RollbackAsync();
                    return (Guid?)null;
                }

                var reservation = new UsageTracking
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Provider = "pending",
                    ApiCallCount = 1,
                    Date = DateTime.UtcNow
                };
                _context.UsageTracking.Add(reservation);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync(); // Kilit transaction'la birlikte serbest kalır.
                return (Guid?)reservation.Id;
            });
        }

        /// <summary>Ayrılan hakkı, işi kimin yaptığı ve harcanan token bilgisiyle tamamlar.</summary>
        public async Task CompleteAsync(Guid reservationId, string provider, int? tokens)
        {
            var row = await _context.UsageTracking.FindAsync(reservationId);
            if (row == null) return;
            row.Provider = provider;
            row.TokensUsed = tokens;
            await _context.SaveChangesAsync();
        }

        /// <summary>İş yapılamadıysa (örn. AI analizi başarısız) ayrılan hakkı iade eder.</summary>
        public async Task ReleaseAsync(Guid reservationId) =>
            await _context.UsageTracking.Where(t => t.Id == reservationId).ExecuteDeleteAsync();
    }

    public record PlanDefinition(string Key, string Name, int MonthlyLimit, string Price, string[] Features);
}
