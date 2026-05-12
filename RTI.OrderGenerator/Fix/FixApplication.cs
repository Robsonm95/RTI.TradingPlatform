using QuickFix;
using RTI.OrderGenerator.Models;
using RTI.OrderGenerator.Services;

namespace RTI.OrderGenerator.Fix;

public class FixApplication : MessageCracker, IApplication
{
    private readonly ExecutionReportTracker _tracker;

    public FixApplication(
        ExecutionReportTracker tracker)
    {
        _tracker = tracker;
    }

    public void OnCreate(SessionID sessionID)
    {
        Console.WriteLine($"SESSION CREATED: {sessionID}");
    }

    public void OnLogon(SessionID sessionID)
    {
        SessionManager.CurrentSession = sessionID;

        Console.WriteLine($"LOGON: {sessionID}");
    }

    public void OnLogout(SessionID sessionID)
    {
        Console.WriteLine($"LOGOUT: {sessionID}");
    }

    public void ToAdmin(Message message, SessionID sessionID)
    {
        Console.WriteLine($"TO ADMIN: {message}");
    }

    public void FromAdmin(Message message, SessionID sessionID)
    {
        Console.WriteLine($"FROM ADMIN: {message}");
    }

    public void ToApp(Message message, SessionID sessionID)
    {
        Console.WriteLine($"SENT: {message}");
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
            report.ClOrdID.getValue();

        var success =
            report.ExecType.getValue()
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
                        ? report.Text.getValue()
                        : "Order rejected"
            };

        _tracker.Complete(clOrdId, result);

        Console.WriteLine(
            $"EXECUTION REPORT [{clOrdId}] {result.Status}");
    }
}