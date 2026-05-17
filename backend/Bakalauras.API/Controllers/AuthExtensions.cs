using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

public static class AuthExtensions
{
    public static int GetUserId(this ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub)
                  ?? user.FindFirstValue("sub");

        return int.TryParse(raw, out var id) ? id : 0;
    }

    public static int GetCompanyId(this ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue("companyId");
        return int.TryParse(raw, out var id) ? id : 0;
    }

    public static string GetCompanyRole(this ClaimsPrincipal user)
        => user.FindFirstValue("companyRole") ?? "";

    public static bool IsMasterAdmin(this ClaimsPrincipal user)
        => (user.FindFirstValue("isMasterAdmin") ?? "0") == "1";
}