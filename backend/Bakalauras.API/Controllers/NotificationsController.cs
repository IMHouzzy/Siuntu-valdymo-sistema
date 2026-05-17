using Bakalauras.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly AppDbContext _db;

    public NotificationsController(AppDbContext db)
    {
        _db = db;
    }

    private int GetUserId() => User.GetUserId();
    private int GetCompanyId() => User.GetCompanyId();

    private IQueryable<notification> ForCurrentUser()
    {
        return _db.notifications.Where(n =>
            (n.visibleToClient == true && n.fk_Usersid_Users == GetUserId()) ||
            (n.visibleToCompany == true && GetCompanyId() > 0 && n.fk_Companyid_Company == GetCompanyId())
        );
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(int page = 1, int pageSize = 30)
    {
        var query = ForCurrentUser().OrderByDescending(n => n.date);

        var total = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new { total, items });
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> Count()
    {
        var count = await ForCurrentUser()
            .CountAsync(n => !n.isRead);

        return Ok(new { count });
    }

    [HttpPut("{id}/read")]
    public async Task<IActionResult> Read(int id)
    {
        var n = await ForCurrentUser().FirstOrDefaultAsync(x => x.id_Notification == id);
        if (n == null) return NotFound();

        n.isRead = true;
        await _db.SaveChangesAsync();

        return Ok();
    }

    [HttpPut("mark-all-read")]
    public async Task<IActionResult> ReadAll()
    {
        await ForCurrentUser()
            .Where(n => !n.isRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.isRead, true));

        return Ok();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var n = await ForCurrentUser().FirstOrDefaultAsync(x => x.id_Notification == id);
        if (n == null) return NotFound();

        _db.notifications.Remove(n);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}