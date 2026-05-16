using Microsoft.EntityFrameworkCore;
using RTI.OrderAccumulator.Data;
using RTI.OrderAccumulator.Models;

namespace RTI.OrderAccumulator.Repositories;

public class ExposureRepository : IExposureRepository
{
    private readonly IDbContextFactory<TradingDbContext> _dbContextFactory;

    public ExposureRepository(IDbContextFactory<TradingDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<decimal> GetExposureAsync(string symbol, DateOnly tradeDate)
    {
        using var db = _dbContextFactory.CreateDbContext();
        var exposure = await db.Exposures
            .AsNoTracking()
            .SingleOrDefaultAsync(e => e.Symbol == symbol && e.TradeDate == tradeDate);
        return exposure?.Exposure ?? 0;
    }

    public async Task<Dictionary<string, decimal>> GetAllExposuresAsync(DateOnly tradeDate)
    {
        using var db = _dbContextFactory.CreateDbContext();
        return await db.Exposures
            .AsNoTracking()
            .Where(e => e.TradeDate == tradeDate)
            .ToDictionaryAsync(e => e.Symbol, e => e.Exposure);
    }

    public async Task UpdateExposureAsync(string symbol, DateOnly tradeDate, decimal exposure)
    {
        using var db = _dbContextFactory.CreateDbContext();
        var existing = await db.Exposures
            .SingleOrDefaultAsync(e => e.Symbol == symbol && e.TradeDate == tradeDate);
        if (existing != null)
        {
            existing.Exposure = exposure;
        }
        else
        {
            db.Exposures.Add(new ExposureEntity { Symbol = symbol, TradeDate = tradeDate, Exposure = exposure });
        }
        await db.SaveChangesAsync();
    }

    public async Task<(bool accepted, decimal newExposure)> ValidateAndUpdateExposureAsync(
        string symbol,
        DateOnly tradeDate,
        decimal orderValue,
        bool isBuy,
        decimal maxExposure)
    {
        await using var tempDb = _dbContextFactory.CreateDbContext();
        var strategy = tempDb.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var db = _dbContextFactory.CreateDbContext();
            await using var transaction = await db.Database.BeginTransactionAsync();

            try
            {
                // Lock row com SERIALIZABLE isolation para evitar race conditions
                var existing = await db.Exposures
                    .SingleOrDefaultAsync(e => e.Symbol == symbol && e.TradeDate == tradeDate);

                var currentExposure = existing?.Exposure ?? 0m;
                var newExposure = isBuy ? currentExposure + orderValue : currentExposure - orderValue;
                var accepted = Math.Abs(newExposure) <= maxExposure;

                if (accepted)
                {
                    if (existing != null)
                    {
                        existing.Exposure = newExposure;
                        existing.UpdatedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        db.Exposures.Add(new ExposureEntity
                        {
                            Symbol = symbol,
                            TradeDate = tradeDate,
                            Exposure = newExposure,
                            UpdatedAt = DateTime.UtcNow
                        });
                    }

                    await db.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return (true, newExposure);
                }
                else
                {
                    await transaction.RollbackAsync();
                    return (false, currentExposure);
                }
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
    }
}