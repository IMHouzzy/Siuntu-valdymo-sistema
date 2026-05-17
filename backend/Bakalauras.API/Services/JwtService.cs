using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Bakalauras.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

public class JwtService
{
    private readonly IConfiguration _config;
    private readonly AppDbContext _db;

    public JwtService(IConfiguration config, AppDbContext db)
    {
        _config = config;
        _db = db;
    }

    public async Task<string> GenerateTokenAsync(user user)
    {
        if (user == null) throw new ArgumentNullException(nameof(user));

        var jwtKey = _config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key missing");
        var issuer = _config["Jwt:Issuer"];
        var audience = _config["Jwt:Audience"];

        var companies = await _db.company_users
            .AsNoTracking()
            .Where(cu => cu.fk_Usersid_Users == user.id_Users)
            .Select(cu => new
            {
                id_Company = cu.fk_Companyid_Company,
                name = cu.fk_Companyid_CompanyNavigation.name,
                code = cu.fk_Companyid_CompanyNavigation.companyCode,
                role = cu.role,
                image = cu.fk_Companyid_CompanyNavigation.image
            })
            .ToListAsync();

        var desiredCompanyId = user.fk_Companyid_Company ?? 0;
        var active = companies.FirstOrDefault(c => c.id_Company == desiredCompanyId)
                     ?? companies.FirstOrDefault();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.id_Users.ToString()),
            new(JwtRegisteredClaimNames.Sub,   user.id_Users.ToString()),
            new(JwtRegisteredClaimNames.Email, user.email ?? ""),

            new("provider",      user.authProvider ?? "LOCAL"),
            new("name",          user.name         ?? ""),
            new("surname",       user.surname       ?? ""),
            new("fullName",      $"{user.name} {user.surname}".Trim()),
            new("isMasterAdmin", user.isMasterAdmin ? "1" : "0"),

            // Active company
            new("companyId",    (active?.id_Company ?? 0).ToString()),
            new("companyName",   active?.name  ?? ""),
            new("companyCode",   active?.code  ?? ""),
            new("companyRole",   active?.role  ?? ""),
            new("companyImage",  active?.image ?? ""),

            // All memberships (for company switcher)
            new("companies", JsonSerializer.Serialize(companies)),

            // Password-change sentinel
            // We store a SHORT hash fingerprint (first 8 chars of the bcrypt hash).
            // This lets us invalidate old tokens when the user changes their
            // password WITHOUT embedding the full hash in the token.
            // The full bcrypt hash is never exposed.
            new("pwdFp", (user.password ?? "").Length >= 8
                            ? user.password![..8]
                            : ""),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // Cookie helpers

    /// <summary>
    /// Writes the JWT as a secure httpOnly SameSite=Strict cookie.
    /// The browser sends it automatically on every request; JS can't read it.
    /// </summary>
    public static void SetAuthCookie(HttpResponse response, string jwtToken, IConfiguration config)
    {
        var days = int.TryParse(config["Jwt:CookieDays"], out var d) ? d : 1;

        // CookieSecure defaults to true (production).
        // Set "Jwt:CookieSecure": "false" in appsettings.Development.json
        // so cookies work on plain http://localhost during development.
        var secure = !string.Equals(config["Jwt:CookieSecure"], "false",
                         StringComparison.OrdinalIgnoreCase);

        response.Cookies.Append("auth_token", jwtToken, new CookieOptions
        {
            HttpOnly = true,// JS cannot read this
            Secure = secure,// false in dev, true in prod
            SameSite = SameSiteMode.Lax,// Strict blocks cookie on first navigation; Lax is safe + works
            Expires = DateTimeOffset.UtcNow.AddDays(days),
            Path = "/",
        });
    }

    public static void ClearAuthCookie(HttpResponse response)
    {
        response.Cookies.Delete("auth_token", new CookieOptions
        {
            HttpOnly = true,
            Secure = false,// must match how it was set — browser won't clear otherwise
            SameSite = SameSiteMode.Lax,
            Path = "/",
        });
    }

    // Password-reset token (JWT-based, short-lived)

    public string GeneratePasswordResetToken(int userId)
    {
        var jwtKey = _config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key missing");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new("type", "reset"),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public ClaimsPrincipal? ValidateResetToken(string token)
    {
        var jwtKey = _config["Jwt:Key"] ?? "";

        try
        {
            var principal = new JwtSecurityTokenHandler().ValidateToken(token,
                new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero,
                }, out _);

            return principal.FindFirst("type")?.Value == "reset" ? principal : null;
        }
        catch { return null; }
    }
}