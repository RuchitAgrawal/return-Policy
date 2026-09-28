namespace ReturnFlow.Core.Entities;

public class OrderLineItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int QuantityPurchased { get; set; }

    /// <summary>Price per unit actually paid (may differ from Product.UnitPrice due to discounts)</summary>
    public decimal UnitPricePaid { get; set; }

    // Navigation - an RMA may target a specific line item
    public ICollection<RmaTicket> RmaTickets { get; set; } = new List<RmaTicket>();
}

