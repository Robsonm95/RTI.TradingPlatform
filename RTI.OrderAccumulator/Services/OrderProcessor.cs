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

    public void Process(
        NewOrderSingle order,
        SessionID sessionID)
    {
        var clOrdId =
            order.ClOrdID.getValue();

        var symbol =
            order.Symbol.getValue();

        var quantity =
            (decimal)order.OrderQty.getValue();

        var price =
            order.Price.getValue();

        var side =
            order.Side.getValue() == Side.BUY
                ? OrderSide.Buy
                : OrderSide.Sell;

        var orderValue =
            quantity * price;

        var accepted =
            _exposureService.CanAccept(
                symbol,
                side,
                orderValue);

        ExecutionReport report;

        if (accepted)
        {
            _exposureService.ApplyOrder(
                symbol,
                side,
                orderValue);

            report =
                ExecutionReportFactory
                    .CreateAccepted(
                        clOrdId,
                        symbol,
                        order.Side.getValue(),
                        quantity,
                        price);

            Console.WriteLine(
                $"ORDER ACCEPTED [{clOrdId}]");
        }
        else
        {
            report =
                ExecutionReportFactory
                    .CreateRejected(
                        clOrdId,
                        symbol,
                        order.Side.getValue(),
                        quantity,
                        price,
                        "Exposure limit exceeded");

            Console.WriteLine(
                $"ORDER REJECTED [{clOrdId}]");
        }

        Session.SendToTarget(report, sessionID);
    }
}