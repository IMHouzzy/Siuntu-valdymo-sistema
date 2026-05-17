using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/companies/{companyId:int}/courier-provider")]
[Authorize]
public class GenericCourierController : ControllerBase
{
    private readonly CourierProviderFactory _factory;

    public GenericCourierController(CourierProviderFactory factory)
        => _factory = factory;

    // ── GET /api/companies/{companyId}/courier-provider/{courierType}/lockers ──
    // courierType = "DPD_PARCEL" | "LP_EXPRESS_PARCEL" | "OMNIVA_PARCEL" etc.
    // Returns pickup points / lockers for the given country (default: LT).
    // 400 if the company does not have that integration enabled.

    [HttpGet("{courierType}/lockers")]
    public async Task<IActionResult> GetLockers(
        int    companyId,
        string courierType,
        [FromQuery] string countryCode = "LT",
        CancellationToken ct = default)
    {
        if (!IsAllowed(companyId)) return Forbid();

        Console.WriteLine($"[GenericCourierController] GetLockers company={companyId} type={courierType}");

        try
        {
            var provider = await _factory.GetProviderAsync(companyId, courierType, ct);

            if (!provider.SupportsLockers)
                return BadRequest($"{provider.ProviderName} does not support locker pickup.");

            var lockers = await provider.GetLockersAsync(countryCode, ct);
            Console.WriteLine($"[GenericCourierController] GetLockers returned {lockers.Count} lockers");
            return Ok(lockers);
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"[GenericCourierController] GetLockers 400: {ex.Message}");
            return BadRequest(ex.Message);
        }
        catch (NotSupportedException ex)
        {
            Console.WriteLine($"[GenericCourierController] GetLockers 400 NotSupported: {ex.Message}");
            return BadRequest(ex.Message);
        }
        catch (NotImplementedException ex)   { return StatusCode(501, ex.Message); }
        catch (Exception ex)
        {
            Console.WriteLine($"[GenericCourierController.GetLockers] {ex}");
            return StatusCode(502, "Courier provider API unreachable.");
        }
    }

    // ── GET /api/companies/{companyId}/courier-provider/tracking/{parcelNumber} ─
    // ?courierType=DPD_PARCEL
    // Returns tracking events for the given provider parcel number.

    [HttpGet("tracking/{parcelNumber}")]
    public async Task<IActionResult> GetTracking(
        int    companyId,
        string parcelNumber,
        [FromQuery] string courierType,
        CancellationToken ct = default)
    {
        if (!IsAllowed(companyId)) return Forbid();

        try
        {
            var provider = await _factory.GetProviderAsync(companyId, courierType, ct);
            var events   = await provider.GetTrackingAsync(parcelNumber, ct);
            return Ok(events);
        }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
        catch (NotSupportedException ex)     { return BadRequest(ex.Message); }
        catch (NotImplementedException ex)   { return StatusCode(501, ex.Message); }
        catch (Exception ex)
        {
            return StatusCode(502, ex.Message);
        }
    }

    private bool IsAllowed(int companyId)
        => User.IsMasterAdmin() || User.GetCompanyId() == companyId;
}