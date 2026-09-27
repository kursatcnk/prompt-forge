using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PromptForge.Api.Dtos;
using PromptForge.Api.Services;

namespace PromptForge.Api.Controllers
{
    /// <summary>
    /// Favoriler.
    ///
    /// GET    /api/favorites              → Favori listesi (en son eklenen üstte)
    /// POST   /api/favorites/{promptId}   → Geçmişteki bir kaydı favoriye ekle
    /// DELETE /api/favorites/{promptId}   → Favoriden çıkar (geçmişten silinmez)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FavoritesController : ControllerBase
    {
        private readonly PromptLibraryService _library;

        public FavoritesController(PromptLibraryService library) => _library = library;

        [HttpGet]
        public async Task<ActionResult<List<PromptRecordDto>>> GetAll() =>
            Ok(await _library.GetFavoritesAsync(User.GetUserId()));

        [HttpPost("{promptId:guid}")]
        public async Task<IActionResult> Add(Guid promptId) =>
            await _library.AddFavoriteAsync(User.GetUserId(), promptId) ? NoContent() : NotFound(MessageResponse.Fail("Kayıt bulunamadı."));

        [HttpDelete("{promptId:guid}")]
        public async Task<IActionResult> Remove(Guid promptId)
        {
            await _library.RemoveFavoriteAsync(User.GetUserId(), promptId);
            return NoContent();
        }
    }
}
