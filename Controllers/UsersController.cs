using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PromptForge.Api.Dtos;
using PromptForge.Api.Services;

namespace PromptForge.Api.Controllers
{
    /// <summary>
    /// Kullanıcı işlemleri controller'ı.
    ///
    /// ENDPOINT'LER:
    /// GET /api/users → Tüm kullanıcıları listeler
    ///
    /// [Authorize]: Bu controller'daki her endpoint giriş yapmış olmayı gerektirir.
    /// İstek "Authorization: Bearer {token}" başlığı olmadan gelirse 401 Unauthorized döner.
    /// Token'ın kontrolünü biz yazmıyoruz; Program.cs'deki AddJwtBearer ayarı otomatik yapıyor.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// Tüm kullanıcıları listeler (en yeni kayıt en üstte).
        ///
        /// ÖRNEK ISTEK:
        /// GET /api/users
        /// Authorization: Bearer eyJhbGciOi...
        ///
        /// BAŞARILI YANIT (200):
        /// [
        ///   { "id": "...", "email": "ahmet@example.com", "displayName": "Ahmet", "avatar": null }
        /// ]
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<UserInfo>>> GetAll()
        {
            var users = await _userService.GetAllUsersAsync();
            return Ok(users);
        }
    }
}
