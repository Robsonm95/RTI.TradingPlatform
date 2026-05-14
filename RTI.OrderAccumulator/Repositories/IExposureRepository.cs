using RTI.OrderAccumulator.Models;

namespace RTI.OrderAccumulator.Repositories;

public interface IExposureRepository
{
    Task<decimal> GetExposureAsync(string symbol, DateOnly tradeDate);
    Task<Dictionary<string, decimal>> GetAllExposuresAsync(DateOnly tradeDate);
    Task UpdateExposureAsync(string symbol, DateOnly tradeDate, decimal exposure);
    
    /// <summary>
    /// Valida e atualiza exposição em uma transação atômica.
    /// Previne race conditions verificando o limite dentro da mesma transação.
    /// </summary>
    Task<(bool accepted, decimal newExposure)> ValidateAndUpdateExposureAsync(
        string symbol,
        DateOnly tradeDate,
        decimal orderValue,
        bool isBuy,
        decimal maxExposure);
}