using ReturnFlow.Core.Enums;

namespace ReturnFlow.Core.Entities;

public class Order
{
    public int Id { get; set; }

    /// <summary>Unique order number, e.g. "ORD-2026-1001"</summary>
    public string OrderNumber { get; set; } = string.Empty;

    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;

    public DateTime OrderDate { get; set; }

    /// <summary>Date the order was physically delivered to the customer</summary>
    public DateTime DeliveredDate { get; set; }

    public SalesChannel SalesChannel { get; set; }

    /// <summary>Total order value in INR (₹)</summary>
    public decimal TotalAmount { get; set; }

    // Navigation
    public ICollection<OrderLineItem> LineItems { get; set; } = new List<OrderLineItem>();
    public ICollection<RmaTicket> RmaTickets { get; set; } = new List<RmaTicket>();
}

