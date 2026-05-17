using Bakalauras.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Bakalauras.API.Dtos;

[ApiController]
[Route("api/profile")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly JwtService _jwtService;

    public ProfileController(AppDbContext db, JwtService jwtService)
    {
        _db = db;
        _jwtService = jwtService;
    }

    // GET own profile (base info + per-company client/employee data)

    [HttpGet]
public async Task<IActionResult> GetProfile()
{
    var userId = User.GetUserId();

    var user = await _db.users
        .AsNoTracking()
        .FirstOrDefaultAsync(u => u.id_Users == userId);

    if (user == null) return Unauthorized();

    // take first global client info
    var client = await _db.client_companies
        .AsNoTracking()
        .Where(x => x.fk_Clientid_Users == userId)
        .Select(x => new
        {
            x.deliveryAddress,
            x.city,
            x.country,
            x.vat,
            x.bankCode
        })
        .FirstOrDefaultAsync();

    var memberships = await _db.company_users
        .AsNoTracking()
        .Where(cu => cu.fk_Usersid_Users == userId)
        .Select(cu => new
        {
            companyId = cu.fk_Companyid_Company,
            companyName = cu.fk_Companyid_CompanyNavigation.name,
            cu.role,
            cu.position,
            cu.startDate,
            cu.active
        })
        .ToListAsync();

    return Ok(new
    {
        id = user.id_Users,
        user.name,
        user.surname,
        user.email,
        user.phoneNumber,
        user.authProvider,

        deliveryAddress = client?.deliveryAddress,
        city = client?.city,
        country = client?.country,
        vat = client?.vat,
        bankCode = client?.bankCode,

        memberships
    });
}

    // UPDATE base info (name, surname, phone)

    [HttpPut("info")]
public async Task<IActionResult> UpdateInfo([FromBody] UpdateProfileInfoDto dto)
{
    var userId = User.GetUserId();

    var user = await _db.users.FirstOrDefaultAsync(u => u.id_Users == userId);
    if (user == null) return Unauthorized();

    user.name = dto.Name?.Trim() ?? user.name;
    user.surname = dto.Surname?.Trim() ?? user.surname;
    user.phoneNumber = dto.PhoneNumber?.Trim() ?? user.phoneNumber;

    var clientRows = await _db.client_companies
        .Where(x => x.fk_Clientid_Users == userId)
        .ToListAsync();

    if (!clientRows.Any())
    {
        _db.client_companies.Add(new client_company
        {
            fk_Clientid_Users = userId,
            fk_Companyid_Company = 1, // fallback
            deliveryAddress = dto.DeliveryAddress,
            city = dto.City,
            country = dto.Country,
            vat = dto.Vat,
            bankCode = dto.BankCode
        });
    }
    else
    {
        foreach (var row in clientRows)
        {
            row.deliveryAddress = dto.DeliveryAddress;
            row.city = dto.City;
            row.country = dto.Country;
            row.vat = dto.Vat;
            row.bankCode = dto.BankCode;
        }
    }

    await _db.SaveChangesAsync();
    return Ok();
}

    // CHANGE PASSWORD

    [HttpPut("password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var userId = User.GetUserId();

        var user = await _db.users.FirstOrDefaultAsync(u => u.id_Users == userId);
        if (user == null) return Unauthorized();

        if (user.authProvider != "LOCAL")
            return BadRequest("Password change is only available for LOCAL accounts.");

        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.password))
            return BadRequest("Current password is incorrect.");

        if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 8)
            return BadRequest("New password must be at least 8 characters.");

        user.password = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        await _db.SaveChangesAsync();
        return Ok();
    }

    // DELETE own account

    [HttpDelete]
    public async Task<IActionResult> DeleteAccount()
    {
        var userId = User.GetUserId();

        var user = await _db.users.FirstOrDefaultAsync(u => u.id_Users == userId);
        if (user == null) return Unauthorized();

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var cus = await _db.company_users.Where(x => x.fk_Usersid_Users == userId).ToListAsync();
            _db.company_users.RemoveRange(cus);

            var ccs = await _db.client_companies.Where(x => x.fk_Clientid_Users == userId).ToListAsync();
            _db.client_companies.RemoveRange(ccs);

            _db.users.Remove(user);

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return StatusCode(500, ex.InnerException?.Message ?? ex.Message);
        }
    }
}
