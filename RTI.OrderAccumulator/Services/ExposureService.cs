using RTI.OrderAccumulator.Constants;
using RTI.Shared.Enums;

namespace RTI.OrderAccumulator.Services;

public class ExposureService
{
    private readonly Dictionary<string, decimal>
        _exposures = new();

    public decimal GetExposure(string symbol)
    {
        return _exposures.GetValueOrDefault(symbol, 0);
    }

    public bool CanAccept(
        string symbol,
        OrderSide side,
        decimal orderValue)
    {
        var currentExposure =
            GetExposure(symbol);

        var newExposure =
            side == OrderSide.Buy
                ? currentExposure + orderValue
                : currentExposure - orderValue;

        return Math.Abs(newExposure)
            <= ExposureLimits.MaxExposure;
    }

    public void ApplyOrder(
        string symbol,
        OrderSide side,
        decimal orderValue)
    {
        var currentExposure =
            GetExposure(symbol);

        var newExposure =
            side == OrderSide.Buy
                ? currentExposure + orderValue
                : currentExposure - orderValue;

        _exposures[symbol] = newExposure;

        Console.WriteLine(
            $"EXPOSURE [{symbol}] = {newExposure:N2}");
    }
}