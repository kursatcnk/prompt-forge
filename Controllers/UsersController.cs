using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PromptForge.Api.Dtos;
using PromptForge.Api.Services;

namespace PromptForge.Api.Controllers
{
    // Geliştirirken Postman'den kullanıcıları görmek için. Canlıda ve dışarıdan gelen isteklerde 404.
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
            // Site tünelle paylaşılırken dışarıdan gelenler gerçek IP'leriyle görünüyor (ForwardedHeaders),
            // loopback kontrolü sayesinde siteye kayıt olan biri herkesin mailini listeleyemiyor.
            if (!_environment.IsDevelopment() || !IPAddress.IsLoopback(HttpContext.Connection.RemoteIpAddress ?? IPAddress.None))
                return NotFound();
            return Ok(await _userService.GetAllUsersAsync());
        }
    }
}
