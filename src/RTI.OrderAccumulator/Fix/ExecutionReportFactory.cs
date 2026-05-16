using QuickFix.Fields;
using QuickFix.FIX44;

namespace RTI.OrderAccumulator.Fix;

public static class ExecutionReportFactory
{
    public static ExecutionReport CreateAccepted(
        string clOrdId,
        string symbol,
        char side,
        decimal quantity,
        decimal price)
    {
        var report = new ExecutionReport(
            new OrderID(Guid.NewGuid().ToString()),
            new ExecID(Guid.NewGuid().ToString()),
            new ExecType(ExecType.NEW),
            new OrdStatus(OrdStatus.NEW),
            new Symbol(symbol),
            new Side(side),
            new LeavesQty(quantity),
            new CumQty(0),
            new AvgPx(0));

        report.Set(new ClOrdID(clOrdId));

        report.Set(new OrderQty(quantity));

        report.Set(new Price(price));

        return report;
    }

    public static ExecutionReport CreateRejected(
        string clOrdId,
        string symbol,
        char side,
        decimal quantity,
        decimal price,
        string reason)
    {
        var report = new ExecutionReport(
            new OrderID(Guid.NewGuid().ToString()),
            new ExecID(Guid.NewGuid().ToString()),
            new ExecType(ExecType.REJECTED),
            new OrdStatus(OrdStatus.REJECTED),
            new Symbol(symbol),
            new Side(side),
            new LeavesQty(0),
            new CumQty(0),
            new AvgPx(0));

        report.Set(new ClOrdID(clOrdId));

        report.Set(new Text(reason));

        report.Set(new OrderQty(quantity));

        report.Set(new Price(price));

        return report;
    }
}