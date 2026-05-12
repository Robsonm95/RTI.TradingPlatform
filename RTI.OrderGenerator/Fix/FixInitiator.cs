using QuickFix;
using QuickFix.Logger;
using QuickFix.Store;
using QuickFix.Transport;
using RTI.OrderGenerator.Services;

namespace RTI.OrderGenerator.Fix;

public class FixInitiator
{
    private readonly ExecutionReportTracker _tracker;

    private SocketInitiator? _initiator;

    public FixInitiator(
        ExecutionReportTracker tracker)
    {
        _tracker = tracker;
    }

    public void Start()
    {
        SessionSettings settings =
            new SessionSettings("Fix/fix.cfg");

        IApplication application =
            new FixApplication(_tracker);

        IMessageStoreFactory storeFactory =
            new FileStoreFactory(settings);

        ILogFactory logFactory =
            new FileLogFactory(settings);

        _initiator = new SocketInitiator(
            application,
            storeFactory,
            settings,
            logFactory);

        _initiator.Start();

        Console.WriteLine("FIX Initiator Started");
    }
    
    public void Stop()
    {
        _initiator?.Stop();
    }
}