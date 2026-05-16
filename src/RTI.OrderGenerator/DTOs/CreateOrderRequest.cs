using RTI.Shared.Enums;

namespace RTI.OrderGenerator.DTOs;

public class CreateOrderRequest
{
    public string Symbol { get; set; } = string.Empty;

    public OrderSide Side { get; set; }

    public int Quantity { get; set; }

    public decimal Price { get; set; }
}