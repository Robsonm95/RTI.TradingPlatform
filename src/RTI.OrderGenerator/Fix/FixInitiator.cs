using Microsoft.Extensions.Logging;
using QuickFix;
using QuickFix.Logger;
using QuickFix.Store;
using QuickFix.Transport;
using RTI.OrderGenerator.Services;

namespace RTI.OrderGenerator.Fix;

public class FixInitiator
{
    private readonly ExecutionReportTracker _tracker;
    private readonly ILogger<FixInitiator> _logger;
    private readonly ILoggerFactory _loggerFactory;

    private SocketInitiator? _initiator;

    public FixInitiator(
        ExecutionReportTracker tracker,
        ILogger<FixInitiator> logger,
        ILoggerFactory loggerFactory)
    {
        _tracker = tracker;
        _logger = logger;
        _loggerFactory = loggerFactory;
    }

    public void Start()
    {
        var settings = new SessionSettings("Fix/fix.cfg");
        OverrideConnectionSettings(settings);

        IApplication application =
            new FixApplication(
                _tracker,
                _loggerFactory.CreateLogger<FixApplication>());
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

        _logger.LogInformation("FIX Initiator started");
    }

    private static void OverrideConnectionSettings(SessionSettings settings)
    {
        var hostOverride = Environment.GetEnvironmentVariable("FIX_CONNECT_HOST");
        var portOverride = Environment.GetEnvironmentVariable("FIX_CONNECT_PORT");

        if (string.IsNullOrWhiteSpace(hostOverride) && string.IsNullOrWhiteSpace(portOverride))
        {
            return;
        }

        foreach (SessionID sessionId in settings.GetSessions())
        {
            var sessionSettings = settings.Get(sessionId);
            if (!string.IsNullOrWhiteSpace(hostOverride))
            {
                sessionSettings.SetString("SocketConnectHost", hostOverride);
            }

            if (!string.IsNullOrWhiteSpace(portOverride))
            {
                sessionSettings.SetString("SocketConnectPort", portOverride);
            }
        }
    }
    
    public void Stop()
    {
        _initiator?.Stop();
    }
}