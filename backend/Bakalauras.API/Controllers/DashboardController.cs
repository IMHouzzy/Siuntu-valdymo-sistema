using Bakalauras.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _db;

    public DashboardController(AppDbContext db) => _db = db;

    //  Helpers 

    private int GetRequiredCompanyId()
    {
        var id = User.GetCompanyId();
        if (id <= 0) throw new UnauthorizedAccessException("No active company selected.");
        return id;
    }

    private async Task<bool> IsStaffAsync(int companyId)
    {
        if (User.IsMasterAdmin()) return true;
        var userId = User.GetUserId();
        var role = await _db.company_users
            .AsNoTracking()
            .Where(cu => cu.fk_Companyid_Company == companyId && cu.fk_Usersid_Users == userId)
            .Select(cu => cu.role)
            .FirstOrDefaultAsync();
        return role is "OWNER" or "ADMIN" or "STAFF";
    }

    //  GET /api/dashboard/stats 
    // period: "year"  — last 12 months, grouped by month
    //         "month" — last 30 days, grouped by day
    //         "week"  — last 7 days, grouped by day
    //         "day"   — last 24 hours, grouped by hour
    //         "all"   — all time from the earliest order, grouped by year

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats([FromQuery] string period = "month")
    {
        int companyId;
        try { companyId = GetRequiredCompanyId(); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }

        if (!await IsStaffAsync(companyId))
            return Forbid("Only company staff can view dashboard.");

        var now = DateTime.UtcNow;
        var p = period.ToLowerInvariant();

        //  Orders 

        var allOrders = await _db.orders
            .AsNoTracking()
            .Where(o => o.fk_Companyid_Company == companyId)
            .Select(o => new { o.id_Orders, o.OrdersDate, o.totalAmount, o.status })
            .ToListAsync();

        // Determine period window
        DateTime from;
        if (p == "all")
        {
            from = allOrders.Count > 0
                ? allOrders.Min(o => o.OrdersDate.Date)
                : now.Date;
        }
        else
        {
            from = p switch
            {
                "year"  => now.AddMonths(-11).Date.AddDays(1 - now.AddMonths(-11).Day),
                "week"  => now.AddDays(-6).Date,
                "day"   => now.AddHours(-23),
                _       => now.AddDays(-29).Date  // month
            };
        }

        var ordersInPeriod = allOrders.Where(o => o.OrdersDate >= from).ToList();

        var totalOrders   = allOrders.Count;
        var totalRevenue  = allOrders.Sum(o => o.totalAmount);
        var periodRevenue = ordersInPeriod.Sum(o => o.totalAmount);

        // Orders over time — fill every bucket so chart has no gaps
        List<object> ordersOverTime;

        if (p == "all")
        {
            // Group by year from the earliest order year to current year
            var minYear = allOrders.Count > 0 ? allOrders.Min(o => o.OrdersDate.Year) : now.Year;
            var years = Enumerable.Range(minYear, now.Year - minYear + 1)
                .Select(y => y.ToString())
                .ToList();

            var grouped = ordersInPeriod
                .GroupBy(o => o.OrdersDate.Year.ToString())
                .ToDictionary(g => g.Key, g => new
                {
                    count   = g.Count(),
                    revenue = g.Sum(x => x.totalAmount)
                });

            ordersOverTime = years.Select(y => (object)new
            {
                label   = y,
                count   = grouped.ContainsKey(y) ? grouped[y].count   : 0,
                revenue = grouped.ContainsKey(y) ? grouped[y].revenue : 0.0
            }).ToList();
        }
        else if (p == "year")
        {
            var months = Enumerable.Range(0, 12)
                .Select(i => now.AddMonths(-11 + i).ToString("yyyy-MM"))
                .ToList();

            var grouped = ordersInPeriod
                .GroupBy(o => o.OrdersDate.ToString("yyyy-MM"))
                .ToDictionary(g => g.Key, g => new
                {
                    count   = g.Count(),
                    revenue = g.Sum(x => x.totalAmount)
                });

            ordersOverTime = months.Select(m => (object)new
            {
                label   = m,
                count   = grouped.ContainsKey(m) ? grouped[m].count   : 0,
                revenue = grouped.ContainsKey(m) ? grouped[m].revenue : 0.0
            }).ToList();
        }
        else if (p == "day")
        {
            var hours = Enumerable.Range(0, 24)
                .Select(i => now.AddHours(-23 + i).ToString("yyyy-MM-dd HH"))
                .ToList();

            var grouped = ordersInPeriod
                .GroupBy(o => o.OrdersDate.ToString("yyyy-MM-dd HH"))
                .ToDictionary(g => g.Key, g => new
                {
                    count   = g.Count(),
                    revenue = g.Sum(x => x.totalAmount)
                });

            ordersOverTime = hours.Select(h => (object)new
            {
                label   = h,
                count   = grouped.ContainsKey(h) ? grouped[h].count   : 0,
                revenue = grouped.ContainsKey(h) ? grouped[h].revenue : 0.0
            }).ToList();
        }
        else
        {
            var daysCount = p == "week" ? 7 : 30;
            var days = Enumerable.Range(0, daysCount)
                .Select(i => now.AddDays(-(daysCount - 1) + i).Date.ToString("yyyy-MM-dd"))
                .ToList();

            var grouped = ordersInPeriod
                .GroupBy(o => o.OrdersDate.Date.ToString("yyyy-MM-dd"))
                .ToDictionary(g => g.Key, g => new
                {
                    count   = g.Count(),
                    revenue = g.Sum(x => x.totalAmount)
                });

            ordersOverTime = days.Select(d => (object)new
            {
                label   = d,
                count   = grouped.ContainsKey(d) ? grouped[d].count   : 0,
                revenue = grouped.ContainsKey(d) ? grouped[d].revenue : 0.0
            }).ToList();
        }

        // revenueOverTime is derived from ordersOverTime — no duplication
        var revenueOverTime = ordersOverTime
            .Select(x =>
            {
                var t = x.GetType();
                return (object)new
                {
                    label   = t.GetProperty("label")!.GetValue(x),
                    revenue = t.GetProperty("revenue")!.GetValue(x)
                };
            }).ToList();

        // Orders by status (all time, for donut)
        var orderStatusCounts = allOrders
            .GroupBy(o => o.status)
            .ToDictionary(g => g.Key, g => g.Count());

        var ordersByStatus = new[]
        {
            new { statusId = 1, name = "Awaiting confirmation", count = orderStatusCounts.GetValueOrDefault(1, 0) },
            new { statusId = 2, name = "Cancelled",             count = orderStatusCounts.GetValueOrDefault(2, 0) },
            new { statusId = 3, name = "Completed",             count = orderStatusCounts.GetValueOrDefault(3, 0) },
            new { statusId = 4, name = "In progress",           count = orderStatusCounts.GetValueOrDefault(4, 0) },
            new { statusId = 5, name = "Sent",                  count = orderStatusCounts.GetValueOrDefault(5, 0) },
        };

        // Shipments

        var allShipments = await _db.shipments
            .AsNoTracking()
            .Where(s => s.fk_Companyid_Company == companyId)
            .Select(s => new { s.id_Shipment, s.shippingDate })
            .ToListAsync();

        // For "all" period the shipment window starts from the earliest shipment date
        DateTime shipmentFrom;
        if (p == "all")
        {
            var earliest = allShipments
                .Where(s => s.shippingDate.HasValue)
                .Select(s => s.shippingDate!.Value)
                .DefaultIfEmpty(now)
                .Min();
            shipmentFrom = earliest.Date;
        }
        else
        {
            shipmentFrom = from;
        }

        var shipmentsInPeriod = allShipments
            .Where(s => s.shippingDate.HasValue && s.shippingDate.Value >= shipmentFrom)
            .ToList();

        // Latest status per shipment (all shipments, for donut)
        var allShipmentStatuses = await _db.shipment_statuses
            .AsNoTracking()
            .Where(ss => _db.shipments.Any(s =>
                s.id_Shipment == ss.fk_Shipmentid_Shipment &&
                s.fk_Companyid_Company == companyId))
            .Select(ss => new
            {
                ss.fk_Shipmentid_Shipment,
                ss.fk_ShipmentStatusTypeid_ShipmentStatusType,
                ss.date
            })
            .ToListAsync();

        var latestStatusPerShipment = allShipmentStatuses
            .GroupBy(ss => ss.fk_Shipmentid_Shipment)
            .Select(g => g.OrderByDescending(x => x.date).First())
            .ToList();

        var shipmentStatusCounts = latestStatusPerShipment
            .GroupBy(ss => ss.fk_ShipmentStatusTypeid_ShipmentStatusType)
            .ToDictionary(g => g.Key, g => g.Count());

        var shipmentsByStatus = new[]
        {
            new { statusId = 1, name = "Sukurta",                  count = shipmentStatusCounts.GetValueOrDefault(1, 0) },
            new { statusId = 2, name = "Vežama",                   count = shipmentStatusCounts.GetValueOrDefault(2, 0) },
            new { statusId = 3, name = "Pristatyta",               count = shipmentStatusCounts.GetValueOrDefault(3, 0) },
            new { statusId = 4, name = "Vėluoja",                  count = shipmentStatusCounts.GetValueOrDefault(4, 0) },
            new { statusId = 5, name = "Grąžinimas sukurtas",      count = shipmentStatusCounts.GetValueOrDefault(5, 0) },
            new { statusId = 6, name = "Grąžinimas vežamas",       count = shipmentStatusCounts.GetValueOrDefault(6, 0) },
            new { statusId = 7, name = "Grąžinimas pristatytas",   count = shipmentStatusCounts.GetValueOrDefault(7, 0) },
            new { statusId = 8, name = "Grąžinimas vėluoja",       count = shipmentStatusCounts.GetValueOrDefault(8, 0) },
        };

        // Shipments over time (period window, filled buckets)
        List<object> shipmentsOverTime;

        if (p == "all")
        {
            var minYear = shipmentsInPeriod.Count > 0
                ? shipmentsInPeriod.Min(s => s.shippingDate!.Value.Year)
                : now.Year;
            var years = Enumerable.Range(minYear, now.Year - minYear + 1)
                .Select(y => y.ToString()).ToList();

            var grouped = shipmentsInPeriod
                .GroupBy(s => s.shippingDate!.Value.Year.ToString())
                .ToDictionary(g => g.Key, g => g.Count());

            shipmentsOverTime = years.Select(y => (object)new
            {
                label = y,
                count = grouped.ContainsKey(y) ? grouped[y] : 0
            }).ToList();
        }
        else if (p == "year")
        {
            var months = Enumerable.Range(0, 12)
                .Select(i => now.AddMonths(-11 + i).ToString("yyyy-MM")).ToList();

            var grouped = shipmentsInPeriod
                .GroupBy(s => s.shippingDate!.Value.ToString("yyyy-MM"))
                .ToDictionary(g => g.Key, g => g.Count());

            shipmentsOverTime = months.Select(m => (object)new
            {
                label = m,
                count = grouped.ContainsKey(m) ? grouped[m] : 0
            }).ToList();
        }
        else if (p == "day")
        {
            var hours = Enumerable.Range(0, 24)
                .Select(i => now.AddHours(-23 + i).ToString("yyyy-MM-dd HH")).ToList();

            var grouped = shipmentsInPeriod
                .GroupBy(s => s.shippingDate!.Value.ToString("yyyy-MM-dd HH"))
                .ToDictionary(g => g.Key, g => g.Count());

            shipmentsOverTime = hours.Select(h => (object)new
            {
                label = h,
                count = grouped.ContainsKey(h) ? grouped[h] : 0
            }).ToList();
        }
        else
        {
            var daysCount = p == "week" ? 7 : 30;
            var days = Enumerable.Range(0, daysCount)
                .Select(i => now.AddDays(-(daysCount - 1) + i).Date.ToString("yyyy-MM-dd")).ToList();

            var grouped = shipmentsInPeriod
                .GroupBy(s => s.shippingDate!.Value.Date.ToString("yyyy-MM-dd"))
                .ToDictionary(g => g.Key, g => g.Count());

            shipmentsOverTime = days.Select(d => (object)new
            {
                label = d,
                count = grouped.ContainsKey(d) ? grouped[d] : 0
            }).ToList();
        }

        var deliveredCount  = shipmentStatusCounts.GetValueOrDefault(3, 0);
        var lateCount       = shipmentStatusCounts.GetValueOrDefault(4, 0)
                            + shipmentStatusCounts.GetValueOrDefault(8, 0);
        var totalShipments  = allShipments.Count;

        // Courier usage (all time)
        var courierUsage = await _db.shipments
            .AsNoTracking()
            .Where(s => s.fk_Companyid_Company == companyId && s.fk_Courierid_Courier != null)
            .GroupBy(s => s.fk_Courierid_CourierNavigation!.name)
            .Select(g => new { courier = g.Key, count = g.Count() })
            .OrderByDescending(x => x.count)
            .ToListAsync();

        // Returns

        var allReturns = await _db.product_returns
            .AsNoTracking()
            .Where(r => r.fk_Companyid_Company == companyId)
            .Select(r => new { r.id_Returns, r.date, r.fk_ReturnStatusTypeid_ReturnStatusType })
            .ToListAsync();

        DateTime returnFrom;
        if (p == "all")
        {
            returnFrom = allReturns.Count > 0
                ? allReturns.Min(r => r.date.Date)
                : now.Date;
        }
        else
        {
            returnFrom = from;
        }

        var returnsInPeriod = allReturns.Where(r => r.date >= returnFrom).ToList();

        var returnStatusCounts = allReturns
            .GroupBy(r => r.fk_ReturnStatusTypeid_ReturnStatusType)
            .ToDictionary(g => g.Key, g => g.Count());

        var returnsByStatus = new[]
        {
            new { statusId = 1, name = "Sukurtas",           count = returnStatusCounts.GetValueOrDefault(1, 0) },
            new { statusId = 2, name = "Vertinamas",         count = returnStatusCounts.GetValueOrDefault(2, 0) },
            new { statusId = 3, name = "Gauta",              count = returnStatusCounts.GetValueOrDefault(3, 0) },
            new { statusId = 4, name = "Užbaigta",           count = returnStatusCounts.GetValueOrDefault(4, 0) },
            new { statusId = 5, name = "Patvirtintas",       count = returnStatusCounts.GetValueOrDefault(5, 0) },
            new { statusId = 6, name = "Atmestas",           count = returnStatusCounts.GetValueOrDefault(6, 0) },
            new { statusId = 7, name = "Etiketės paruoštos", count = returnStatusCounts.GetValueOrDefault(7, 0) },
        };

        // Returns over time (filled buckets)
        List<object> returnsOverTime;

        if (p == "all")
        {
            var minYear = returnsInPeriod.Count > 0
                ? returnsInPeriod.Min(r => r.date.Year)
                : now.Year;
            var years = Enumerable.Range(minYear, now.Year - minYear + 1)
                .Select(y => y.ToString()).ToList();

            var grouped = returnsInPeriod
                .GroupBy(r => r.date.Year.ToString())
                .ToDictionary(g => g.Key, g => g.Count());

            returnsOverTime = years.Select(y => (object)new
            {
                label = y,
                count = grouped.ContainsKey(y) ? grouped[y] : 0
            }).ToList();
        }
        else if (p == "year")
        {
            var months = Enumerable.Range(0, 12)
                .Select(i => now.AddMonths(-11 + i).ToString("yyyy-MM")).ToList();

            var grouped = returnsInPeriod
                .GroupBy(r => r.date.ToString("yyyy-MM"))
                .ToDictionary(g => g.Key, g => g.Count());

            returnsOverTime = months.Select(m => (object)new
            {
                label = m,
                count = grouped.ContainsKey(m) ? grouped[m] : 0
            }).ToList();
        }
        else if (p == "day")
        {
            var hours = Enumerable.Range(0, 24)
                .Select(i => now.AddHours(-23 + i).ToString("yyyy-MM-dd HH")).ToList();

            var grouped = returnsInPeriod
                .GroupBy(r => r.date.ToString("yyyy-MM-dd HH"))
                .ToDictionary(g => g.Key, g => g.Count());

            returnsOverTime = hours.Select(h => (object)new
            {
                label = h,
                count = grouped.ContainsKey(h) ? grouped[h] : 0
            }).ToList();
        }
        else
        {
            var daysCount = p == "week" ? 7 : 30;
            var days = Enumerable.Range(0, daysCount)
                .Select(i => now.AddDays(-(daysCount - 1) + i).Date.ToString("yyyy-MM-dd")).ToList();

            var grouped = returnsInPeriod
                .GroupBy(r => r.date.Date.ToString("yyyy-MM-dd"))
                .ToDictionary(g => g.Key, g => g.Count());

            returnsOverTime = days.Select(d => (object)new
            {
                label = d,
                count = grouped.ContainsKey(d) ? grouped[d] : 0
            }).ToList();
        }

        //  Assemble response

        return Ok(new
        {
            period       = p,
            generatedAt  = now,

            kpi = new
            {
                totalOrders,
                totalRevenue          = Math.Round(totalRevenue, 2),
                periodRevenue         = Math.Round(periodRevenue, 2),
                totalShipments,
                deliveredCount,
                lateCount,
                deliverySuccessRate   = totalShipments > 0
                    ? Math.Round((double)deliveredCount / totalShipments * 100, 1) : 0,
                totalReturns          = allReturns.Count,
                pendingReturns        = returnStatusCounts.GetValueOrDefault(1, 0)
                                      + returnStatusCounts.GetValueOrDefault(2, 0),
                newOrdersInPeriod     = ordersInPeriod.Count,
                newShipmentsInPeriod  = shipmentsInPeriod.Count,
                newReturnsInPeriod    = returnsInPeriod.Count,
            },

            orders = new
            {
                byStatus        = ordersByStatus,
                overTime        = ordersOverTime,
                revenueOverTime,
            },

            shipments = new
            {
                byStatus      = shipmentsByStatus,
                overTime      = shipmentsOverTime,
                courierUsage,
            },

            returns = new
            {
                byStatus  = returnsByStatus,
                overTime  = returnsOverTime,
            },
        });
    }
}