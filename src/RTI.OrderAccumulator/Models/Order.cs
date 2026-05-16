using RTI.Shared.Enums;

namespace RTI.OrderAccumulator.Models;

public class Order
{
    public string ClOrdId { get; set; } = string.Empty;

    public string Symbol { get; set; } = string.Empty;

    public OrderSide Side { get; set; }

    public int Quantity { get; set; }

    public decimal Price { get; set; }
}