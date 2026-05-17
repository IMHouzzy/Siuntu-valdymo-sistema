using Bakalauras.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Bakalauras.API.Dtos;

[ApiController]
[Route("api/companies/{companyId:int}/integrations")]
[Authorize]
public class CompanyIntegrationsController : ControllerBase
{
    private readonly AppDbContext _db;

    public CompanyIntegrationsController(AppDbContext db) => _db = db;

    // Auth helpers

    private async Task<string?> GetMyRoleInCompany(int companyId, int userId)
        => await _db.company_users.AsNoTracking()
            .Where(cu => cu.fk_Companyid_Company == companyId && cu.fk_Usersid_Users == userId)
            .Select(cu => cu.role)
            .FirstOrDefaultAsync();

    private static bool CanManageIntegrations(string? role)
        => string.Equals(role, "OWNER", StringComparison.OrdinalIgnoreCase)
        || string.Equals(role, "ADMIN", StringComparison.OrdinalIgnoreCase);

    private async Task<bool> CanManageThisCompany(int companyId)
    {
        if (User.IsMasterAdmin()) return true;
        if (User.GetCompanyId() != companyId) return false;
        var role = await GetMyRoleInCompany(companyId, User.GetUserId());
        return CanManageIntegrations(role);
    }

    // GET /api/companies/{companyId}/integrations 
    // Lists all integrations for the company (without passwords).

    [HttpGet]
    public async Task<IActionResult> List(int companyId)
    {
        if (!await CanManageThisCompany(companyId))
            return Forbid("You cannot view integrations of this company.");

        var items = await _db.company_integrations.AsNoTracking()
            .Where(x => x.fk_Companyid_Company == companyId)
            .OrderByDescending(x => x.updatedAt)
            .Select(x => new CompanyIntegrationViewDto
            {
                Id        = x.id_CompanyIntegration,
                CompanyId = x.fk_Companyid_Company,
                Type      = x.type,
                BaseUrl   = x.baseUrl,
                Enabled   = x.enabled == true,
                UpdatedAt = x.updatedAt
            })
            .ToListAsync();

        return Ok(items);
    }


    // BUTENT
    

    [HttpPut("butent")]
    public async Task<IActionResult> UpsertButent(int companyId, [FromBody] CompanyIntegrationUpsertDto dto)
    {
        if (!await CanManageThisCompany(companyId))
            return Forbid("You cannot manage integrations of this company.");

        if (string.IsNullOrWhiteSpace(dto.Username)) return BadRequest("Username required.");
        if (string.IsNullOrWhiteSpace(dto.Password)) return BadRequest("Password required.");

        var baseUrl = NormalizeBaseUrl(dto.BaseUrl);
        if (dto.BaseUrl != null && baseUrl == null)
            return BadRequest("BaseUrl is not a valid absolute URL.");

        return await UpsertIntegration(companyId, "BUTENT", dto.Username, dto.Password, baseUrl, dto.Enabled);
    }

    [HttpPost("butent/disable")]
    public async Task<IActionResult> DisableButent(int companyId)
        => await DisableIntegration(companyId, "BUTENT");

    [HttpDelete("butent")]
    public async Task<IActionResult> DeleteButent(int companyId)
        => await DeleteIntegration(companyId, "BUTENT");

    //  DPD
    //  Enabling DPD also ensures the global DPD courier rows exist in the DB.

    [HttpPut("dpd")]
    public async Task<IActionResult> UpsertDpd(int companyId, [FromBody] CompanyIntegrationUpsertDto dto)
    {
        if (!await CanManageThisCompany(companyId))
            return Forbid("You cannot manage integrations of this company.");

        if (string.IsNullOrWhiteSpace(dto.Username)) return BadRequest("Username required.");
        if (string.IsNullOrWhiteSpace(dto.Password)) return BadRequest("Password required.");

        // Default to sandbox URL; company can override via BaseUrl
        var baseUrl = string.IsNullOrWhiteSpace(dto.BaseUrl)
            ? "https://sandbox-esiunta.dpd.lt/api/v1/"
            : dto.BaseUrl.Trim();

        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out _))
            return BadRequest("BaseUrl is not a valid absolute URL.");

        var result = await UpsertIntegration(companyId, "DPD", dto.Username, dto.Password, baseUrl, dto.Enabled);

        // Seed global DPD courier rows once (only if they don't exist yet)
        await EnsureDpdCouriersExistAsync();

        return result;
    }

    [HttpPost("dpd/disable")]
    public async Task<IActionResult> DisableDpd(int companyId)
        => await DisableIntegration(companyId, "DPD");

    [HttpDelete("dpd")]
    public async Task<IActionResult> DeleteDpd(int companyId)
    {
        var result = await DeleteIntegration(companyId, "DPD");
        // Also clear the cached DPD token so it cannot be reused
        var integ = await _db.company_integrations
            .FirstOrDefaultAsync(ci => ci.fk_Companyid_Company == companyId && ci.type == "DPD");
        if (integ != null)
        {
            integ.dpdToken         = null;
            integ.dpdTokenExpires  = null;
            integ.dpdTokenSecretId = null;
            await _db.SaveChangesAsync();
        }
        return result;
    }

    //LP Express (add more providers here with the same pattern)

    [HttpPut("lp-express")]
    public async Task<IActionResult> UpsertLpExpress(int companyId, [FromBody] CompanyIntegrationUpsertDto dto)
    {
        if (!await CanManageThisCompany(companyId))
            return Forbid();

        if (string.IsNullOrWhiteSpace(dto.Username)) return BadRequest("Username required.");
        if (string.IsNullOrWhiteSpace(dto.Password)) return BadRequest("Password required.");

        var baseUrl = string.IsNullOrWhiteSpace(dto.BaseUrl)
            ? "https://api.lpexpress.lt/v2/"
            : dto.BaseUrl.Trim();

        return await UpsertIntegration(companyId, "LP_EXPRESS", dto.Username, dto.Password, baseUrl, dto.Enabled);
    }

    [HttpPost("lp-express/disable")]
    public async Task<IActionResult> DisableLpExpress(int companyId)
        => await DisableIntegration(companyId, "LP_EXPRESS");

    [HttpDelete("lp-express")]
    public async Task<IActionResult> DeleteLpExpress(int companyId)
        => await DeleteIntegration(companyId, "LP_EXPRESS");

    //  Shared private helpers — all providers use these

    private async Task<IActionResult> UpsertIntegration(
        int companyId, string type,
        string username, string password, string? baseUrl, bool enabled)
    {
        var secretsJson = IntegrationSecrets.Pack(username, password, baseUrl);

        var existing = await _db.company_integrations
            .FirstOrDefaultAsync(x => x.fk_Companyid_Company == companyId && x.type == type);

        if (existing == null)
        {
            existing = new company_integration
            {
                fk_Companyid_Company = companyId,
                type                 = type,
                baseUrl              = baseUrl,
                encryptedSecrets     = secretsJson,
                enabled              = enabled
            };
            _db.company_integrations.Add(existing);
        }
        else
        {
            existing.baseUrl          = baseUrl;
            existing.encryptedSecrets = secretsJson;
            existing.enabled          = enabled;
            // Invalidate cached token whenever credentials change
            existing.dpdToken         = null;
            existing.dpdTokenExpires  = null;
            existing.dpdTokenSecretId = null;
        }

        await _db.SaveChangesAsync();

        return Ok(new
        {
            companyId,
            type      = existing.type,
            enabled   = existing.enabled,
            baseUrl   = existing.baseUrl,
            updatedAt = existing.updatedAt
        });
    }

    private async Task<IActionResult> DisableIntegration(int companyId, string type)
    {
        if (!await CanManageThisCompany(companyId)) return Forbid();

        var existing = await _db.company_integrations
            .FirstOrDefaultAsync(x => x.fk_Companyid_Company == companyId && x.type == type);

        if (existing == null) return NotFound($"{type} integration not found.");

        existing.enabled = false;
        await _db.SaveChangesAsync();
        return Ok();
    }

    private async Task<IActionResult> DeleteIntegration(int companyId, string type)
    {
        if (!await CanManageThisCompany(companyId)) return Forbid();

        var existing = await _db.company_integrations
            .FirstOrDefaultAsync(x => x.fk_Companyid_Company == companyId && x.type == type);

        if (existing == null) return NotFound($"{type} integration not found.");

        _db.company_integrations.Remove(existing);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private async Task EnsureDpdCouriersExistAsync()
    {
        var hasDpdParcel = await _db.couriers
            .AnyAsync(c => c.type == "DPD_PARCEL" && c.fk_Companyid_Company == null);

        if (!hasDpdParcel)
        {
            _db.couriers.AddRange(
                new courier { name = "DPD Paštomatas", type = "DPD_PARCEL", deliveryTermDays = 2, deliveryPrice = 3.50, fk_Companyid_Company = null },
                new courier { name = "DPD Kurjeris",   type = "DPD_HOME",   deliveryTermDays = 1, deliveryPrice = 5.00, fk_Companyid_Company = null }
            );
            await _db.SaveChangesAsync();
        }
    }

    private static string? NormalizeBaseUrl(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var trimmed = raw.Trim();
        return Uri.TryCreate(trimmed, UriKind.Absolute, out _) ? trimmed : null;
    }
}