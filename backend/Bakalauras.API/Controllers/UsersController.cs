using Bakalauras.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Bakalauras.API.Dtos;
[ApiController]
[Route("api/users/")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;

    public UsersController(AppDbContext db)
    {
        _db = db;
    }

    // Helpers

    private int GetRequiredCompanyId()
    {
        var companyId = User.GetCompanyId();
        if (companyId <= 0)
            throw new UnauthorizedAccessException("No active company selected.");
        return companyId;
    }

    private Task<bool> UserBelongsToCompany(int companyId, int userId)
        => _db.company_users.AnyAsync(cu =>
            cu.fk_Companyid_Company == companyId &&
            cu.fk_Usersid_Users == userId);

    // LIST (staff only)

    [HttpGet("allUsers")]
    public async Task<IActionResult> GetAllUsers()
    {
        int companyId;
        try { companyId = GetRequiredCompanyId(); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }

        var users = await _db.users
            .AsNoTracking()
            .Where(u => _db.company_users.Any(cu =>
                cu.fk_Companyid_Company == companyId &&
                cu.fk_Usersid_Users == u.id_Users))
            .Select(u => new
            {
                u.id_Users,
                u.name,
                u.surname,
                u.email,
                u.phoneNumber,
                u.creationDate,
                u.authProvider,
                u.fk_Companyid_Company,
                u.isMasterAdmin
            })
            .ToListAsync();

        return Ok(users);
    }

    // LIST with full client + employee data

    [HttpGet("allUsersWithClients")]
    public async Task<IActionResult> GetAllUsersWithClients()
    {
        int companyId;
        try { companyId = GetRequiredCompanyId(); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }

        // Union of member ids and client ids for this company
        var memberIds = _db.company_users
            .AsNoTracking()
            .Where(cu => cu.fk_Companyid_Company == companyId)
            .Select(cu => cu.fk_Usersid_Users);

        var clientIds = _db.client_companies
            .AsNoTracking()
            .Where(cc => cc.fk_Companyid_Company == companyId)
            .Select(cc => cc.fk_Clientid_Users);

        var ids = await memberIds.Union(clientIds).Distinct().ToListAsync();

        var result = await _db.users
            .AsNoTracking()
            .Where(u => ids.Contains(u.id_Users))
            .Select(u => new
            {
                u.id_Users,
                u.name,
                u.surname,
                u.email,
                u.phoneNumber,
                u.creationDate,
                u.authProvider,
                u.fk_Companyid_Company,

                // Per-company client data
                client = _db.client_companies
                    .AsNoTracking()
                    .Where(cc => cc.fk_Companyid_Company == companyId && cc.fk_Clientid_Users == u.id_Users)
                    .Select(cc => new
                    {
                        cc.deliveryAddress,
                        cc.city,
                        cc.country,
                        cc.vat,
                        cc.bankCode,
                        cc.externalClientId
                    })
                    .FirstOrDefault(),

                // Membership row (contains position/startDate/active for staff)
                membership = _db.company_users
                    .AsNoTracking()
                    .Where(cu => cu.fk_Companyid_Company == companyId && cu.fk_Usersid_Users == u.id_Users)
                    .Select(cu => new
                    {
                        cu.role,
                        cu.position,
                        cu.startDate,
                        cu.active
                    })
                    .FirstOrDefault()
            })
            .ToListAsync();

        return Ok(result);
    }

    // CREATE

    [HttpPost("createUser")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
    {
        int companyId;
        try { companyId = GetRequiredCompanyId(); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }

        if (string.IsNullOrWhiteSpace(dto.Email))
            return BadRequest("Email is required.");

        if (await _db.users.AnyAsync(u => u.email == dto.Email))
            return Conflict("User with this email already exists.");

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // 1) Base user
            var user = new user
            {
                name = dto.Name,
                surname = dto.Surname,
                email = dto.Email,
                phoneNumber = dto.PhoneNumber,
                password = null,
                authProvider = "LOCAL",
                creationDate = DateTime.Now,
                fk_Companyid_Company = companyId,
                isMasterAdmin = false
            };

            _db.users.Add(user);
            await _db.SaveChangesAsync();

            // 2) Determine role for company_users
            string role;
            if (dto.IsEmployee)
                role = dto.Position switch
                {
                    "ADMIN" or "OWNER" => dto.Position,
                    "COURIER" => "COURIER",
                    _ => "STAFF"
                };
            else if (dto.IsClient)
                role = "CLIENT";
            else
                role = "USER";

            _db.company_users.Add(new company_user
            {
                fk_Companyid_Company = companyId,
                fk_Usersid_Users = user.id_Users,
                role = role,
                position = dto.IsEmployee ? dto.Position : null,
                startDate = dto.IsEmployee ? (dto.StartDate ?? DateTime.Now) : null,
                active = dto.IsEmployee ? dto.Active : true
            });

            await _db.SaveChangesAsync();

            // 3) Client data
            if (dto.IsClient)
            {
                _db.client_companies.Add(new client_company
                {
                    fk_Clientid_Users = user.id_Users,
                    fk_Companyid_Company = companyId,
                    externalClientId = null,
                    deliveryAddress = dto.DeliveryAddress,
                    city = dto.City,
                    country = dto.Country,
                    vat = dto.Vat,
                    bankCode = dto.BankCode
                });

                await _db.SaveChangesAsync();
            }

            await tx.CommitAsync();
            return Ok(new { userId = user.id_Users });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return StatusCode(500, ex.InnerException?.Message ?? ex.Message);
        }
    }

    // READ (SINGLE)

    [HttpGet("user/{id:int}")]
    public async Task<IActionResult> GetUser(int id)
    {
        int companyId;
        try { companyId = GetRequiredCompanyId(); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }

        if (!await UserBelongsToCompany(companyId, id))
            return StatusCode(403, "User is not in your company.");

        var user = await _db.users.AsNoTracking().FirstOrDefaultAsync(u => u.id_Users == id);
        if (user == null) return NotFound();

        var cc = await _db.client_companies.AsNoTracking()
            .FirstOrDefaultAsync(x => x.fk_Companyid_Company == companyId && x.fk_Clientid_Users == id);

        var cu = await _db.company_users.AsNoTracking()
            .FirstOrDefaultAsync(x => x.fk_Companyid_Company == companyId && x.fk_Usersid_Users == id);

        return Ok(new
        {
            id = user.id_Users,
            user.name,
            user.surname,
            user.email,
            user.phoneNumber,

            // Client data (per-company)
            isClient = cc != null,
            deliveryAddress = cc?.deliveryAddress,
            city = cc?.city,
            country = cc?.country,
            vat = cc?.vat,
            bankCode = cc?.bankCode,
            externalClientId = cc?.externalClientId,

            // Staff data (from company_users)
            role = cu?.role,
            isEmployee = cu != null && (cu.role == "STAFF" || cu.role == "ADMIN" || cu.role == "OWNER" || cu.role == "COURIER"),
            position = cu?.position,
            startDate = cu?.startDate,
            active = cu?.active ?? false,
            isAdmin = cu != null && (cu.role == "ADMIN" || cu.role == "OWNER")
        });
    }

    // UPDATE

    [HttpPut("editUser/{id:int}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] CreateUserDto dto)
    {
        int companyId;
        try { companyId = GetRequiredCompanyId(); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }

        if (!await UserBelongsToCompany(companyId, id))
            return StatusCode(403, "User is not in your company.");

        var user = await _db.users.FirstOrDefaultAsync(u => u.id_Users == id);
        if (user == null) return NotFound();

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // Base user fields
            user.name = dto.Name;
            user.surname = dto.Surname;
            user.email = dto.Email;
            user.phoneNumber = dto.PhoneNumber;
            user.fk_Companyid_Company = companyId;

            await _db.SaveChangesAsync();

            // Client data (client_company)
            var cc = await _db.client_companies.FirstOrDefaultAsync(x =>
                x.fk_Companyid_Company == companyId && x.fk_Clientid_Users == id);

            if (dto.IsClient)
            {
                if (cc == null)
                {
                    _db.client_companies.Add(new client_company
                    {
                        fk_Clientid_Users = id,
                        fk_Companyid_Company = companyId,
                        externalClientId = null,
                        deliveryAddress = dto.DeliveryAddress,
                        city = dto.City,
                        country = dto.Country,
                        vat = dto.Vat,
                        bankCode = dto.BankCode
                    });
                }
                else
                {
                    cc.deliveryAddress = dto.DeliveryAddress;
                    cc.city = dto.City;
                    cc.country = dto.Country;
                    cc.vat = dto.Vat;
                    cc.bankCode = dto.BankCode;
                    // externalClientId is managed by sync worker, not by this endpoint
                }
            }
            else
            {
                // Remove client_company link for this company only
                if (cc != null) _db.client_companies.Remove(cc);
            }

            // company_users (role / position / startDate / active)
            var cu = await _db.company_users.FirstOrDefaultAsync(x =>
                x.fk_Companyid_Company == companyId && x.fk_Usersid_Users == id);

            string newRole;
            if (dto.IsEmployee)
                newRole = dto.Position switch
                {
                    "ADMIN" or "OWNER" => dto.Position,
                    "COURIER" => "COURIER",
                    _ => "STAFF"
                };
            else if (dto.IsClient)
                newRole = "CLIENT";
            else
                newRole = "USER";

            if (cu == null)
            {
                _db.company_users.Add(new company_user
                {
                    fk_Companyid_Company = companyId,
                    fk_Usersid_Users = id,
                    role = newRole,
                    position = dto.IsEmployee ? dto.Position : null,
                    startDate = dto.IsEmployee ? (dto.StartDate ?? DateTime.Now) : null,
                    active = dto.IsEmployee ? dto.Active : true
                });
            }
            else
            {
                cu.role = newRole;
                cu.position = dto.IsEmployee ? dto.Position : null;
                if (dto.IsEmployee)
                {
                    cu.startDate = dto.StartDate ?? cu.startDate ?? DateTime.Now;
                    cu.active = dto.Active;
                }
                else
                {
                    cu.active = true;
                }
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return Ok();
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return StatusCode(500, ex.InnerException?.Message ?? ex.Message);
        }
    }

    // DELETE 

    [HttpDelete("deleteUser/{id:int}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        int companyId;
        try { companyId = GetRequiredCompanyId(); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }

        if (!await UserBelongsToCompany(companyId, id))
            return StatusCode(403, "User is not in your company.");

        var user = await _db.users.FirstOrDefaultAsync(u => u.id_Users == id);
        if (user == null) return NotFound();

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // Remove all company_users links (across all companies)
            var cus = await _db.company_users.Where(x => x.fk_Usersid_Users == id).ToListAsync();
            _db.company_users.RemoveRange(cus);

            // Remove all client_company links (across all companies)
            var ccs = await _db.client_companies.Where(x => x.fk_Clientid_Users == id).ToListAsync();
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