using RTI.OrderAccumulator.DTOs;

namespace RTI.OrderAccumulator.DTOs;

public class OrderListResponse
{
    public string Symbol { get; set; } = string.Empty;
    public DateOnly TradeDate { get; set; }
    public List<OrderDto> Orders { get; set; } = new();
}