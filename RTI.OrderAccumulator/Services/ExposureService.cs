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

    public async Task<decimal> GetExposureAsync(string symbol)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        return await _exposureRepository.GetExposureAsync(symbol, today);
    }

    public async Task<Dictionary<string, decimal>> GetExposureAllAsync(DateOnly? tradeDate = null)
    {
        var date = tradeDate ?? DateOnly.FromDateTime(DateTime.UtcNow.Date);
        return await _exposureRepository.GetAllExposuresAsync(date);
    }

    public async Task<bool> CanAcceptAsync(
        string symbol,
        OrderSide side,
        decimal orderValue)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var (accepted, _) = await _exposureRepository.ValidateAndUpdateExposureAsync(
            symbol,
            today,
            orderValue,
            side == OrderSide.Buy,
            ExposureLimits.MaxExposure);
        return accepted;
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
        string status)
    {
        var tradeDate = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var now = DateTime.UtcNow;

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

        if (status == "ACCEPTED")
        {
            var exposureValue = await _exposureRepository.GetExposureAsync(symbol, tradeDate);
            Console.WriteLine($"EXPOSURE [{symbol}] = {exposureValue:N2}");
        }
    }
}