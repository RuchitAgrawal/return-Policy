using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReturnFlow.Core.Common;
using ReturnFlow.Core.DTOs;
using ReturnFlow.Core.Entities;
using ReturnFlow.Core.Enums;
using ReturnFlow.Core.Interfaces;
using ReturnFlow.Core.Services;
using ReturnFlow.Web.Data;

namespace ReturnFlow.Web.Controllers.Api;

[ApiController]
[Route("api/rma")]
[Produces("application/json")]
public class RmaApiController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IAiTriageService _aiTriageService;
    private readonly ILogger<RmaApiController> _logger;

    public RmaApiController(
        AppDbContext context,
        IAiTriageService aiTriageService,
        ILogger<RmaApiController> logger)
    {
        _context = context;
        _aiTriageService = aiTriageService;
        _logger = logger;
    }

    /// <summary>
    /// Evaluates an RMA request through SQL policy rules and the AI Triage engine,
    /// generating and persisting a new RmaTicket.
    /// </summary>
    [HttpPost("analyze")]
    [ProducesResponseType(typeof(RmaTicketDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AnalyzeAndSubmit([FromBody] RmaSubmitDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.OrderNumber))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Missing RMA Details",
                Detail = "A valid order number and submission payload are required.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var order = await _context.Orders
            .Include(o => o.LineItems)
                .ThenInclude(li => li.Product)
            .FirstOrDefaultAsync(o => o.OrderNumber.ToLower() == dto.OrderNumber.Trim().ToLower());

        if (order == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Order Not Found",
                Detail = $"Order '{dto.OrderNumber}' does not exist.",
                Status = StatusCodes.Status404NotFound
            });
        }

        var lineItem = order.LineItems.FirstOrDefault(li => li.Id == dto.OrderLineItemId);
        if (lineItem == null)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Line Item Not Found",
                Detail = $"Line item ID {dto.OrderLineItemId} does not belong to order '{dto.OrderNumber}'.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var product = lineItem.Product;

        // Step 1: Run deterministic C# policy engine
        var policyResult = RmaPolicyEngine.Evaluate(
            order,
            lineItem,
            product,
            dto.ConditionReportedByCustomer
        );

        // Step 2: Assemble SQL grounding context for AI
        var aiContext = new AiTriageContext(
            OrderNumber: order.OrderNumber,
            SalesChannel: order.SalesChannel.ToString(),
            ProductName: product.Name,
            Sku: product.Sku,
            UnitPrice: lineItem.UnitPricePaid,
            OrderDate: order.OrderDate,
            DeliveredDate: order.DeliveredDate,
            DaysSinceDelivery: policyResult.DaysSinceDelivery,
            IsWithinReturnWindow: policyResult.IsWithinReturnWindow,
            IsWithinWarrantyWindow: policyResult.IsWithinWarrantyWindow,
            WarrantyDays: product.WarrantyDays,
            ConditionReportedByCustomer: dto.ConditionReportedByCustomer.ToString(),
            RequestedResolution: dto.RequestedResolution.ToString()
        );

        // Step 3: Run AI Triage Engine
        var aiResult = await _aiTriageService.AnalyzeAsync(dto.CustomerComplaintRawText, aiContext);

        // Step 4: Map status based on combined deterministic policy + AI scoring
        RmaStatus initialStatus;
        if (aiResult.FraudRiskLevel.Equals("High", StringComparison.OrdinalIgnoreCase))
        {
            initialStatus = RmaStatus.QuarantinedForFraud;
        }
        else if (aiResult.RecommendedAction.Equals("RejectOutOfPolicy", StringComparison.OrdinalIgnoreCase))
        {
            initialStatus = RmaStatus.Rejected;
        }
        else if (aiResult.RecommendedAction.Equals("ApproveFullRefund", StringComparison.OrdinalIgnoreCase) &&
                 dto.ConditionReportedByCustomer == ConditionReported.Unopened &&
                 policyResult.IsWithinReturnWindow)
        {
            initialStatus = RmaStatus.AutoApproved;
        }
        else
        {
            initialStatus = RmaStatus.PendingReview;
        }

        // Adjust refund values if AI flagged remorse with restocking fee
        decimal refundAmount = policyResult.CalculatedRefundAmount;
        decimal restockingFee = policyResult.RestockingFeeAmount;

        if (aiResult.RecommendedAction.Equals("ApproveWithRestockingFee", StringComparison.OrdinalIgnoreCase) &&
            restockingFee == 0m && policyResult.IsWithinReturnWindow)
        {
            decimal feePercent = order.SalesChannel == SalesChannel.B2BWholesale ? 0.20m : 0.15m;
            decimal totalPaid = lineItem.UnitPricePaid * lineItem.QuantityPurchased;
            restockingFee = Math.Round(totalPaid * feePercent, 2);
            refundAmount = Math.Max(0m, totalPaid - restockingFee);
        }

        // Create RmaTicket entity
        var ticket = new RmaTicket
        {
            TicketNumber = TicketNumberGenerator.Generate(),
            OrderId = order.Id,
            OrderLineItemId = lineItem.Id,
            CreatedAt = DateTime.UtcNow,
            Status = initialStatus,
            CustomerComplaintRawText = dto.CustomerComplaintRawText,
            RequestedResolution = dto.RequestedResolution,
            ConditionReportedByCustomer = dto.ConditionReportedByCustomer,
            DaysSinceDelivery = policyResult.DaysSinceDelivery,
            IsWithinReturnWindow = policyResult.IsWithinReturnWindow,
            IsWithinWarrantyWindow = policyResult.IsWithinWarrantyWindow,
            CalculatedRefundAmount = refundAmount,
            RestockingFeeAmount = restockingFee,
            AiExtractedDefectCategory = aiResult.ParsedDefectCategory,
            AiSentimentScore = aiResult.SentimentScore,
            AiFraudRiskLevel = aiResult.ParsedFraudRiskLevel,
            AiRecommendedAction = aiResult.ParsedRecommendedAction,
            AiReasoningSummary = aiResult.ReasoningSummary,
            AiDraftedCustomerEmail = aiResult.DraftedCustomerEmail,
            WarehouseRoutingAction = aiResult.ParsedWarehouseRoutingAction
        };

        // If auto-approved, sync inventory immediately
        if (initialStatus == RmaStatus.AutoApproved && ticket.WarehouseRoutingAction.HasValue)
        {
            RmaPolicyEngine.ApplyInventorySync(product, ticket.WarehouseRoutingAction.Value, lineItem.QuantityPurchased);
        }

        _context.RmaTickets.Add(ticket);
        await _context.SaveChangesAsync();

        var detailDto = MapToDetailDto(ticket, order, lineItem, product);
        return CreatedAtAction(nameof(GetById), new { id = ticket.Id }, detailDto);
    }

    /// <summary>
    /// Returns a list of RMA tickets with optional filtering by status, fraudRisk, or channel.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<RmaListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status,
        [FromQuery] string? fraudRisk,
        [FromQuery] string? channel)
    {
        var query = _context.RmaTickets
            .Include(t => t.Order)
            .Include(t => t.OrderLineItem)
                .ThenInclude(li => li.Product)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && !status.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            if (Enum.TryParse<RmaStatus>(status, true, out var st))
            {
                query = query.Where(t => t.Status == st);
            }
        }

        if (!string.IsNullOrWhiteSpace(fraudRisk) && !fraudRisk.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            if (Enum.TryParse<FraudRiskLevel>(fraudRisk, true, out var fr))
            {
                query = query.Where(t => t.AiFraudRiskLevel == fr);
            }
        }

        if (!string.IsNullOrWhiteSpace(channel) && !channel.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            if (Enum.TryParse<SalesChannel>(channel, true, out var ch))
            {
                query = query.Where(t => t.Order.SalesChannel == ch);
            }
        }

        var tickets = await query
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new RmaListItemDto(
                t.Id,
                t.TicketNumber,
                t.Order.CustomerName,
                t.OrderLineItem.Product.Name,
                t.OrderLineItem.Product.Sku,
                t.Order.SalesChannel.ToString(),
                t.DaysSinceDelivery,
                t.Status.ToString(),
                t.AiFraudRiskLevel.HasValue ? t.AiFraudRiskLevel.Value.ToString() : null,
                t.AiRecommendedAction.HasValue ? t.AiRecommendedAction.Value.ToString() : null,
                t.AiExtractedDefectCategory.HasValue ? t.AiExtractedDefectCategory.Value.ToString() : null,
                t.CalculatedRefundAmount,
                t.CreatedAt
            ))
            .ToListAsync();

        return Ok(tickets);
    }

    /// <summary>
    /// Returns the full details of a specific RMA ticket for the inspector slide-over panel.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(RmaTicketDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var ticket = await _context.RmaTickets
            .Include(t => t.Order)
            .Include(t => t.OrderLineItem)
                .ThenInclude(li => li.Product)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Ticket Not Found",
                Detail = $"No RMA ticket with ID {id} was found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        var detail = MapToDetailDto(ticket, ticket.Order, ticket.OrderLineItem, ticket.OrderLineItem.Product);
        return Ok(detail);
    }

    /// <summary>
    /// Operations staff action endpoint: approve with inventory sync, apply restocking fee, or reject.
    /// </summary>
    [HttpPatch("{id:int}/decision")]
    [ProducesResponseType(typeof(RmaDecisionResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ProcessDecision(int id, [FromBody] RmaDecisionDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Action))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Decision Payload",
                Detail = "A valid Action ('Approve', 'ApplyRestockingFee', or 'Reject') must be provided.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var ticket = await _context.RmaTickets
            .Include(t => t.Order)
            .Include(t => t.OrderLineItem)
                .ThenInclude(li => li.Product)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Ticket Not Found",
                Detail = $"No RMA ticket with ID {id} was found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        var product = ticket.OrderLineItem.Product;
        string action = dto.Action.Trim().ToLowerInvariant();
        string stockMsg = string.Empty;

        switch (action)
        {
            case "approve":
                ticket.Status = RmaStatus.StaffApproved;
                var routing = ticket.WarehouseRoutingAction ?? WarehouseRoutingAction.RestockToSellable;
                RmaPolicyEngine.ApplyInventorySync(product, routing, ticket.OrderLineItem.QuantityPurchased);

                stockMsg = routing switch
                {
                    WarehouseRoutingAction.RestockToSellable =>
                        $"Ticket {ticket.TicketNumber} Approved. {product.Sku} Sellable Stock incremented to {product.CurrentStockOnHand}.",
                    WarehouseRoutingAction.SendToRefurbish =>
                        $"Ticket {ticket.TicketNumber} Approved. {product.Sku} routed to Refurbish. Damaged Stock incremented to {product.DamagedStockCount}.",
                    WarehouseRoutingAction.ScrapDefect =>
                        $"Ticket {ticket.TicketNumber} Approved. {product.Sku} marked for Scrap. Damaged Stock incremented to {product.DamagedStockCount}.",
                    _ =>
                        $"Ticket {ticket.TicketNumber} Approved. Item held in quarantine."
                };
                break;

            case "applyrestockingfee":
                ticket.Status = RmaStatus.StaffApproved;
                decimal feePercent = ticket.Order.SalesChannel == SalesChannel.B2BWholesale ? 0.20m : 0.15m;
                decimal totalCost = ticket.OrderLineItem.UnitPricePaid * ticket.OrderLineItem.QuantityPurchased;
                ticket.RestockingFeeAmount = Math.Round(totalCost * feePercent, 2);
                ticket.CalculatedRefundAmount = Math.Max(0m, totalCost - ticket.RestockingFeeAmount);

                var feeRouting = ticket.WarehouseRoutingAction ?? WarehouseRoutingAction.RestockToSellable;
                RmaPolicyEngine.ApplyInventorySync(product, feeRouting, ticket.OrderLineItem.QuantityPurchased);

                stockMsg = $"Ticket {ticket.TicketNumber} approved with {feePercent * 100:0}% restocking fee (₹{ticket.RestockingFeeAmount:N2}). {product.Sku} Stock updated.";
                break;

            case "reject":
                ticket.Status = RmaStatus.Rejected;
                ticket.CalculatedRefundAmount = 0m;
                stockMsg = $"Ticket {ticket.TicketNumber} Rejected. No inventory changes executed.";
                break;

            default:
                return BadRequest(new ProblemDetails
                {
                    Title = "Unsupported Decision Action",
                    Detail = $"The action '{dto.Action}' is not supported. Use 'Approve', 'ApplyRestockingFee', or 'Reject'.",
                    Status = StatusCodes.Status400BadRequest
                });
        }

        await _context.SaveChangesAsync();

        var result = new RmaDecisionResultDto(
            TicketNumber: ticket.TicketNumber,
            NewStatus: ticket.Status.ToString(),
            Message: $"RMA decision '{dto.Action}' successfully recorded.",
            StockUpdateMessage: stockMsg,
            UpdatedStockOnHand: product.CurrentStockOnHand,
            UpdatedDamagedStock: product.DamagedStockCount
        );

        return Ok(result);
    }

    private static RmaTicketDetailDto MapToDetailDto(
        RmaTicket ticket,
        Order order,
        OrderLineItem lineItem,
        Product product)
    {
        return new RmaTicketDetailDto(
            Id: ticket.Id,
            TicketNumber: ticket.TicketNumber,
            OrderNumber: order.OrderNumber,
            CustomerName: order.CustomerName,
            CustomerEmail: order.CustomerEmail,
            ProductName: product.Name,
            Sku: product.Sku,
            UnitPricePaid: lineItem.UnitPricePaid,
            QuantityPurchased: lineItem.QuantityPurchased,
            SalesChannel: order.SalesChannel.ToString(),
            DeliveredDate: order.DeliveredDate,
            DaysSinceDelivery: ticket.DaysSinceDelivery,
            IsWithinReturnWindow: ticket.IsWithinReturnWindow,
            IsWithinWarrantyWindow: ticket.IsWithinWarrantyWindow,
            CustomerComplaintRawText: ticket.CustomerComplaintRawText,
            RequestedResolution: ticket.RequestedResolution.ToString(),
            ConditionReportedByCustomer: ticket.ConditionReportedByCustomer.ToString(),
            Status: ticket.Status.ToString(),
            CalculatedRefundAmount: ticket.CalculatedRefundAmount,
            RestockingFeeAmount: ticket.RestockingFeeAmount,
            AiExtractedDefectCategory: ticket.AiExtractedDefectCategory?.ToString(),
            AiSentimentScore: ticket.AiSentimentScore,
            AiFraudRiskLevel: ticket.AiFraudRiskLevel?.ToString(),
            AiRecommendedAction: ticket.AiRecommendedAction?.ToString(),
            AiReasoningSummary: ticket.AiReasoningSummary,
            AiDraftedCustomerEmail: ticket.AiDraftedCustomerEmail,
            WarehouseRoutingAction: ticket.WarehouseRoutingAction?.ToString(),
            CreatedAt: ticket.CreatedAt
        );
    }
}
