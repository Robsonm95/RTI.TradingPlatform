using Microsoft.Extensions.Logging;
using QuickFix;
using QuickFix.Fields;
using QuickFix.FIX44;
using RTI.OrderAccumulator.Fix;
using RTI.Shared.Enums;

namespace RTI.OrderAccumulator.Services;

public class OrderProcessor
{
    private readonly ExposureService _exposureService;
    private readonly ILogger<OrderProcessor> _logger;

    public OrderProcessor(
        ExposureService exposureService,
        ILogger<OrderProcessor> logger)
    {
        _exposureService = exposureService;
        _logger = logger;
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

        var report = accepted
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

        if (accepted)
        {
            _logger.LogInformation("Order accepted: {ClOrdId} symbol={Symbol} price={Price} qty={Quantity}", clOrdId, symbol, price, quantity);
        }
        else
        {
            _logger.LogWarning("Order rejected: {ClOrdId} symbol={Symbol} price={Price} qty={Quantity} reason=Exposure limit exceeded", clOrdId, symbol, price, quantity);
        }

        Session.SendToTarget(report, sessionID);
    }
}