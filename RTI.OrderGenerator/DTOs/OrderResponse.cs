namespace RTI.OrderGenerator.DTOs;

public class OrderResponse
{
    public bool Success { get; set; }

    public string Status { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string ClOrdId { get; set; } = string.Empty;
}