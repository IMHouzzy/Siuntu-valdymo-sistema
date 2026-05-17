using Bakalauras.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Bakalauras.API.Dtos;

[ApiController]
[Route("api/companies")]
[Authorize]
public class CompaniesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly JwtService _jwt;
    private readonly IWebHostEnvironment _env;

    public CompaniesController(AppDbContext db, JwtService jwt, IWebHostEnvironment env)
    {
        _db = db;
        _jwt = jwt;
        _env = env;
    }

    // Helpers

    private async Task<string?> GetMyRoleInCompany(int companyId, int userId)
        => await _db.company_users.AsNoTracking()
            .Where(cu => cu.fk_Companyid_Company == companyId && cu.fk_Usersid_Users == userId)
            .Select(cu => cu.role)
            .FirstOrDefaultAsync();

    private static bool CanEditCompany(string? role)
        => string.Equals(role, "OWNER", StringComparison.OrdinalIgnoreCase)
        || string.Equals(role, "ADMIN", StringComparison.OrdinalIgnoreCase);

    private static bool CanManageMembers(string? role)
        => string.Equals(role, "OWNER", StringComparison.OrdinalIgnoreCase)
        || string.Equals(role, "ADMIN", StringComparison.OrdinalIgnoreCase);

    private async Task<bool> CanManageThisCompanyMembers(int companyId)
    {
        if (User.IsMasterAdmin()) return true;
        var activeCompanyId = User.GetCompanyId();
        if (activeCompanyId != companyId) return false;
        var role = await GetMyRoleInCompany(companyId, User.GetUserId());
        return CanManageMembers(role);
    }

    // Shared select projection (avoids repeating the same anonymous type)

    private static object MapCompany(company c) => new
    {
        c.id_Company,
        c.name,
        code = c.companyCode,
        c.active,
        c.creationDate,
        c.email,
        c.phoneNumber,
        c.address,
        c.documentCode,
        c.image,
        // Legacy free-text address strings
        c.shippingAddress,
        c.returnAddress,
        // Structured shipping address
        c.shippingStreet,
        c.shippingCity,
        c.shippingPostalCode,
        c.shippingCountry,
        // Structured return address
        c.returnStreet,
        c.returnCity,
        c.returnPostalCode,
        c.returnCountry,
    };

    // LIST 

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var userId = User.GetUserId();
        var isMaster = User.IsMasterAdmin();

        IQueryable<company> q = _db.companies.AsNoTracking();

        if (!isMaster)
        {
            q = q.Where(c => _db.company_users.Any(cu =>
                cu.fk_Usersid_Users == userId &&
                cu.fk_Companyid_Company == c.id_Company));
        }

        var items = await q.OrderByDescending(c => c.id_Company).ToListAsync();
        return Ok(items.Select(MapCompany));
    }

    // GET 

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var userId = User.GetUserId();
        var isMaster = User.IsMasterAdmin();

        if (!isMaster)
        {
            var member = await _db.company_users.AsNoTracking()
                .AnyAsync(cu => cu.fk_Usersid_Users == userId && cu.fk_Companyid_Company == id);
            if (!member) return Forbid("Not in this company.");
        }

        var c = await _db.companies.AsNoTracking()
            .FirstOrDefaultAsync(x => x.id_Company == id);

        if (c == null) return NotFound();
        return Ok(MapCompany(c));
    }

    // CREATE (master only)

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CompanyUpsertDto dto)
    {
        if (!User.IsMasterAdmin()) return Forbid("Only master admin can create companies.");
        if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest("Name required.");
        if (string.IsNullOrWhiteSpace(dto.CompanyCode)) return BadRequest("CompanyCode required.");

        if (await _db.companies.AnyAsync(c => c.companyCode == dto.CompanyCode))
            return Conflict("CompanyCode already exists.");

        var creatorUserId = User.GetUserId();

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var c = new company
            {
                name = dto.Name.Trim(),
                companyCode = dto.CompanyCode.Trim(),
                active = dto.Active,
                creationDate = DateTime.UtcNow,
                documentCode = dto.DocumentCode ?? "",
                phoneNumber = dto.PhoneNumber ?? "",
                address = dto.Address ?? "",
                email = dto.Email ?? "",
                image = dto.Image ?? "",

                shippingAddress = dto.ShippingAddress,
                returnAddress = dto.ReturnAddress,


                shippingStreet = dto.ShippingStreet?.Trim(),
                shippingCity = dto.ShippingCity?.Trim(),
                shippingPostalCode = dto.ShippingPostalCode?.Trim(),
                shippingCountry = string.IsNullOrWhiteSpace(dto.ShippingCountry) ? "LT" : dto.ShippingCountry.Trim().ToUpper(),

                // Structured return
                returnStreet = dto.ReturnStreet?.Trim(),
                returnCity = dto.ReturnCity?.Trim(),
                returnPostalCode = dto.ReturnPostalCode?.Trim(),
                returnCountry = string.IsNullOrWhiteSpace(dto.ReturnCountry) ? "LT" : dto.ReturnCountry.Trim().ToUpper(),
            };

            _db.companies.Add(c);
            await _db.SaveChangesAsync();

            var exists = await _db.company_users.AnyAsync(cu =>
                cu.fk_Companyid_Company == c.id_Company &&
                cu.fk_Usersid_Users == creatorUserId);

            if (!exists)
            {
                _db.company_users.Add(new company_user
                {
                    fk_Companyid_Company = c.id_Company,
                    fk_Usersid_Users = creatorUserId,
                    role = "OWNER",
                    position = "ADMIN",
                    startDate = DateTime.UtcNow,
                    active = true
                });
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return Ok(new { c.id_Company });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return StatusCode(500, ex.InnerException?.Message ?? ex.Message);
        }
    }

    // UPDATE 

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] CompanyUpsertDto dto)
    {
        var userId = User.GetUserId();
        var isMaster = User.IsMasterAdmin();

        if (!isMaster)
        {
            var activeCompanyId = User.GetCompanyId();
            if (activeCompanyId != id) return Forbid("You can edit only your company.");
            var role = await GetMyRoleInCompany(id, userId);
            if (!CanEditCompany(role)) return Forbid("Only company admin can edit this company.");
        }

        var c = await _db.companies.FirstOrDefaultAsync(x => x.id_Company == id);
        if (c == null) return NotFound();

        if (!string.Equals(c.companyCode, dto.CompanyCode, StringComparison.OrdinalIgnoreCase))
        {
            if (!isMaster) return Forbid("Only master can change company code.");
            if (string.IsNullOrWhiteSpace(dto.CompanyCode)) return BadRequest("CompanyCode required.");
            if (await _db.companies.AnyAsync(x => x.id_Company != id && x.companyCode == dto.CompanyCode))
                return Conflict("CompanyCode already exists.");
            c.companyCode = dto.CompanyCode.Trim();
        }

        c.name = dto.Name?.Trim() ?? c.name;
        c.active = dto.Active;
        c.documentCode = dto.DocumentCode ?? "";
        c.phoneNumber = dto.PhoneNumber ?? "";
        c.address = dto.Address ?? "";
        c.email = dto.Email ?? "";
        c.image = dto.Image ?? "";


        c.shippingAddress = dto.ShippingAddress;
        c.returnAddress = dto.ReturnAddress;

        c.shippingStreet = dto.ShippingStreet?.Trim();
        c.shippingCity = dto.ShippingCity?.Trim();
        c.shippingPostalCode = dto.ShippingPostalCode?.Trim();
        c.shippingCountry = string.IsNullOrWhiteSpace(dto.ShippingCountry) ? "LT" : dto.ShippingCountry.Trim().ToUpper();

        c.returnStreet = dto.ReturnStreet?.Trim();
        c.returnCity = dto.ReturnCity?.Trim();
        c.returnPostalCode = dto.ReturnPostalCode?.Trim();
        c.returnCountry = string.IsNullOrWhiteSpace(dto.ReturnCountry) ? "LT" : dto.ReturnCountry.Trim().ToUpper();

        await _db.SaveChangesAsync();

        // Refresh JWT if this is the caller's active company
        var activeCompanyIdClaim = User.GetCompanyId();
        if (isMaster || activeCompanyIdClaim == id)
        {
            var u = await _db.users.AsNoTracking().FirstAsync(x => x.id_Users == userId);
            var newToken = await _jwt.GenerateTokenAsync(u);
            return Ok(new { token = newToken });
        }

        return Ok();
    }

    // DELETE (master only) 

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!User.IsMasterAdmin()) return Forbid("Only master admin can delete companies.");

        var c = await _db.companies.FirstOrDefaultAsync(x => x.id_Company == id);
        if (c == null) return NotFound();

        _db.companies.Remove(c);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // MEMBERS LIST

    [HttpGet("{id:int}/members")]
    public async Task<IActionResult> Members(int id)
    {
        if (!await CanManageThisCompanyMembers(id))
            return Forbid("You cannot view members of this company.");

        var members = await _db.company_users.AsNoTracking()
            .Where(cu => cu.fk_Companyid_Company == id)
            .Where(cu => cu.role == "OWNER" || cu.role == "ADMIN" || cu.role == "STAFF" || cu.role == "COURIER")
            .Select(cu => new
            {
                userId = cu.fk_Usersid_Users,
                role = cu.role,
                position = cu.position,
                startDate = cu.startDate,
                active = cu.active,
                email = cu.fk_Usersid_UsersNavigation.email,
                name = cu.fk_Usersid_UsersNavigation.name,
                surname = cu.fk_Usersid_UsersNavigation.surname,
                isMasterAdmin = cu.fk_Usersid_UsersNavigation.isMasterAdmin
            })
            .ToListAsync();

        return Ok(members);
    }

    // ADD MEMBER

    [HttpPost("{id:int}/members")]
    public async Task<IActionResult> AddMember(int id, [FromBody] MemberUpsertDto dto)
    {
        if (!await CanManageThisCompanyMembers(id))
            return Forbid("You cannot add members to this company.");

        if (dto.UserId <= 0) return BadRequest("UserId invalid.");

        var roleToSet = (dto.Role ?? "STAFF").Trim().ToUpperInvariant();
        if (roleToSet is not ("OWNER" or "ADMIN" or "STAFF" or "CLIENT" or "COURIER"))
            return BadRequest("Invalid role. Allowed: OWNER, ADMIN, STAFF, CLIENT OR COURIER.");

        if (!await _db.companies.AnyAsync(c => c.id_Company == id))
            return NotFound("Company not found.");
        if (!await _db.users.AnyAsync(u => u.id_Users == dto.UserId))
            return NotFound("User not found.");

        if (roleToSet is "OWNER" or "ADMIN" or "STAFF" or "COURIER")
        {
            var hasStaffElsewhere = await _db.company_users.AsNoTracking()
                .AnyAsync(cu =>
                    cu.fk_Usersid_Users == dto.UserId &&
                    cu.fk_Companyid_Company != id &&
                    (cu.role == "OWNER" || cu.role == "ADMIN" || cu.role == "STAFF" || cu.role == "COURIER"));
            if (hasStaffElsewhere)
                return Conflict("This user already has STAFF/ADMIN/OWNER role in another company.");
        }

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var cu = await _db.company_users
                .FirstOrDefaultAsync(x => x.fk_Companyid_Company == id && x.fk_Usersid_Users == dto.UserId);

            if (cu == null)
            {
                _db.company_users.Add(new company_user
                {
                    fk_Companyid_Company = id,
                    fk_Usersid_Users = dto.UserId,
                    role = roleToSet,
                    position = dto.Position,
                    startDate = dto.StartDate ?? DateTime.UtcNow,
                    active = true
                });
            }
            else
            {
                cu.role = roleToSet;
                cu.position = dto.Position ?? cu.position;
                cu.active = true;
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

    // UPDATE MEMBER ROLE

    [HttpPut("{id:int}/members/{userId:int}")]
    public async Task<IActionResult> UpdateMemberRole(int id, int userId, [FromBody] MemberUpsertDto dto)
    {
        if (!await CanManageThisCompanyMembers(id))
            return Forbid("You cannot change roles in this company.");

        var actorId = User.GetUserId();
        var actorIsMaster = User.IsMasterAdmin();

        if (!actorIsMaster && actorId == userId)
            return Forbid("You cannot change your own role.");

        if (!actorIsMaster)
        {
            var targetIsMaster = await _db.users.AsNoTracking()
                .Where(u => u.id_Users == userId)
                .Select(u => u.isMasterAdmin)
                .FirstOrDefaultAsync();
            if (targetIsMaster) return Forbid("You cannot change master admin role.");
        }

        var nextRole = (dto.Role ?? "STAFF").Trim().ToUpperInvariant();
        if (nextRole is not ("OWNER" or "ADMIN" or "STAFF" or "CLIENT" or "CLIENT" or "COURIER"))
            return BadRequest("Invalid role.");

        if (nextRole is "OWNER" or "ADMIN" or "STAFF" or "COURIER")
        {
            var hasStaffElsewhere = await _db.company_users.AsNoTracking()
                .AnyAsync(cu =>
                    cu.fk_Usersid_Users == userId &&
                    cu.fk_Companyid_Company != id &&
                    (cu.role == "OWNER" || cu.role == "ADMIN" || cu.role == "STAFF" || cu.role == "COURIER"));
            if (hasStaffElsewhere)
                return Conflict("This user already has STAFF/ADMIN/OWNER role in another company.");
        }

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var cu = await _db.company_users
                .FirstOrDefaultAsync(x => x.fk_Companyid_Company == id && x.fk_Usersid_Users == userId);
            if (cu == null) return NotFound("Membership not found.");

            cu.role = nextRole;
            cu.position = dto.Position ?? cu.position;
            if (dto.StartDate.HasValue) cu.startDate = dto.StartDate;

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return Ok(new { userId, role = cu.role });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return StatusCode(500, ex.InnerException?.Message ?? ex.Message);
        }
    }

    // REMOVE MEMBER

    [HttpDelete("{id:int}/members/{userId:int}")]
    public async Task<IActionResult> RemoveMember(int id, int userId)
    {
        if (!await CanManageThisCompanyMembers(id))
            return Forbid("You cannot remove members from this company.");

        var actorId = User.GetUserId();
        var actorIsMaster = User.IsMasterAdmin();

        if (!actorIsMaster && actorId == userId)
            return Forbid("You cannot remove yourself.");

        if (!actorIsMaster)
        {
            var targetIsMaster = await _db.users.AsNoTracking()
                .Where(u => u.id_Users == userId)
                .Select(u => u.isMasterAdmin)
                .FirstOrDefaultAsync();
            if (targetIsMaster) return Forbid("You cannot remove master admin.");
        }

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var cu = await _db.company_users
                .FirstOrDefaultAsync(x => x.fk_Companyid_Company == id && x.fk_Usersid_Users == userId);
            if (cu == null) return NotFound();

            var isClient = await _db.client_companies.AnyAsync(cc =>
                cc.fk_Companyid_Company == id && cc.fk_Clientid_Users == userId);

            if (isClient)
            {
                cu.role = "CLIENT";
                cu.position = null;
                cu.active = false;
            }
            else
            {
                _db.company_users.Remove(cu);
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return Ok(new { userId, role = isClient ? "CLIENT" : "REMOVED" });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return StatusCode(500, ex.InnerException?.Message ?? ex.Message);
        }
    }

    // ASSIGNABLE USERS

    [HttpGet("{id:int}/assignable-users")]
    public async Task<IActionResult> AssignableUsers(int id)
    {
        if (!await CanManageThisCompanyMembers(id))
            return Forbid("Only company admin can manage members.");

        var users = await _db.company_users
            .Where(cu => cu.fk_Companyid_Company == id)
            .Select(cu => new
            {
                id = cu.fk_Usersid_UsersNavigation.id_Users,
                email = cu.fk_Usersid_UsersNavigation.email,
                name = cu.fk_Usersid_UsersNavigation.name,
                surname = cu.fk_Usersid_UsersNavigation.surname,
                role = cu.role,
                position = cu.position,
                active = cu.active
            })
            .OrderBy(u => u.surname)
            .ThenBy(u => u.name)
            .ToListAsync();

        return Ok(users);
    }
    // LOGO UPLOAD
    // POST /api/companies/{id}/logo
    // Saves file to wwwroot/uploads/companies/{id}/logo.{ext}
    // Returns { imageUrl: "/uploads/companies/{id}/logo.jpg" }

    [HttpPost("{id:int}/logo")]
    public async Task<IActionResult> UploadLogo(int id, IFormFile file)
    {
        var userId = User.GetUserId();
        var isMaster = User.IsMasterAdmin();

        if (!isMaster)
        {
            var activeCompanyId = User.GetCompanyId();
            if (activeCompanyId != id) return Forbid("You can edit only your company.");
            var role = await GetMyRoleInCompany(id, userId);
            if (!CanEditCompany(role)) return Forbid("Only company admin can edit this company.");
        }

        if (file == null || file.Length == 0)
            return BadRequest("No file provided.");

        var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".svg" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowed.Contains(ext))
            return BadRequest("Allowed formats: jpg, png, webp, svg.");

        var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var dir = Path.Combine(webRoot, "uploads", "companies", id.ToString());
        Directory.CreateDirectory(dir);

        // Always overwrite to the same filename — simple, no orphans
        var fileName = $"logo{ext}";
        var fullPath = Path.Combine(dir, fileName);

        await using var stream = System.IO.File.Create(fullPath);
        await file.CopyToAsync(stream);

        var imageUrl = $"/uploads/companies/{id}/{fileName}";

        // Persist to DB
        var c = await _db.companies.FirstOrDefaultAsync(x => x.id_Company == id);
        if (c != null) { c.image = imageUrl; await _db.SaveChangesAsync(); }

        return Ok(new { imageUrl });
    }
}