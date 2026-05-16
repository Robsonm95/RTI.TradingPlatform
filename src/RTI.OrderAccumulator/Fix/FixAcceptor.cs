using Microsoft.Extensions.Logging;
using QuickFix;
using QuickFix.Logger;
using QuickFix.Store;
using RTI.OrderAccumulator.Services;

namespace RTI.OrderAccumulator.Fix;

public class FixAcceptor
{
    private ThreadedSocketAcceptor? _acceptor;
    private readonly ExposureService _exposureService;
    private readonly ILogger<FixAcceptor> _logger;

    public FixAcceptor(ExposureService exposureService, ILogger<FixAcceptor> logger)
    {
        _exposureService = exposureService;
        _logger = logger;
    }

    public void Start()
    {
        SessionSettings settings =
            new SessionSettings("Fix/fix.cfg");

        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var application = new FixApplication(
            _exposureService,
            loggerFactory.CreateLogger<FixApplication>(),
            loggerFactory.CreateLogger<RTI.OrderAccumulator.Services.OrderProcessor>());

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

        _logger.LogInformation("FIX Acceptor started");
    }

    public void Stop()
    {
        _acceptor?.Stop();
        _logger.LogInformation("FIX Acceptor stopped");
    }
}
