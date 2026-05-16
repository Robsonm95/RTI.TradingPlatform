using Microsoft.Extensions.Logging;
using Moq;
using QuickFix.Fields;
using RTI.OrderAccumulator.Models;
using RTI.OrderAccumulator.Repositories;
using RTI.OrderAccumulator.Services;
using RTI.Shared.Enums;

namespace RTI.OrderAccumulator.Tests;

public class ExposureServiceTests
{
    private readonly Mock<IExposureRepository> _exposureRepository = new();
    private readonly Mock<IOrderRepository> _orderRepository = new();
    private readonly Mock<ILogger<ExposureService>> _logger = new();
    private readonly ExposureService _service;

    public ExposureServiceTests()
    {
        _service = new ExposureService(
            _exposureRepository.Object,
            _orderRepository.Object,
            _logger.Object);
    }

    [Fact]
    public async Task GetExposureAsync_ReturnsValueFromRepository()
    {
        const decimal expectedExposure = 42m;
        _exposureRepository
            .Setup(r => r.GetExposureAsync("AAPL", It.IsAny<DateOnly>()))
            .ReturnsAsync(expectedExposure);

        var actual = await _service.GetExposureAsync("AAPL");

        Assert.Equal(expectedExposure, actual);
    }

    [Fact]
    public async Task GetExposureAllAsync_CallsRepositoryWithProvidedDate()
    {
        var date = new DateOnly(2026, 5, 14);
        var expected = new Dictionary<string, decimal> { ["AAPL"] = 10m };

        _exposureRepository
            .Setup(r => r.GetAllExposuresAsync(date))
            .ReturnsAsync(expected);

        var actual = await _service.GetExposureAllAsync(date);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task CanAcceptAsync_ReturnsTrueWhenRepositoryAccepts()
    {
        _exposureRepository
            .Setup(r => r.ValidateAndUpdateExposureAsync("AAPL", It.IsAny<DateOnly>(), 150m, true, It.IsAny<decimal>()))
            .ReturnsAsync((true, 150m));

        var actual = await _service.CanAcceptAsync("AAPL", OrderSide.Buy, 150m);

        Assert.True(actual);
    }

    [Theory]
    [InlineData("ACCEPTED", true)]
    [InlineData("REJECTED", false)]
    public async Task ApplyOrderAsync_RecordsOrderAndLoadsExposureOnlyForAcceptedOrders(string status, bool shouldReadExposure)
    {
        const string clOrdId = "TEST-123";
        const string symbol = "AAPL";

        _orderRepository
            .Setup(r => r.AddOrderAsync(It.IsAny<OrderEntity>()))
            .Returns(Task.CompletedTask);

        _exposureRepository
            .Setup(r => r.GetExposureAsync(symbol, It.IsAny<DateOnly>()))
            .ReturnsAsync(12m);

        await _service.ApplyOrderAsync(clOrdId, symbol, Side.BUY, 10, 100m, status);

        _orderRepository.Verify(
            r => r.AddOrderAsync(It.Is<OrderEntity>(o =>
                o.ClOrdId == clOrdId &&
                o.Symbol == symbol &&
                o.Quantity == 10 &&
                o.Price == 100m &&
                o.Status == status)),
            Times.Once);

        _exposureRepository.Verify(
            r => r.GetExposureAsync(symbol, It.IsAny<DateOnly>()),
            shouldReadExposure ? Times.Once() : Times.Never());
    }
}
