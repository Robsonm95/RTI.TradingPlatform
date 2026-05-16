namespace RTI.OrderGenerator.Models;

public class FixExecutionResult
{
    public bool Success { get; set; }

    public string Status { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string ClOrdId { get; set; } = string.Empty;
}