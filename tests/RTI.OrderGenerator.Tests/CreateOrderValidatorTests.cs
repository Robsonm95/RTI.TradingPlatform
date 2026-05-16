using Microsoft.Extensions.Logging;
using Moq;
using RTI.OrderGenerator.DTOs;
using RTI.OrderGenerator.Services;
using RTI.OrderGenerator.Validation;
using RTI.Shared.Constants;
using RTI.Shared.Enums;
using Xunit;

namespace RTI.OrderGenerator.Tests;

public class CreateOrderValidatorTests
{
    [Theory]
    [InlineData("PETR4", OrderSide.Buy, 100, 42.50, true)]
    [InlineData("VALE3", OrderSide.Sell, 50, 125.75, true)]
    [InlineData("VIIA4", OrderSide.Buy, 1, 0.01, true)]
    [InlineData("VIIA4", OrderSide.Buy, 99999, 999.99, true)]
    public void Validate_AcceptsValidOrders(string symbol, OrderSide side, int quantity, decimal price, bool _)
    {
        var request = new CreateOrderRequest
        {
            Symbol = symbol,
            Side = side,
            Quantity = quantity,
            Price = price
        };

        var errors = CreateOrderValidator.Validate(request);

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData("INVALID")]
    [InlineData("")]
    [InlineData("PETR5")]
    public void Validate_RejectsInvalidSymbol(string symbol)
    {
        var request = new CreateOrderRequest
        {
            Symbol = symbol,
            Side = OrderSide.Buy,
            Quantity = 100,
            Price = 42.5m
        };

        var errors = CreateOrderValidator.Validate(request);

        Assert.Contains("Invalid symbol", errors);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    [InlineData(100000)]
    [InlineData(100001)]
    public void Validate_RejectsInvalidQuantity(int quantity)
    {
        var request = new CreateOrderRequest
        {
            Symbol = "PETR4",
            Side = OrderSide.Buy,
            Quantity = quantity,
            Price = 42.5m
        };

        var errors = CreateOrderValidator.Validate(request);

        Assert.Contains("Invalid quantity", errors);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10.5)]
    [InlineData(1000)]
    [InlineData(1000.01)]
    public void Validate_RejectsInvalidPrice(decimal price)
    {
        var request = new CreateOrderRequest
        {
            Symbol = "PETR4",
            Side = OrderSide.Buy,
            Quantity = 100,
            Price = price
        };

        var errors = CreateOrderValidator.Validate(request);

        Assert.Contains("Invalid price", errors);
    }

    [Theory]
    [InlineData(42.501)]
    [InlineData(42.555)]
    [InlineData(42.123)]
    public void Validate_RejectsNonDecimalPrice(decimal price)
    {
        var request = new CreateOrderRequest
        {
            Symbol = "PETR4",
            Side = OrderSide.Buy,
            Quantity = 100,
            Price = price
        };

        var errors = CreateOrderValidator.Validate(request);

        Assert.Contains("Price must be multiple of 0.01", errors);
    }
}

public class ExecutionReportTrackerTests
{
    [Fact]
    public async Task WaitForExecution_ReturnsCompletedTaskWhenResultAvailable()
    {
        var tracker = new ExecutionReportTracker();
        var clOrdId = "ORDER-123";
        var result = new Models.FixExecutionResult { Success = true, Status = "FILLED", ClOrdId = clOrdId };

        var task = tracker.WaitForExecution(clOrdId);
        tracker.Complete(clOrdId, result);

        var actualResult = await task;

        Assert.Equal(clOrdId, actualResult.ClOrdId);
        Assert.True(actualResult.Success);
        Assert.Equal("FILLED", actualResult.Status);
    }

    [Fact]
    public void Complete_RemovesOrderFromPendingAfterCompletion()
    {
        var tracker = new ExecutionReportTracker();
        var clOrdId = "ORDER-456";
        var result = new Models.FixExecutionResult { Success = true, Status = "FILLED", ClOrdId = clOrdId };

        var task = tracker.WaitForExecution(clOrdId);
        tracker.Complete(clOrdId, result);

        // Attempting to complete again should not throw
        tracker.Complete(clOrdId, result);
    }

    [Fact]
    public async Task WaitForExecution_AllowsMultipleConcurrentWaits()
    {
        var tracker = new ExecutionReportTracker();
        var order1 = "ORDER-1";
        var order2 = "ORDER-2";
        var result1 = new Models.FixExecutionResult { Success = true, Status = "ACCEPTED", ClOrdId = order1 };
        var result2 = new Models.FixExecutionResult { Success = false, Status = "REJECTED", ClOrdId = order2 };

        var task1 = tracker.WaitForExecution(order1);
        var task2 = tracker.WaitForExecution(order2);

        tracker.Complete(order1, result1);
        tracker.Complete(order2, result2);

        var actualResult1 = await task1;
        var actualResult2 = await task2;

        Assert.Equal(order1, actualResult1.ClOrdId);
        Assert.True(actualResult1.Success);
        Assert.Equal(order2, actualResult2.ClOrdId);
        Assert.False(actualResult2.Success);
    }
}

