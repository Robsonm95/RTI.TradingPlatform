using RTI.OrderAccumulator.Models;

namespace RTI.OrderAccumulator.Repositories;

public interface IOrderRepository
{
    Task AddOrderAsync(OrderEntity order);
    Task<OrderEntity?> GetOrderByClOrdIdAsync(string clOrdId);
    Task<IEnumerable<OrderEntity>> GetOrdersBySymbolAsync(string symbol, DateOnly tradeDate);
}