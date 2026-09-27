using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PromptForge.Api.Dtos;
using PromptForge.Api.Services;
using PromptForge.Api.Services.Ai;

namespace PromptForge.Api.Controllers
{
    // Optimize, analiz ve geçmiş.
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PromptsController : ControllerBase
    {
        // Normal kullanımın çok üstünde; hem maliyet hem kötüye kullanım için bir tavan.
        private const int MaxPromptLength = 20_000;

        private readonly PromptOptimizerService _optimizer;
        private readonly PromptLibraryService _library;
        private readonly UsageService _usage;
        private readonly AccountService _account;

        public PromptsController(PromptOptimizerService optimizer, PromptLibraryService library, UsageService usage, AccountService account)
        {
            _optimizer = optimizer;
            _library = library;
            _usage = usage;
            _account = account;
        }

        [HttpPost("optimize")]
        [EnableRateLimiting("ai")]
        public async Task<ActionResult<OptimizeResponse>> Optimize([FromBody] OptimizeRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Original))
                return BadRequest(MessageResponse.Fail("Prompt boş olamaz."));
            if (request.Original.Length > MaxPromptLength)
                return BadRequest(MessageResponse.Fail($"Prompt en fazla {MaxPromptLength:N0} karakter olabilir."));

            var userId = User.GetUserId();
            var me = await _account.GetMeAsync(userId);
            if (me == null) return Unauthorized();
            if (QuotaExceeded(me.Usage) is { } quotaError) return quotaError;

            // Hakkı iş başlamadan ayırıyorum; aynı anda gelen iki istek son hakkı ikisi birden kullanamasın.
            var reservation = await _usage.TryReserveAsync(userId, me.Usage.Plan);
            if (reservation == null) return QuotaResponse(await _usage.GetUsageAsync(userId, me.Usage.Plan));

            var (optimized, engine, usedAi, notice, tokens) = await _optimizer.OptimizeAsync(request, cancellationToken);
            if (string.IsNullOrWhiteSpace(optimized))
            {
                await _usage.ReleaseAsync(reservation.Value);
                return StatusCode(StatusCodes.Status502BadGateway, MessageResponse.Fail(notice ?? "Optimizasyon sonucu üretilemedi."));
            }

            await _usage.CompleteAsync(reservation.Value, usedAi ? engine : "local", tokens);

            return Ok(new OptimizeResponse
            {
                Optimized = optimized,
                Engine = engine,
                UsedAi = usedAi,
                Notice = notice,
                Usage = await _usage.GetUsageAsync(userId, me.Usage.Plan)
            });
        }

        // AI yoksa 503 dönüyor, arayüz o zaman tarayıcıdaki yerel analizi gösteriyor.
        [HttpPost("analyze")]
        [EnableRateLimiting("ai")]
        public async Task<ActionResult<AnalysisResult>> Analyze([FromBody] AnalyzeRequest request, [FromServices] PromptAnalysisService analysis, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Prompt))
                return BadRequest(MessageResponse.Fail("Analiz için bir prompt yaz."));
            if (request.Prompt.Length > MaxPromptLength)
                return BadRequest(MessageResponse.Fail($"Prompt en fazla {MaxPromptLength:N0} karakter olabilir."));

            var userId = User.GetUserId();
            var me = await _account.GetMeAsync(userId);
            if (me == null) return Unauthorized();
            if (QuotaExceeded(me.Usage) is { } quotaError) return quotaError;

            var reservation = await _usage.TryReserveAsync(userId, me.Usage.Plan);
            if (reservation == null) return QuotaResponse(await _usage.GetUsageAsync(userId, me.Usage.Plan));

            try
            {
                var result = await analysis.AnalyzeAsync(request.Prompt, cancellationToken);
                await _usage.CompleteAsync(reservation.Value, result.Engine ?? "ai", null);
                result.Usage = await _usage.GetUsageAsync(userId, me.Usage.Plan);
                return Ok(result);
            }
            catch (AiProviderException ex)
            {
                // AI cevap vermediyse hak iade; kullanıcı yerel analizi görüyor, kotası boşa gitmiyor.
                await _usage.ReleaseAsync(reservation.Value);
                return StatusCode(StatusCodes.Status503ServiceUnavailable, MessageResponse.Fail(ex.Message));
            }
        }

        // Optimize ve analiz aynı aylık hakkı paylaşıyor; dolunca ikisi de ay sonuna kadar duruyor.
        private ObjectResult? QuotaExceeded(UsageDto usage) => usage.Used >= usage.Limit ? QuotaResponse(usage) : null;

        private ObjectResult QuotaResponse(UsageDto usage) =>
            StatusCode(StatusCodes.Status429TooManyRequests, new QuotaExceededResponse
            {
                Message = $"Bu ayki {usage.Limit} hakkının tamamını kullandın. Kotan {usage.ResetsAt.ToLocalTime():dd.MM.yyyy HH:mm} tarihinde yenilenecek.",
                Usage = usage
            });

        [HttpPost]
        public async Task<ActionResult<PromptRecordDto>> Save([FromBody] PromptRecordDto record)
        {
            if (string.IsNullOrWhiteSpace(record.Original) || string.IsNullOrWhiteSpace(record.Optimized))
                return BadRequest(MessageResponse.Fail("Kayıt eksik."));
            if (record.Original.Length > MaxPromptLength || record.Optimized.Length > MaxPromptLength * 3)
                return BadRequest(MessageResponse.Fail("Kayıt çok uzun."));

            return Ok(await _library.SaveAsync(User.GetUserId(), record));
        }

        [HttpGet]
        public async Task<ActionResult<List<PromptRecordDto>>> GetHistory() =>
            Ok(await _library.GetHistoryAsync(User.GetUserId()));

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id) =>
            await _library.DeleteAsync(User.GetUserId(), id) ? NoContent() : NotFound();

        [HttpDelete]
        public async Task<IActionResult> Clear()
        {
            await _library.ClearAsync(User.GetUserId());
            return NoContent();
        }
    }
}
