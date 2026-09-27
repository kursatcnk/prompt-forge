using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PromptForge.Api.Data;
using PromptForge.Api.Dtos;
using PromptForge.Api.Models;

namespace PromptForge.Api.Services
{
    // Geçmiş ve favoriler. Her sorguda UserId filtresi var; kimse başkasının kaydına dokunamaz.
    public class PromptLibraryService
    {
        private const int MaxHistory = 200;
        private static readonly HashSet<string> KnownModels = new() { "gpt", "claude", "gemini", "deepseek", "universal" };
        private static readonly HashSet<string> KnownGoals = new() { "quality", "balanced", "lean" };

        private readonly PromptForgeDbContext _context;

        public PromptLibraryService(PromptForgeDbContext context) => _context = context;

        // Kalite raporunun detayları ayrı tablolar yerine tek JSON kolonda (DetailsJson) duruyor, sorgulamıyorum zaten.
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
            // Bu kayıt tarayıcıdan geliyor; kolon sınırlarını aşan ya da bilinmeyen bir değer 500'e yol açmasın.
            var profile = record.Profile ?? new PromptProfileDto();
            var entity = new PromptOptimization
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                OriginalContent = record.Original,
                OptimizedContent = record.Optimized,
                TargetModel = KnownModels.Contains(record.Model) ? record.Model : "universal",
                OptimizationTarget = KnownGoals.Contains(record.Goal) ? record.Goal : "balanced",
                UseCase = Clip(profile.UseCase, 50),
                OutputFormat = Clip(profile.ResponseFormat, 50),
                ResponseLanguage = Clip(profile.ResponseLanguage, 20),
                ClarifyingQuestions = profile.AskClarifying,
                AssumptionVisibility = profile.ExposeAssumptions,
                HealthScoreOriginal = Math.Clamp(record.BeforeHealth, 0, 100),
                HealthScoreOptimized = Math.Clamp(record.AfterHealth, 0, 100),
                TokensOriginal = Math.Max(0, record.BeforeTokens),
                TokensOptimized = Math.Max(0, record.AfterTokens),
                Engine = Clip(record.Engine, 100) ?? "local",
                DetailsJson = JsonSerializer.Serialize(new PromptDetails(record.Requirements ?? new(), record.Issues ?? new(), record.Variables ?? new(), record.Checks ?? new())),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.PromptOptimizations.Add(entity);
            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        // Favori ilişkisi NoAction, önce favoriyi silmezsem FK hatası alıyorum.
        public async Task<bool> DeleteAsync(Guid userId, Guid promptId)
        {
            await _context.FavoritePrompts.Where(f => f.UserId == userId && f.PromptOptimizationId == promptId).ExecuteDeleteAsync();
            return await _context.PromptOptimizations.Where(p => p.UserId == userId && p.Id == promptId).ExecuteDeleteAsync() > 0;
        }

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

        // Kayıt kullanıcının değilse false. Zaten favorideyse bir şey yapmadan true.
        public async Task<bool> AddFavoriteAsync(Guid userId, Guid promptId)
        {
            var ownsPrompt = await _context.PromptOptimizations.AnyAsync(p => p.Id == promptId && p.UserId == userId);
            if (!ownsPrompt) return false;

            var exists = await _context.FavoritePrompts.AnyAsync(f => f.UserId == userId && f.PromptOptimizationId == promptId);
            if (exists) return true;

            _context.FavoritePrompts.Add(new FavoritePrompt
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                PromptOptimizationId = promptId,
                SavedAt = DateTime.UtcNow
            });
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Aynı anda iki tıklama: unique index ikinciyi reddediyor, sonuç yine "favoride".
            }
            return true;
        }

        public async Task RemoveFavoriteAsync(Guid userId, Guid promptId) =>
            await _context.FavoritePrompts.Where(f => f.UserId == userId && f.PromptOptimizationId == promptId).ExecuteDeleteAsync();

        private static string? Clip(string? value, int max) =>
            string.IsNullOrEmpty(value) ? value : value.Length <= max ? value : value[..max];

        private static PromptRecordDto ToDto(PromptOptimization p)
        {
            PromptDetails? details = null;
            if (!string.IsNullOrEmpty(p.DetailsJson))
            {
                try { details = JsonSerializer.Deserialize<PromptDetails>(p.DetailsJson); }
                catch (JsonException) { /* bozuk detay yüzünden geçmiş listesi patlamasın */ }
            }

            return new PromptRecordDto
            {
                Id = p.Id.ToString(),
                // DB'de UTC ama Kind'ı Unspecified geliyor; işaretlemezsem JSON'da sonuna Z koymuyor, tarayıcı yerel saat sanıyor.
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
