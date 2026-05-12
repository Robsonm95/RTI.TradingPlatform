using RTI.OrderGenerator.Models;

namespace RTI.OrderGenerator.Services;

public class ExecutionReportTracker
{
    private readonly Dictionary<string,
        TaskCompletionSource<FixExecutionResult>>
        _pendingOrders = new();

    public Task<FixExecutionResult> WaitForExecution(
        string clOrdId)
    {
        var source =
            new TaskCompletionSource<FixExecutionResult>();

        _pendingOrders[clOrdId] = source;

        return source.Task;
    }

    public void Complete(
        string clOrdId,
        FixExecutionResult result)
    {
        if (_pendingOrders.TryGetValue(
            clOrdId,
            out var source))
        {
            source.SetResult(result);

            _pendingOrders.Remove(clOrdId);
        }
    }
}