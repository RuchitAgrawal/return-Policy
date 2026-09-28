using ReturnFlow.Core.Enums;

namespace ReturnFlow.Core.Entities;

public class Product
{
    public int Id { get; set; }

    /// <summary>Unique SKU identifier, e.g. "SKU-MECH-KB01"</summary>
    public string Sku { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;

    /// <summary>Unit price in INR (₹), precision decimal(18,2)</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>Number of days this product is covered under warranty</summary>
    public int WarrantyDays { get; set; }

    public int CurrentStockOnHand { get; set; }
    public int DamagedStockCount { get; set; }

    // Navigation
    public ICollection<OrderLineItem> OrderLineItems { get; set; } = new List<OrderLineItem>();
}

