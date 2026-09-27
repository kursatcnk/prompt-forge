using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PromptForge.Api.Dtos;
using PromptForge.Api.Services;

namespace PromptForge.Api.Controllers
{
    /// <summary>
    /// GELİŞTİRİCİ ARACI: Tüm kullanıcıları listeler.
    ///
    /// GET /api/users → Tüm kullanıcılar (en yeni kayıt en üstte)
    ///
    /// Güvenlik: [Authorize] ile sadece giriş yapmış kişiler erişebilir ve sadece Development ortamında çalışır.
    /// Canlıda herkesin tüm e-postaları görmesi veri sızıntısı olurdu; orada 404 döner.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IWebHostEnvironment _environment;

        public UsersController(IUserService userService, IWebHostEnvironment environment)
        {
            _userService = userService;
            _environment = environment;
        }

        [HttpGet]
        public async Task<ActionResult<List<UserInfo>>> GetAll()
        {
            if (!_environment.IsDevelopment()) return NotFound();
            return Ok(await _userService.GetAllUsersAsync());
        }
    }
}
