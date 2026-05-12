using QuickFix;
using RTI.OrderAccumulator.Services;

namespace RTI.OrderAccumulator.Fix;

public class FixApplication : MessageCracker, IApplication
{
    private readonly OrderProcessor _processor;

    public FixApplication()
    {
        _processor = new OrderProcessor(
            new ExposureService());
    }

    public void OnCreate(SessionID sessionID)
    {
        Console.WriteLine($"SESSION CREATED: {sessionID}");
    }

    public void OnLogon(SessionID sessionID)
    {
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
        QuickFix.FIX44.NewOrderSingle order,
        SessionID sessionID)
    {
        _processor.Process(order, sessionID);
    }
}