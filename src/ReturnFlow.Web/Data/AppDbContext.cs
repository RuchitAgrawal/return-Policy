using Microsoft.EntityFrameworkCore;
using ReturnFlow.Core.Entities;

namespace ReturnFlow.Web.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLineItem> OrderLineItems => Set<OrderLineItem>();
    public DbSet<RmaTicket> RmaTickets => Set<RmaTicket>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Product ──────────────────────────────────────────────────────────
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Sku).IsUnique();
            entity.Property(e => e.Sku).IsRequired().HasMaxLength(64);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Category).IsRequired().HasMaxLength(128);
            entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
        });

        // ── Order ────────────────────────────────────────────────────────────
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.OrderNumber).IsUnique();
            entity.Property(e => e.OrderNumber).IsRequired().HasMaxLength(64);
            entity.Property(e => e.CustomerName).IsRequired().HasMaxLength(256);
            entity.Property(e => e.CustomerEmail).IsRequired().HasMaxLength(256);
            entity.Property(e => e.SalesChannel).HasConversion<string>().HasMaxLength(64);
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
        });

        // ── OrderLineItem ────────────────────────────────────────────────────
        modelBuilder.Entity<OrderLineItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UnitPricePaid).HasPrecision(18, 2);

            entity.HasOne(e => e.Order)
                  .WithMany(o => o.LineItems)
                  .HasForeignKey(e => e.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Product)
                  .WithMany(p => p.OrderLineItems)
                  .HasForeignKey(e => e.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── RmaTicket ────────────────────────────────────────────────────────
        modelBuilder.Entity<RmaTicket>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TicketNumber).IsUnique();
            entity.Property(e => e.TicketNumber).IsRequired().HasMaxLength(64);

            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(64);
            entity.Property(e => e.RequestedResolution).HasConversion<string>().HasMaxLength(64);
            entity.Property(e => e.ConditionReportedByCustomer).HasConversion<string>().HasMaxLength(64);

            entity.Property(e => e.CalculatedRefundAmount).HasPrecision(18, 2);
            entity.Property(e => e.RestockingFeeAmount).HasPrecision(18, 2);

            entity.Property(e => e.AiExtractedDefectCategory).HasConversion<string>().HasMaxLength(64);
            entity.Property(e => e.AiFraudRiskLevel).HasConversion<string>().HasMaxLength(64);
            entity.Property(e => e.AiRecommendedAction).HasConversion<string>().HasMaxLength(64);
            entity.Property(e => e.WarehouseRoutingAction).HasConversion<string>().HasMaxLength(64);

            entity.HasOne(e => e.Order)
                  .WithMany(o => o.RmaTickets)
                  .HasForeignKey(e => e.OrderId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.OrderLineItem)
                  .WithMany(li => li.RmaTickets)
                  .HasForeignKey(e => e.OrderLineItemId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
