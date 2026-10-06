using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReturnFlow.Core.DTOs;
using ReturnFlow.Core.Enums;
using ReturnFlow.Web.Data;

namespace ReturnFlow.Web.Controllers.Mvc;

[Route("Staff")]
public class StaffController : Controller
{
    private readonly AppDbContext _context;

    public StaffController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("")]
    [HttpGet("Dashboard")]
    public IActionResult Dashboard()
    {
        return View();
    }

    [HttpGet("Inventory")]
    public async Task<IActionResult> Inventory()
    {
        var products = await _context.Products
            .Include(p => p.OrderLineItems)
                .ThenInclude(li => li.RmaTickets)
            .AsNoTracking()
            .ToListAsync();

        var model = products.Select(p =>
        {
            int totalUnitsSold = p.OrderLineItems.Sum(li => li.QuantityPurchased);
            var rmas = p.OrderLineItems.SelectMany(li => li.RmaTickets).ToList();
            int totalRmas = rmas.Count;
            int approvedRmas = rmas.Count(t => t.Status is RmaStatus.AutoApproved or RmaStatus.StaffApproved);

            decimal returnRate = totalUnitsSold > 0
                ? Math.Round(((decimal)approvedRmas / totalUnitsSold) * 100m, 1)
                : 0m;

            return new InventorySummaryDto(
                ProductId: p.Id,
                Sku: p.Sku,
                Name: p.Name,
                Category: p.Category,
                UnitPrice: p.UnitPrice,
                WarrantyDays: p.WarrantyDays,
                CurrentStockOnHand: p.CurrentStockOnHand,
                DamagedStockCount: p.DamagedStockCount,
                TotalRmaCount: totalRmas,
                ApprovedRmaCount: approvedRmas,
                ReturnRatePercent: returnRate
            );
        }).ToList();

        return View(model);
    }
}
