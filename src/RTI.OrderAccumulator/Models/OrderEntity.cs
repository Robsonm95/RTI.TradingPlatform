namespace RTI.OrderAccumulator.Models;

public class OrderEntity
{
    public int Id { get; set; }
    public string ClOrdId { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public string Side { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateOnly TradeDate { get; set; }
    public DateTime CreatedAt { get; set; }
}
