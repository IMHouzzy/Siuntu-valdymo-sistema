using Google.Apis.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Bakalauras.API.Models;
using Bakalauras.API.Dtos;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly JwtService _jwtService;
    private readonly IConfiguration _config;

    public AuthController(AppDbContext db, JwtService jwtService, IConfiguration config)
    {
        _db = db;
        _jwtService = jwtService;
        _config = config;
    }

    //Shared: build token + set cookie + return safe user info 

    private async Task<IActionResult> IssueTokenAsync(user user)
    {
        var jwt = await _jwtService.GenerateTokenAsync(user);
        JwtService.SetAuthCookie(Response, jwt, _config);

        // Return non-sensitive user info for the frontend to consume
        // (the token itself is in the httpOnly cookie — JS never sees it)
        var companies = await _db.company_users
            .AsNoTracking()
            .Where(cu => cu.fk_Usersid_Users == user.id_Users)
            .Select(cu => new
            {
                id_Company = cu.fk_Companyid_Company,
                name       = cu.fk_Companyid_CompanyNavigation.name,
                code       = cu.fk_Companyid_CompanyNavigation.companyCode,
                role       = cu.role,
                image      = cu.fk_Companyid_CompanyNavigation.image
            })
            .ToListAsync();

        var active = companies
            .FirstOrDefault(c => c.id_Company == (user.fk_Companyid_Company ?? 0))
            ?? companies.FirstOrDefault();

        return Ok(new
        {
            userId        = user.id_Users,
            email         = user.email,
            fullName      = $"{user.name} {user.surname}".Trim(),
            isMasterAdmin = user.isMasterAdmin,
            authProvider  = user.authProvider,
            companies,
            activeCompany = active,
            companyRole   = active?.role ?? "",
        });
    }

    //REGISTER

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            return BadRequest("Email and password required.");

        if (await _db.users.AnyAsync(u => u.email == dto.Email))
            return Conflict("User with this email already exists.");

        var user = new user
        {
            email                = dto.Email.Trim().ToLower(),
            password             = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            name                 = dto.Name,
            surname              = dto.Surname,
            authProvider         = "LOCAL",
            creationDate         = DateTime.UtcNow,
            isMasterAdmin        = false,
            fk_Companyid_Company = null,
        };

        _db.users.Add(user);
        await _db.SaveChangesAsync();

        return await IssueTokenAsync(user);
    }

    //LOGIN

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var user = await _db.users.FirstOrDefaultAsync(u => u.email == dto.Email);
        if (user == null) return Unauthorized("Neteisingas el. paštas.");

        if (string.IsNullOrEmpty(user.password) ||
            !BCrypt.Net.BCrypt.Verify(dto.Password, user.password))
            return Unauthorized("Neteisingas slaptažodis.");

        return await IssueTokenAsync(user);
    }

    // GOOGLE LOGIN

    [HttpPost("google")]
    public async Task<IActionResult> GoogleLogin(GoogleLoginDto dto)
    {
        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(dto.IdToken,
                new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { _config["GoogleAuth:ClientId"] }
                });
        }
        catch { return Unauthorized("Invalid Google token."); }

        var user = await _db.users.FirstOrDefaultAsync(u => u.googleId == payload.Subject);

        if (user == null)
        {
            user = new user
            {
                email                = payload.Email ?? $"unknown{Guid.NewGuid()}@google.com",
                name                 = payload.GivenName  ?? "Unknown",
                surname              = payload.FamilyName ?? "Unknown",
                googleId             = payload.Subject,
                authProvider         = "GOOGLE",
                creationDate         = DateTime.UtcNow,
                password             = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()),
                phoneNumber          = "",
                isMasterAdmin        = false,
                fk_Companyid_Company = null,
            };
            _db.users.Add(user);
            await _db.SaveChangesAsync();
        }

        return await IssueTokenAsync(user);
    }

    //LOGOUT

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        JwtService.ClearAuthCookie(Response);
        return Ok();
    }

    //ME 

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var userId = User.GetUserId();
        var user   = await _db.users.AsNoTracking().FirstOrDefaultAsync(u => u.id_Users == userId);
        if (user == null) return Unauthorized();

        return await IssueTokenAsync(user); // refreshes cookie too
    }

    // SWITCH COMPANY

    [Authorize]
    [HttpPost("switch-company/{companyId:int}")]
    public async Task<IActionResult> SwitchCompany(int companyId)
    {
        var userId   = User.GetUserId();
        var isMaster = User.IsMasterAdmin();

        if (companyId <= 0) return BadRequest("Invalid company.");

        if (isMaster)
        {
            if (!await _db.companies.AsNoTracking().AnyAsync(c => c.id_Company == companyId))
                return NotFound("Company not found.");
        }
        else
        {
            if (!await _db.company_users.AsNoTracking().AnyAsync(cu =>
                    cu.fk_Usersid_Users == userId &&
                    cu.fk_Companyid_Company == companyId))
                return Forbid();
        }

        var user = await _db.users.FirstOrDefaultAsync(u => u.id_Users == userId);
        if (user == null) return Unauthorized();

        user.fk_Companyid_Company = companyId;
        await _db.SaveChangesAsync();

        return await IssueTokenAsync(user);
    }
}