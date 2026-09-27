using System.Security.Claims;

namespace PromptForge.Api.Controllers
{
    public static class ClaimsPrincipalExtensions
    {
        /// <summary>
        /// Token'ın içindeki kullanıcı id'sini okur. [Authorize] olan endpoint'lerde her zaman doludur.
        /// Kullanıcı id'si istekten (URL/body) değil token'dan alınır; böylece kimse başkası adına işlem yapamaz.
        /// </summary>
        public static Guid GetUserId(this ClaimsPrincipal user) =>
            Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}
