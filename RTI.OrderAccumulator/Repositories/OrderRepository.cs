using Microsoft.EntityFrameworkCore;
using RTI.OrderAccumulator.Data;
using RTI.OrderAccumulator.Models;

namespace RTI.OrderAccumulator.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly IDbContextFactory<TradingDbContext> _dbContextFactory;

    public OrderRepository(IDbContextFactory<TradingDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task AddOrderAsync(OrderEntity order)
    {
        using var db = _dbContextFactory.CreateDbContext();
        db.Orders.Add(order);
        await db.SaveChangesAsync();
    }

    public async Task<OrderEntity?> GetOrderByClOrdIdAsync(string clOrdId)
    {
        using var db = _dbContextFactory.CreateDbContext();
        return await db.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.ClOrdId == clOrdId);
    }

    public async Task<IEnumerable<OrderEntity>> GetOrdersBySymbolAsync(string symbol, DateOnly tradeDate)
    {
        using var db = _dbContextFactory.CreateDbContext();
        return await db.Orders
            .AsNoTracking()
            .Where(o => o.Symbol == symbol && o.TradeDate == tradeDate)
            .ToListAsync();
    }
}