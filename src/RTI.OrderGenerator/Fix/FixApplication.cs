using QuickFix;
using RTI.OrderGenerator.Models;
using RTI.OrderGenerator.Services;

namespace RTI.OrderGenerator.Fix;

public class FixApplication : MessageCracker, IApplication
{
    private readonly ExecutionReportTracker _tracker;
    private readonly ILogger<FixApplication> _logger;

    public FixApplication(
        ExecutionReportTracker tracker,
        ILogger<FixApplication> logger)
    {
        _tracker = tracker;
        _logger = logger;
    }

    public void OnCreate(SessionID sessionID)
    {
        _logger.LogInformation("FIX session created: {SessionId}", sessionID);
    }

    public void OnLogon(SessionID sessionID)
    {
        SessionManager.CurrentSession = sessionID;
        _logger.LogInformation("FIX logged on: {SessionId}", sessionID);
    }

    public void OnLogout(SessionID sessionID)
    {
        SessionManager.CurrentSession = null;
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
        QuickFix.FIX44.ExecutionReport report,
        SessionID sessionID)
    {
        var clOrdId =
            report.ClOrdID.Value;

        var success =
            report.ExecType.Value
                == QuickFix.Fields.ExecType.NEW;

        var result =
            new FixExecutionResult
            {
                ClOrdId = clOrdId,

                Success = success,

                Status = success
                    ? "Accepted"
                    : "Rejected",

                Message = success
                    ? "Order accepted"
                    : report.IsSetText()
                        ? report.Text.Value
                        : "Order rejected"
            };

        _tracker.Complete(clOrdId, result);

        _logger.LogInformation("Execution report processed for ClOrdId {ClOrdId}: {Status}", clOrdId, result.Status);
    }
}