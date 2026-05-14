using QuickFix;
using QuickFix.Fields;
using QuickFix.FIX44;
using RTI.OrderAccumulator.Fix;
using RTI.Shared.Enums;

namespace RTI.OrderAccumulator.Services;

public class OrderProcessor
{
    private readonly ExposureService _exposureService;

    public OrderProcessor(
        ExposureService exposureService)
    {
        _exposureService = exposureService;
    }

    public async Task ProcessAsync(
        NewOrderSingle order,
        SessionID sessionID)
    {
        var clOrdId =
            order.ClOrdID.Value;

        var symbol =
            order.Symbol.Value;

        var quantity =
            (decimal)order.OrderQty.Value;

        var price =
            order.Price.Value;

        var sideValue = order.Side.Value;
        var side = sideValue == Side.BUY
            ? OrderSide.Buy
            : OrderSide.Sell;

        var orderValue =
            quantity * price;

        var accepted =
            await _exposureService.CanAcceptAsync(
                symbol,
                side,
                orderValue);

        await _exposureService.ApplyOrderAsync(
            clOrdId,
            symbol,
            sideValue,
            (int)quantity,
            price,
            accepted ? "ACCEPTED" : "REJECTED");

        ExecutionReport report = accepted
            ? ExecutionReportFactory.CreateAccepted(
                clOrdId,
                symbol,
                sideValue,
                quantity,
                price)
            : ExecutionReportFactory.CreateRejected(
                clOrdId,
                symbol,
                sideValue,
                quantity,
                price,
                "Exposure limit exceeded");

        Console.WriteLine(accepted
            ? $"ORDER ACCEPTED [{clOrdId}]"
            : $"ORDER REJECTED [{clOrdId}]");

        Session.SendToTarget(report, sessionID);
    }
}