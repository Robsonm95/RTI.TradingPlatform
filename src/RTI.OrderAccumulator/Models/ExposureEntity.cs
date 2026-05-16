namespace RTI.OrderAccumulator.Models;

public class ExposureEntity
{
    public int Id { get; set; }
    public DateOnly TradeDate { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public decimal Exposure { get; set; }
    public DateTime UpdatedAt { get; set; }
}
