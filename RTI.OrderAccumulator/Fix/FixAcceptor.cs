using QuickFix;
using QuickFix.Logger;
using QuickFix.Store;

namespace RTI.OrderAccumulator.Fix;

public class FixAcceptor
{
    private ThreadedSocketAcceptor? _acceptor;

    public void Start()
    {
        SessionSettings settings =
            new SessionSettings("Fix/fix.cfg");

        IApplication application =
            new FixApplication();

        IMessageStoreFactory storeFactory =
            new FileStoreFactory(settings);

        ILogFactory logFactory =
            new FileLogFactory(settings);

        _acceptor = new ThreadedSocketAcceptor(
            application,
            storeFactory,
            settings,
            logFactory);

        _acceptor.Start();

        Console.WriteLine("FIX Acceptor Started");
    }

    public void Stop()
    {
        _acceptor?.Stop();
    }
}