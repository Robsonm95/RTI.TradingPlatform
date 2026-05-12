namespace RTI.OrderAccumulator.DTOs;

public class ExposureResponse
{
    public Dictionary<string, decimal> Exposures { get; set; } = new();

    public decimal MaxLimit { get; set; } = 100_000_000m;
}
