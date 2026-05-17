using Bakalauras.API.Models;
using Bakalauras.API.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/companies/{companyId:int}/couriers")]
[Authorize]
public class CouriersController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly CourierProviderFactory _providerFactory;

    public CouriersController(AppDbContext db, CourierProviderFactory providerFactory)
    {
        _db = db;
        _providerFactory = providerFactory;
    }

    // Auth helpers

    private bool CallerIsInCompany(int companyId)
        => User.IsMasterAdmin() || User.GetCompanyId() == companyId;

    private async Task<bool> CallerCanManage(int companyId)
    {
        if (User.IsMasterAdmin()) return true;
        if (User.GetCompanyId() != companyId) return false;
        var role = await _db.company_users
            .Where(cu => cu.fk_Companyid_Company == companyId &&
                         cu.fk_Usersid_Users == User.GetUserId())
            .Select(cu => cu.role)
            .FirstOrDefaultAsync();
        return role is "OWNER" or "ADMIN";
    }

    // GET /api/companies/{companyId}/couriers 
    // Returns couriers visible to this company:
    //   Company-private CUSTOM couriers (fk_Company == companyId)
    //   Global CUSTOM couriers (fk_Company == NULL)
    //   Provider couriers (DPD_*, LP_EXPRESS_*, …)
    //   ONLY shown when the company has that provider integration enabled
    //   Company A with DPD sees DPD couriers; Company B without DPD does NOT

    [HttpGet]
    public async Task<IActionResult> List(int companyId)
    {
        if (!CallerIsInCompany(companyId)) return Forbid();

        var enabledKeys = await _providerFactory.GetEnabledIntegrationKeysAsync(companyId);

        var allowedProviderTypes = CourierProviderFactory.AllProviderCourierTypes
            .Where(t =>
            {
                var key = CourierProviderFactory.GetIntegrationKey(t);
                return key != null && enabledKeys.Contains(key);
            })
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var couriers = await _db.couriers
            .AsNoTracking()
            .Where(c =>

                (c.type == "CUSTOM" && c.fk_Companyid_Company == companyId) ||
                // Global CUSTOM couriers (NULL company = visible to everyone)
                (c.type == "CUSTOM" && c.fk_Companyid_Company == null) ||
                // Provider couriers — only if THIS company has that integration enabled
                (c.fk_Companyid_Company == null && allowedProviderTypes.Contains(c.type))
            )
            // Own couriers first, then alphabetical
            .OrderBy(c => c.fk_Companyid_Company == null ? 1 : 0)
            .ThenBy(c => c.name)
            .Select(c => new CourierDto
            {
                Id = c.id_Courier,
                Name = c.name,
                Type = c.type,
                ContactPhone = c.contactPhone,
                DeliveryTermDays = c.deliveryTermDays,
                DeliveryPrice = c.deliveryPrice,
                IsOwn = c.fk_Companyid_Company == companyId,
                SupportsLockers = c.type.EndsWith("_PARCEL"),
            })
            .ToListAsync();

        return Ok(couriers);
    }

    // POST /api/companies/{companyId}/couriers
    // Creates a new company-private CUSTOM courier.
    // Only OWNER / ADMIN (or master admin) may create.

    [HttpPost]
    public async Task<IActionResult> Create(int companyId, [FromBody] UpsertCourierDto dto)
    {
        if (!await CallerCanManage(companyId)) return Forbid();
        if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest("Name is required.");

        var c = new courier
        {
            name = dto.Name.Trim(),
            contactPhone = dto.ContactPhone?.Trim(),
            deliveryTermDays = dto.DeliveryTermDays,
            deliveryPrice = dto.DeliveryPrice,
            type = "CUSTOM",
            fk_Companyid_Company = companyId,
        };

        _db.couriers.Add(c);
        await _db.SaveChangesAsync();

        return Ok(new CourierDto
        {
            Id = c.id_Courier,
            Name = c.name,
            Type = c.type,
            ContactPhone = c.contactPhone,
            DeliveryTermDays = c.deliveryTermDays,
            DeliveryPrice = c.deliveryPrice,
            IsOwn = true,
            SupportsLockers = false,
        });
    }

    // PUT /api/companies/{companyId}/couriers/{courierId}
    // Updates a company-private CUSTOM courier.
    // Cannot edit global couriers or provider couriers.

    [HttpPut("{courierId:int}")]
    public async Task<IActionResult> Update(int companyId, int courierId, [FromBody] UpsertCourierDto dto)
    {
        if (!await CallerCanManage(companyId)) return Forbid();
        if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest("Name is required.");

        var c = await _db.couriers
            .FirstOrDefaultAsync(x => x.id_Courier == courierId
                                   && x.fk_Companyid_Company == companyId
                                   && x.type == "CUSTOM");
        if (c == null)
            return NotFound("Courier not found or cannot be edited by your company.");

        c.name = dto.Name.Trim();
        c.contactPhone = dto.ContactPhone?.Trim();
        c.deliveryTermDays = dto.DeliveryTermDays;
        c.deliveryPrice = dto.DeliveryPrice;

        await _db.SaveChangesAsync();
        return Ok();
    }

    // DELETE /api/companies/{companyId}/couriers/{courierId}
    // Deletes a company-private CUSTOM courier.

    [HttpDelete("{courierId:int}")]
    public async Task<IActionResult> Delete(int companyId, int courierId)
    {
        if (!await CallerCanManage(companyId)) return Forbid();

        var c = await _db.couriers
            .FirstOrDefaultAsync(x => x.id_Courier == courierId
                                   && x.fk_Companyid_Company == companyId
                                   && x.type == "CUSTOM");
        if (c == null)
            return NotFound("Courier not found or cannot be deleted by your company.");

        _db.couriers.Remove(c);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}