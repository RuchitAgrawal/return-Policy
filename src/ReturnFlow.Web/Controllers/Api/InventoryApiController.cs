using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReturnFlow.Core.DTOs;
using ReturnFlow.Core.Enums;
using ReturnFlow.Web.Data;

namespace ReturnFlow.Web.Controllers.Api;

[ApiController]
[Route("api/inventory")]
[Produces("application/json")]
public class InventoryApiController : ControllerBase
{
    private readonly AppDbContext _context;

    public InventoryApiController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Returns live inventory levels and return rate statistics across all seeded products.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<InventorySummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInventoryLedger()
    {
        var products = await _context.Products
            .Include(p => p.OrderLineItems)
                .ThenInclude(li => li.RmaTickets)
            .AsNoTracking()
            .ToListAsync();

        var dtos = products.Select(p =>
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

        return Ok(dtos);
    }

    /// <summary>
    /// Returns KPI summary ribbon metrics for the warehouse operations dashboard.
    /// </summary>
    [HttpGet("kpis")]
    [ProducesResponseType(typeof(DashboardKpiDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetKpis()
    {
        var tickets = await _context.RmaTickets.AsNoTracking().ToListAsync();
        var products = await _context.Products.AsNoTracking().ToListAsync();

        int totalActive = tickets.Count(t => t.Status is RmaStatus.PendingReview or RmaStatus.QuarantinedForFraud);
        decimal valueAtRisk = tickets
            .Where(t => t.Status is RmaStatus.PendingReview or RmaStatus.QuarantinedForFraud)
            .Sum(t => t.CalculatedRefundAmount);

        decimal autoResolvedPct = tickets.Count > 0
            ? Math.Round(((decimal)tickets.Count(t => t.Status == RmaStatus.AutoApproved) / tickets.Count) * 100m, 1)
            : 0m;

        int highFraudCount = tickets.Count(t => t.AiFraudRiskLevel == FraudRiskLevel.High);
        int totalStockOnHand = products.Sum(p => p.CurrentStockOnHand);

        var kpi = new DashboardKpiDto(
            TotalActiveRmas: totalActive,
            TotalValueAtRisk: valueAtRisk,
            AutoResolvedPercent: autoResolvedPct,
            HighFraudAlerts: highFraudCount,
            StockRecovered: totalStockOnHand
        );

        return Ok(kpi);
    }
}
