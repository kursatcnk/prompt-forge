using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PromptForge.Api.Data;
using PromptForge.Api.Dtos;
using PromptForge.Api.Models;

namespace PromptForge.Api.Services
{
    /// <summary>
    /// Geçmiş (PromptOptimizations) ve favoriler (FavoritePrompts) işlemleri.
    /// Her sorgu mutlaka UserId ile filtrelenir: bir kullanıcı başkasının kaydını göremez veya silemez.
    /// </summary>
    public class PromptLibraryService
    {
        private const int MaxHistory = 200;
        private readonly PromptForgeDbContext _context;

        public PromptLibraryService(PromptForgeDbContext context) => _context = context;

        // DetailsJson sütununda saklanan analiz detayları.
        private record PromptDetails(List<string> Requirements, List<List<string>> Issues, List<string> Variables, Dictionary<string, bool> Checks);

        public async Task<List<PromptRecordDto>> GetHistoryAsync(Guid userId)
        {
            var items = await _context.PromptOptimizations
                .AsNoTracking()
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .Take(MaxHistory)
                .ToListAsync();
            return items.Select(ToDto).ToList();
        }

        public async Task<PromptRecordDto> SaveAsync(Guid userId, PromptRecordDto record)
        {
            var entity = new PromptOptimization
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                OriginalContent = record.Original,
                OptimizedContent = record.Optimized,
                TargetModel = record.Model,
                OptimizationTarget = record.Goal,
                UseCase = record.Profile.UseCase,
                OutputFormat = record.Profile.ResponseFormat,
                ResponseLanguage = record.Profile.ResponseLanguage,
                ClarifyingQuestions = record.Profile.AskClarifying,
                AssumptionVisibility = record.Profile.ExposeAssumptions,
                HealthScoreOriginal = record.BeforeHealth,
                HealthScoreOptimized = record.AfterHealth,
                TokensOriginal = record.BeforeTokens,
                TokensOptimized = record.AfterTokens,
                Engine = record.Engine ?? "local",
                DetailsJson = JsonSerializer.Serialize(new PromptDetails(record.Requirements, record.Issues, record.Variables, record.Checks)),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.PromptOptimizations.Add(entity);
            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        /// <summary>Tek kaydı siler. Favori bağlantısı otomatik silinmediği için (NoAction) önce o silinir.</summary>
        public async Task<bool> DeleteAsync(Guid userId, Guid promptId)
        {
            await _context.FavoritePrompts.Where(f => f.UserId == userId && f.PromptOptimizationId == promptId).ExecuteDeleteAsync();
            return await _context.PromptOptimizations.Where(p => p.UserId == userId && p.Id == promptId).ExecuteDeleteAsync() > 0;
        }

        /// <summary>Tüm geçmişi (ve dolayısıyla favorileri) temizler.</summary>
        public async Task ClearAsync(Guid userId)
        {
            await _context.FavoritePrompts.Where(f => f.UserId == userId).ExecuteDeleteAsync();
            await _context.PromptOptimizations.Where(p => p.UserId == userId).ExecuteDeleteAsync();
        }

        public async Task<List<PromptRecordDto>> GetFavoritesAsync(Guid userId)
        {
            var favorites = await _context.FavoritePrompts
                .AsNoTracking()
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.SavedAt)
                .Select(f => new { f.SavedAt, f.PromptOptimization })
                .ToListAsync();
            return favorites
                .Where(f => f.PromptOptimization != null)
                .Select(f => { var dto = ToDto(f.PromptOptimization!); dto.SavedAt = f.SavedAt; return dto; })
                .ToList();
        }

        /// <summary>Favoriye ekler. Kayıt kullanıcıya ait değilse false döner; zaten favorideyse sorun değil.</summary>
        public async Task<bool> AddFavoriteAsync(Guid userId, Guid promptId)
        {
            var ownsPrompt = await _context.PromptOptimizations.AnyAsync(p => p.Id == promptId && p.UserId == userId);
            if (!ownsPrompt) return false;

            var exists = await _context.FavoritePrompts.AnyAsync(f => f.UserId == userId && f.PromptOptimizationId == promptId);
            if (!exists)
            {
                _context.FavoritePrompts.Add(new FavoritePrompt
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    PromptOptimizationId = promptId,
                    SavedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }
            return true;
        }

        public async Task RemoveFavoriteAsync(Guid userId, Guid promptId) =>
            await _context.FavoritePrompts.Where(f => f.UserId == userId && f.PromptOptimizationId == promptId).ExecuteDeleteAsync();

        private static PromptRecordDto ToDto(PromptOptimization p)
        {
            PromptDetails? details = null;
            if (!string.IsNullOrEmpty(p.DetailsJson))
            {
                try { details = JsonSerializer.Deserialize<PromptDetails>(p.DetailsJson); }
                catch (JsonException) { /* bozuk detay kaydı ekranı engellemesin */ }
            }

            return new PromptRecordDto
            {
                Id = p.Id.ToString(),
                // Veritabanı UTC tutar; "Z" ekli ISO formatında dönmesi için türünü UTC işaretliyoruz.
                CreatedAt = DateTime.SpecifyKind(p.CreatedAt, DateTimeKind.Utc),
                Model = p.TargetModel ?? "universal",
                Goal = p.OptimizationTarget ?? "balanced",
                Original = p.OriginalContent ?? string.Empty,
                Optimized = p.OptimizedContent ?? string.Empty,
                BeforeTokens = p.TokensOriginal ?? 0,
                AfterTokens = p.TokensOptimized ?? 0,
                BeforeHealth = p.HealthScoreOriginal ?? 0,
                AfterHealth = p.HealthScoreOptimized ?? 0,
                Requirements = details?.Requirements ?? new(),
                Issues = details?.Issues ?? new(),
                Variables = details?.Variables ?? new(),
                Checks = details?.Checks ?? new(),
                Profile = new PromptProfileDto
                {
                    UseCase = p.UseCase,
                    ResponseFormat = p.OutputFormat,
                    ResponseLanguage = p.ResponseLanguage,
                    AskClarifying = p.ClarifyingQuestions,
                    ExposeAssumptions = p.AssumptionVisibility
                },
                Engine = p.Engine
            };
        }
    }
}
