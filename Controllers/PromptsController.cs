using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PromptForge.Api.Dtos;
using PromptForge.Api.Services;
using PromptForge.Api.Services.Ai;

namespace PromptForge.Api.Controllers
{
    /// <summary>
    /// Prompt optimizasyonu ve geçmiş.
    ///
    /// POST   /api/prompts/optimize   → Promptu optimize et (AI veya yerel motor), kotaya işle
    /// POST   /api/prompts            → Sonucu geçmişe kaydet
    /// GET    /api/prompts            → Geçmişi getir (en yeni 200 kayıt)
    /// DELETE /api/prompts/{id}       → Tek kaydı sil
    /// DELETE /api/prompts            → Tüm geçmişi temizle
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PromptsController : ControllerBase
    {
        // Çok uzun metinler hem maliyeti hem kötüye kullanımı artırır; bu sınır normal kullanımın çok üstünde.
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
        public async Task<ActionResult<OptimizeResponse>> Optimize([FromBody] OptimizeRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Original))
                return BadRequest(MessageResponse.Fail("Prompt boş olamaz."));
            if (request.Original.Length > MaxPromptLength)
                return BadRequest(MessageResponse.Fail($"Prompt en fazla {MaxPromptLength:N0} karakter olabilir."));

            var userId = User.GetUserId();
            var me = await _account.GetMeAsync(userId);
            if (me == null) return Unauthorized();

            // Kota kontrolü: plan limitine ulaşıldıysa 429 (Too Many Requests) dön.
            if (me.Usage.Used >= me.Usage.Limit)
                return StatusCode(StatusCodes.Status429TooManyRequests,
                    MessageResponse.Fail($"Bu ayki {me.Usage.Limit} optimizasyon hakkını kullandın. Kota {me.Usage.ResetsAt:dd.MM.yyyy} tarihinde yenilenir."));

            var (optimized, engine, usedAi, notice, tokens) = await _optimizer.OptimizeAsync(request, cancellationToken);
            if (string.IsNullOrWhiteSpace(optimized))
                return StatusCode(StatusCodes.Status502BadGateway, MessageResponse.Fail(notice ?? "Optimizasyon sonucu üretilemedi."));

            await _usage.RecordAsync(userId, usedAi ? engine : "local", tokens);

            return Ok(new OptimizeResponse
            {
                Optimized = optimized,
                Engine = engine,
                UsedAi = usedAi,
                Notice = notice,
                Usage = await _usage.GetUsageAsync(userId, me.Usage.Plan)
            });
        }

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
