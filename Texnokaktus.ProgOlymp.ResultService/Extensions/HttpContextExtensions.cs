using System.Security.Claims;

namespace Texnokaktus.ProgOlymp.ResultService.Extensions;

internal static class HttpContextExtensions
{
    public static int GetUserId(this HttpContext context) => context.User.GetUserId();

    public static int GetUserId(this ClaimsPrincipal user) =>
        int.Parse(user.Claims.First(claim => claim.Type == ClaimTypes.NameIdentifier).Value);
}
