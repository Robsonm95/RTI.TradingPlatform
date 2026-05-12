using QuickFix;
using QuickFix.Fields;
using QuickFix.FIX44;
using RTI.OrderGenerator.DTOs;
using RTI.OrderGenerator.Fix;
using RTI.OrderGenerator.Models;
using RTI.Shared.Enums;

namespace RTI.OrderGenerator.Services;

public class OrderFixService
{
    private readonly ExecutionReportTracker _tracker;

    public OrderFixService(
        ExecutionReportTracker tracker)
    {
        _tracker = tracker;
    }

    public async Task<FixExecutionResult> SendOrder(
        CreateOrderRequest request)
    {
        if (SessionManager.CurrentSession is null)
        {
            throw new Exception(
                "FIX session not connected");
        }

        var clOrdId =
            Guid.NewGuid().ToString();

        var side =
            request.Side == OrderSide.Buy
                ? Side.BUY
                : Side.SELL;

        var order = new NewOrderSingle(
            new ClOrdID(clOrdId),
            new Symbol(request.Symbol),
            new Side(side),
            new TransactTime(DateTime.UtcNow),
            new OrdType(OrdType.LIMIT));

        order.Set(new OrderQty(request.Quantity));
        order.Set(new Price(request.Price));

        var waitTask =
            _tracker.WaitForExecution(clOrdId);

        Session.SendToTarget(
            order,
            SessionManager.CurrentSession);

        return await waitTask;
    }
}