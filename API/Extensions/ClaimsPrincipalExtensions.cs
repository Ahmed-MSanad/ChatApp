using System.Security.Claims;

namespace API.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static string GetUserName(this ClaimsPrincipal user)
        => user.FindFirstValue(ClaimTypes.Name) ?? throw new Exception("Can't get username!");

    public static Guid GetUserId(this ClaimsPrincipal user)
        => Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new Exception("Can't get user Id!"));
}
