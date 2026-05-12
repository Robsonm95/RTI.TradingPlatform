using QuickFix;

namespace RTI.OrderGenerator.Fix;

public static class SessionManager
{
    public static SessionID? CurrentSession { get; set; }
}