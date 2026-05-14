using Microsoft.EntityFrameworkCore;
using QuickFix.Fields;
using RTI.OrderAccumulator.Constants;
using RTI.OrderAccumulator.Models;
using RTI.OrderAccumulator.Repositories;
using RTI.Shared.Enums;

namespace RTI.OrderAccumulator.Services;

public class ExposureService
{
    private readonly IExposureRepository _exposureRepository;
    private readonly IOrderRepository _orderRepository;

    public ExposureService(IExposureRepository exposureRepository, IOrderRepository orderRepository)
    {
        _exposureRepository = exposureRepository;
        _orderRepository = orderRepository;
    }

    public decimal GetExposure(string symbol)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        return _exposureRepository.GetExposureAsync(symbol, today).Result;
    }

    public Dictionary<string, decimal> GetExposureAll(DateOnly? tradeDate = null)
    {
        var date = tradeDate ?? DateOnly.FromDateTime(DateTime.UtcNow.Date);
        return _exposureRepository.GetAllExposuresAsync(date).Result;
    }

    public bool CanAccept(
        string symbol,
        OrderSide side,
        decimal orderValue)
    {
        var currentExposure = GetExposure(symbol);

        var newExposure = side == OrderSide.Buy
            ? currentExposure + orderValue
            : currentExposure - orderValue;

        return Math.Abs(newExposure) <= ExposureLimits.MaxExposure;
    }

    public async Task<List<OrderEntity>> GetOrdersBySymbolAsync(string symbol, DateOnly tradeDate)
    {
        var orders = await _orderRepository.GetOrdersBySymbolAsync(symbol, tradeDate);
        return orders.ToList();
    }

    public async Task ApplyOrderAsync(
        string clOrdId,
        string symbol,
        char side,
        int quantity,
        decimal price,
        string status,
        bool accepted)
    {
        var tradeDate = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var now = DateTime.UtcNow;

        if (accepted)
        {
            var orderValue = quantity * price;
            var currentExposure = await _exposureRepository.GetExposureAsync(symbol, tradeDate);
            var updatedExposure = side == Side.BUY
                ? currentExposure + orderValue
                : currentExposure - orderValue;

            await _exposureRepository.UpdateExposureAsync(symbol, tradeDate, updatedExposure);
        }

        var orderEntity = new OrderEntity
        {
            ClOrdId = clOrdId,
            Symbol = symbol,
            Side = side.ToString(),
            Quantity = quantity,
            Price = price,
            Status = status,
            TradeDate = tradeDate,
            CreatedAt = now
        };

        await _orderRepository.AddOrderAsync(orderEntity);

        if (accepted)
        {
            var exposureValue = await _exposureRepository.GetExposureAsync(symbol, tradeDate);
            Console.WriteLine($"EXPOSURE [{symbol}] = {exposureValue:N2}");
        }
    }
}