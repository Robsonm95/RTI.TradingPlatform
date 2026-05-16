using Microsoft.Extensions.Logging;
using QuickFix;
using RTI.OrderAccumulator.Services;

namespace RTI.OrderAccumulator.Fix;

public class FixApplication : MessageCracker, IApplication
{
    private readonly OrderProcessor _processor;
    private readonly ILogger<FixApplication> _logger;

    public FixApplication(ExposureService exposureService, ILogger<FixApplication> logger, ILogger<OrderProcessor> orderProcessorLogger)
    {
        _processor = new OrderProcessor(exposureService, orderProcessorLogger);
        _logger = logger;
    }

    public void OnCreate(SessionID sessionID)
    {
        _logger.LogInformation("FIX session created: {SessionId}", sessionID);
    }

    public void OnLogon(SessionID sessionID)
    {
        _logger.LogInformation("FIX session logon: {SessionId}", sessionID);
    }

    public void OnLogout(SessionID sessionID)
    {
        _logger.LogInformation("FIX session logout: {SessionId}", sessionID);
    }

    public void ToAdmin(Message message, SessionID sessionID)
    {
        _logger.LogDebug("FIX admin out: {Message} [SessionId={SessionId}]", message, sessionID);
    }

    public void FromAdmin(Message message, SessionID sessionID)
    {
        _logger.LogDebug("FIX admin in: {Message} [SessionId={SessionId}]", message, sessionID);
    }

    public void ToApp(Message message, SessionID sessionID)
    {
        _logger.LogDebug("FIX app out: {Message} [SessionId={SessionId}]", message, sessionID);
    }

    public void FromApp(Message message, SessionID sessionID)
    {
        Crack(message, sessionID);
    }

    public void OnMessage(
        QuickFix.FIX44.NewOrderSingle order,
        SessionID sessionID)
    {
        // Fire and forget to avoid blocking the FIX processing thread
        // Use ConfigureAwait(false) to prevent deadlocks
        _ = _processor.ProcessAsync(order, sessionID).ConfigureAwait(false);
    }
}
