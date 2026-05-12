using QuickFix;
using QuickFix.Logger;
using QuickFix.Store;
using RTI.OrderAccumulator.Services;

namespace RTI.OrderAccumulator.Fix;

public class FixAcceptor
{
    private ThreadedSocketAcceptor? _acceptor;
    private readonly ExposureService _exposureService;

    public FixAcceptor(ExposureService exposureService)
    {
        _exposureService = exposureService;
    }

    public void Start()
    {
        SessionSettings settings =
            new SessionSettings("Fix/fix.cfg");

        IApplication application =
            new FixApplication(_exposureService);

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
