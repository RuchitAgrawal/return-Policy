using ReturnFlow.Core.Entities;
using ReturnFlow.Core.Enums;
using ReturnFlow.Core.Services;
using Xunit;

namespace ReturnFlow.UnitTests;

public class RmaPolicyEngineTests
{
    private readonly Product _keyboardProduct = new()
    {
        Id = 1,
        Sku = "SKU-MECH-KB01",
        Name = "Keychron Pro Wireless Mechanical Keyboard",
        UnitPrice = 9500.00m,
        WarrantyDays = 365,
        CurrentStockOnHand = 40,
        DamagedStockCount = 0
    };

    [Fact]
    public void Evaluate_Delivered29DaysAgo_IsWithinReturnWindow()
    {
        var order = new Order
        {
            Id = 1,
            OrderNumber = "ORD-TEST-01",
            DeliveredDate = DateTime.UtcNow.AddDays(-29),
            SalesChannel = SalesChannel.Shopify
        };
        var lineItem = new OrderLineItem
        {
            Id = 10,
            OrderId = 1,
            ProductId = _keyboardProduct.Id,
            Product = _keyboardProduct,
            QuantityPurchased = 1,
            UnitPricePaid = 9500.00m
        };

        var result = RmaPolicyEngine.Evaluate(order, lineItem, _keyboardProduct, ConditionReported.Unopened);

        Assert.True(result.IsWithinReturnWindow);
        Assert.Equal(9500.00m, result.CalculatedRefundAmount);
        Assert.Equal(0.00m, result.RestockingFeeAmount);
    }

    [Fact]
    public void Evaluate_Delivered30DaysAgo_BoundaryIsWithinReturnWindow()
    {
        var order = new Order
        {
            Id = 1,
            OrderNumber = "ORD-TEST-02",
            DeliveredDate = DateTime.UtcNow.AddDays(-30),
            SalesChannel = SalesChannel.Shopify
        };
        var lineItem = new OrderLineItem
        {
            Id = 10,
            OrderId = 1,
            ProductId = _keyboardProduct.Id,
            Product = _keyboardProduct,
            QuantityPurchased = 1,
            UnitPricePaid = 9500.00m
        };

        var result = RmaPolicyEngine.Evaluate(order, lineItem, _keyboardProduct, ConditionReported.Unopened);

        Assert.True(result.IsWithinReturnWindow);
    }

    [Fact]
    public void Evaluate_Delivered31DaysAgo_IsOutsideReturnWindow()
    {
        var order = new Order
        {
            Id = 1,
            OrderNumber = "ORD-TEST-03",
            DeliveredDate = DateTime.UtcNow.AddDays(-31),
            SalesChannel = SalesChannel.Shopify
        };
        var lineItem = new OrderLineItem
        {
            Id = 10,
            OrderId = 1,
            ProductId = _keyboardProduct.Id,
            Product = _keyboardProduct,
            QuantityPurchased = 1,
            UnitPricePaid = 9500.00m
        };

        var result = RmaPolicyEngine.Evaluate(order, lineItem, _keyboardProduct, ConditionReported.OpenedUnused);

        Assert.False(result.IsWithinReturnWindow);
        Assert.True(result.IsWithinWarrantyWindow);
    }

    [Fact]
    public void Evaluate_OpenedUnused_WithinReturnWindow_Applies15PercentRestockingFee()
    {
        var order = new Order
        {
            Id = 1,
            OrderNumber = "ORD-TEST-04",
            DeliveredDate = DateTime.UtcNow.AddDays(-10),
            SalesChannel = SalesChannel.Shopify
        };
        var lineItem = new OrderLineItem
        {
            Id = 10,
            OrderId = 1,
            ProductId = _keyboardProduct.Id,
            Product = _keyboardProduct,
            QuantityPurchased = 1,
            UnitPricePaid = 9500.00m
        };

        var result = RmaPolicyEngine.Evaluate(order, lineItem, _keyboardProduct, ConditionReported.OpenedUnused);

        Assert.True(result.IsWithinReturnWindow);
        Assert.Equal(1425.00m, result.RestockingFeeAmount); // 15% of 9500
        Assert.Equal(8075.00m, result.CalculatedRefundAmount); // 9500 - 1425
    }

    [Fact]
    public void Evaluate_B2BWholesale_OpenedUnused_Applies20PercentRestockingFee()
    {
        var order = new Order
        {
            Id = 1,
            OrderNumber = "ORD-TEST-05",
            DeliveredDate = DateTime.UtcNow.AddDays(-15),
            SalesChannel = SalesChannel.B2BWholesale
        };
        var lineItem = new OrderLineItem
        {
            Id = 10,
            OrderId = 1,
            ProductId = _keyboardProduct.Id,
            Product = _keyboardProduct,
            QuantityPurchased = 2,
            UnitPricePaid = 9500.00m
        };

        var result = RmaPolicyEngine.Evaluate(order, lineItem, _keyboardProduct, ConditionReported.OpenedUnused);

        // Gross = 19,000, 20% = 3,800, Net = 15,200
        Assert.Equal(3800.00m, result.RestockingFeeAmount);
        Assert.Equal(15200.00m, result.CalculatedRefundAmount);
    }

    [Fact]
    public void Evaluate_Defective_WithinReturnWindow_ZeroRestockingFee()
    {
        var order = new Order
        {
            Id = 1,
            OrderNumber = "ORD-TEST-06",
            DeliveredDate = DateTime.UtcNow.AddDays(-12),
            SalesChannel = SalesChannel.Shopify
        };
        var lineItem = new OrderLineItem
        {
            Id = 10,
            OrderId = 1,
            ProductId = _keyboardProduct.Id,
            Product = _keyboardProduct,
            QuantityPurchased = 1,
            UnitPricePaid = 9500.00m
        };

        var result = RmaPolicyEngine.Evaluate(order, lineItem, _keyboardProduct, ConditionReported.Defective);

        Assert.Equal(0.00m, result.RestockingFeeAmount);
        Assert.Equal(9500.00m, result.CalculatedRefundAmount);
    }

    [Fact]
    public void Evaluate_WithinWarranty_HardwareDefect_ZeroRestockingFee()
    {
        var order = new Order
        {
            Id = 1,
            OrderNumber = "ORD-TEST-07",
            DeliveredDate = DateTime.UtcNow.AddDays(-90),
            SalesChannel = SalesChannel.Shopify
        };
        var lineItem = new OrderLineItem
        {
            Id = 10,
            OrderId = 1,
            ProductId = _keyboardProduct.Id,
            Product = _keyboardProduct,
            QuantityPurchased = 1,
            UnitPricePaid = 9500.00m
        };

        var result = RmaPolicyEngine.Evaluate(
            order,
            lineItem,
            _keyboardProduct,
            ConditionReported.Defective,
            DefectCategory.HardwareDefect);

        Assert.True(result.IsWithinWarrantyWindow);
        Assert.False(result.IsWithinReturnWindow);
        Assert.Equal(0.00m, result.RestockingFeeAmount);
    }

    [Fact]
    public void Evaluate_OutsideWarrantyAndReturnWindow_Rejected()
    {
        var order = new Order
        {
            Id = 1,
            OrderNumber = "ORD-TEST-08",
            DeliveredDate = DateTime.UtcNow.AddDays(-400),
            SalesChannel = SalesChannel.Shopify
        };
        var lineItem = new OrderLineItem
        {
            Id = 10,
            OrderId = 1,
            ProductId = _keyboardProduct.Id,
            Product = _keyboardProduct,
            QuantityPurchased = 1,
            UnitPricePaid = 9500.00m
        };

        var result = RmaPolicyEngine.Evaluate(order, lineItem, _keyboardProduct, ConditionReported.Defective);

        Assert.False(result.IsWithinReturnWindow);
        Assert.False(result.IsWithinWarrantyWindow);
        Assert.Equal(0.00m, result.CalculatedRefundAmount);
    }
}
