using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RTI.OrderAccumulator.Data;
using RTI.OrderAccumulator.Models;
using RTI.OrderAccumulator.Repositories;
using Xunit;

namespace RTI.OrderAccumulator.Tests;

public class ExposureRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<TradingDbContext> _options;
    private readonly ExposureRepository _repository;

    public ExposureRepositoryTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<TradingDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = new TradingDbContext(_options);
        context.Database.EnsureCreated();

        _repository = new ExposureRepository(new TestDbContextFactory(_options));
    }

    [Fact]
    public async Task UpdateExposureAsync_InsertsNewExposureAndUpdatesExistingExposure()
    {
        var tradeDate = new DateOnly(2026, 5, 14);

        await _repository.UpdateExposureAsync("AAPL", tradeDate, 100m);
        var firstExposure = await _repository.GetExposureAsync("AAPL", tradeDate);

        Assert.Equal(100m, firstExposure);

        await _repository.UpdateExposureAsync("AAPL", tradeDate, 75m);
        var updatedExposure = await _repository.GetExposureAsync("AAPL", tradeDate);

        Assert.Equal(75m, updatedExposure);
    }

    [Fact]
    public async Task ValidateAndUpdateExposureAsync_AcceptsWhenWithinLimit()
    {
        var tradeDate = new DateOnly(2026, 5, 14);

        var (accepted, newExposure) = await _repository.ValidateAndUpdateExposureAsync(
            "AAPL",
            tradeDate,
            50m,
            true,
            100m);

        Assert.True(accepted);
        Assert.Equal(50m, newExposure);
        Assert.Equal(50m, await _repository.GetExposureAsync("AAPL", tradeDate));
    }

    [Fact]
    public async Task ValidateAndUpdateExposureAsync_RejectsWhenLimitExceededAndLeavesExposureUnchanged()
    {
        var tradeDate = new DateOnly(2026, 5, 14);

        var (accepted, newExposure) = await _repository.ValidateAndUpdateExposureAsync(
            "AAPL",
            tradeDate,
            150m,
            true,
            100m);

        Assert.False(accepted);
        Assert.Equal(0m, newExposure);
        Assert.Equal(0m, await _repository.GetExposureAsync("AAPL", tradeDate));
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    private sealed class TestDbContextFactory : IDbContextFactory<TradingDbContext>
    {
        private readonly DbContextOptions<TradingDbContext> _options;

        public TestDbContextFactory(DbContextOptions<TradingDbContext> options)
        {
            _options = options;
        }

        public TradingDbContext CreateDbContext()
        {
            return new TradingDbContext(_options);
        }
    }
}
