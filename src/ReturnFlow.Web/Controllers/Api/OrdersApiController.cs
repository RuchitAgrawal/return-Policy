using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReturnFlow.Core.DTOs;
using ReturnFlow.Web.Data;

namespace ReturnFlow.Web.Controllers.Api;

[ApiController]
[Route("api/orders")]
[Produces("application/json")]
public class OrdersApiController : ControllerBase
{
    private readonly AppDbContext _context;

    public OrdersApiController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Fetches an order by its unique OrderNumber, including line items and calculated warranty badges.
    /// </summary>
    [HttpGet("{orderNumber}")]
    [ProducesResponseType(typeof(OrderSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByOrderNumber(string orderNumber)
    {
        if (string.IsNullOrWhiteSpace(orderNumber))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Order Number",
                Detail = "An order number must be provided.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var order = await _context.Orders
            .Include(o => o.LineItems)
                .ThenInclude(li => li.Product)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.OrderNumber.ToLower() == orderNumber.Trim().ToLower());

        if (order == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Order Not Found",
                Detail = $"No order exists in the system with order number '{orderNumber}'.",
                Status = StatusCodes.Status404NotFound
            });
        }

        var now = DateTime.UtcNow;
        var daysSinceDelivery = (int)(now - order.DeliveredDate).TotalDays;

        var lineItemDtos = order.LineItems.Select(li =>
        {
            bool withinReturn = daysSinceDelivery <= 30;
            bool withinWarranty = daysSinceDelivery <= li.Product.WarrantyDays;

            string badge;
            if (withinReturn)
            {
                badge = "Within Return Window";
            }
            else if (withinWarranty)
            {
                badge = "In Warranty";
            }
            else
            {
                badge = "Expired";
            }

            return new LineItemDto(
                Id: li.Id,
                ProductId: li.ProductId,
                ProductName: li.Product.Name,
                Sku: li.Product.Sku,
                QuantityPurchased: li.QuantityPurchased,
                UnitPricePaid: li.UnitPricePaid,
                WarrantyDays: li.Product.WarrantyDays,
                IsWithinReturnWindow: withinReturn,
                IsWithinWarrantyWindow: withinWarranty,
                WarrantyBadge: badge
            );
        }).ToList();

        var dto = new OrderSummaryDto(
            Id: order.Id,
            OrderNumber: order.OrderNumber,
            CustomerName: order.CustomerName,
            CustomerEmail: order.CustomerEmail,
            OrderDate: order.OrderDate,
            DeliveredDate: order.DeliveredDate,
            DaysSinceDelivery: daysSinceDelivery,
            SalesChannel: order.SalesChannel.ToString(),
            TotalAmount: order.TotalAmount,
            LineItems: lineItemDtos
        );

        return Ok(dto);
    }
}
