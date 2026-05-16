using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RTI.OrderAccumulator.Models;

namespace RTI.OrderAccumulator.Data;

public class TradingDbContext : DbContext
{
    public TradingDbContext(DbContextOptions<TradingDbContext> options)
        : base(options)
    {
    }

    public DbSet<ExposureEntity> Exposures => Set<ExposureEntity>();
    public DbSet<OrderEntity> Orders => Set<OrderEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var dateOnlyConverter = new ValueConverter<DateOnly, DateTime>(
            date => date.ToDateTime(TimeOnly.MinValue),
            dateTime => DateOnly.FromDateTime(dateTime));

        modelBuilder.Entity<ExposureEntity>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.TradeDate)
                .HasConversion(dateOnlyConverter)
                .HasColumnType("TEXT")
                .IsRequired();
            builder.Property(x => x.Symbol).IsRequired();
            builder.Property(x => x.Exposure).IsRequired();
            builder.Property(x => x.UpdatedAt).IsRequired();
            builder.HasIndex(x => new { x.TradeDate, x.Symbol }).IsUnique();
        });

        modelBuilder.Entity<OrderEntity>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.ClOrdId).IsRequired();
            builder.Property(x => x.Symbol).IsRequired();
            builder.Property(x => x.Side).IsRequired();
            builder.Property(x => x.Quantity).IsRequired();
            builder.Property(x => x.Price).IsRequired();
            builder.Property(x => x.Status).IsRequired();
            builder.Property(x => x.CreatedAt).IsRequired();
            builder.HasIndex(x => x.ClOrdId);
        });
    }
}
