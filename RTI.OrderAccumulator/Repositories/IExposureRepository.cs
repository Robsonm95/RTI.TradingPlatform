using RTI.OrderAccumulator.Models;

namespace RTI.OrderAccumulator.Repositories;

public interface IExposureRepository
{
    Task<decimal> GetExposureAsync(string symbol, DateOnly tradeDate);
    Task<Dictionary<string, decimal>> GetAllExposuresAsync(DateOnly tradeDate);
    Task UpdateExposureAsync(string symbol, DateOnly tradeDate, decimal exposure);
}