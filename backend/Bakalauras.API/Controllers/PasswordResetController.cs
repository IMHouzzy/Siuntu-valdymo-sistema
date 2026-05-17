using Bakalauras.API.Models;
using Bakalauras.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Bakalauras.API.Dtos;
using System.Security.Cryptography;


[ApiController]
[Route("api/auth/reset-password")]
public class PasswordResetController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IEmailService _email;
    private readonly IConfiguration _cfg;

    public PasswordResetController(AppDbContext db, IEmailService email, IConfiguration cfg)
    {
        _db = db;
        _email = email;
        _cfg = cfg;
    }

    // STEP 1: user submits email
    // POST /api/auth/reset-password/request
    // Always returns 200 — never reveals whether the email exists.

    [HttpPost("request")]
    public async Task<IActionResult> Request([FromBody] ResetRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email))
            return BadRequest("El. pašto adresas privalomas.");

        var user = await _db.users
            .FirstOrDefaultAsync(u => u.email == dto.Email.Trim().ToLower());

        if (user == null || user.authProvider != "LOCAL")
            return Ok(); // silent — don't leak whether email exists

        // Secure random token, URL-safe base64
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                             .Replace("+", "-").Replace("/", "_").TrimEnd('=');

        // Store directly on the user row — no extra table
        user.resetToken = rawToken;
        user.resetTokenExpiry = DateTime.UtcNow.AddHours(1);

        await _db.SaveChangesAsync();

        var frontendBase = (_cfg["FrontendBaseUrl"] ?? "http://46.101.161.47").TrimEnd('/');
        var resetUrl = $"{frontendBase}/reset-password?token={rawToken}";

        await _email.SendAsync(
            to: user.email,
            subject: "Slaptažodžio atstatymas",
            htmlBody: $"Norėdami atstatyti slaptažodį, spauskite šią nuorodą:\n\n{resetUrl}\n\nNuoroda galioja 1 valandą."
        );

        return Ok();
    }

    // STEP 2: validate token (frontend calls this on page load)
    // GET /api/auth/reset-password/validate?token=xxx

    [HttpGet("validate")]
    public async Task<IActionResult> Validate([FromQuery] string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return BadRequest("Trūksta žetono.");

        var user = await _db.users.FirstOrDefaultAsync(u => u.resetToken == token);

        if (user == null)
            return BadRequest("Nuoroda negaliojanti arba jau panaudota.");

        if (user.resetTokenExpiry == null || user.resetTokenExpiry < DateTime.UtcNow)
            return StatusCode(410, "Nuoroda pasibaigė. Prašykite naujos.");

        return Ok();
    }

    // STEP 3: set new password
    // POST /api/auth/reset-password/confirm
    // Body: { "token": "...", "newPassword": "..." }

    [HttpPost("confirm")]
    public async Task<IActionResult> Confirm([FromBody] ResetConfirmDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Token) || string.IsNullOrWhiteSpace(dto.NewPassword))
            return BadRequest("Trūksta duomenų.");

        if (dto.NewPassword.Length < 8)
            return BadRequest("Slaptažodis turi būti bent 8 simbolių.");

        var user = await _db.users.FirstOrDefaultAsync(u => u.resetToken == dto.Token);

        if (user == null)
            return BadRequest("Nuoroda negaliojanti arba jau panaudota.");

        if (user.resetTokenExpiry == null || user.resetTokenExpiry < DateTime.UtcNow)
            return StatusCode(410, "Nuoroda pasibaigė. Prašykite naujos.");

        user.password = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.resetToken = null;   // wipe so token can't be reused
        user.resetTokenExpiry = null;

        await _db.SaveChangesAsync();
        return Ok();
    }
}

namespace Bakalauras.API.Dtos
{
    public class ResetRequestDto { public string Email { get; set; } = ""; }
    public class ResetConfirmDto
    {
        public string Token { get; set; } = "";
        public string NewPassword { get; set; } = "";
    }
}
