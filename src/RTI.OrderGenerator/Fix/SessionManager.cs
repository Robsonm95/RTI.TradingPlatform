using QuickFix;

namespace RTI.OrderGenerator.Fix;

/// <summary>
/// Thread-safe session manager for FIX client.
/// Uses lock to prevent race conditions when accessing CurrentSession.
/// </summary>
public static class SessionManager
{
    private static SessionID? _currentSession;
    private static readonly object _lockObject = new();

    public static SessionID? CurrentSession
    {
        get
        {
            lock (_lockObject)
            {
                return _currentSession;
            }
        }
        set
        {
            lock (_lockObject)
            {
                _currentSession = value;
            }
        }
    }

    public static bool IsConnected
    {
        get
        {
            lock (_lockObject)
            {
                return _currentSession != null;
            }
        }
    }
}