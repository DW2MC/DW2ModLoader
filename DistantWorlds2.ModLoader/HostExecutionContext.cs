using System.Globalization;

namespace DistantWorlds2.ModLoader;

public sealed class HostExecutionContext : System.Threading.HostExecutionContext
{
    private readonly System.Threading.HostExecutionContext? _wrapped;

    public HostExecutionContext(System.Threading.HostExecutionContext? wrapped)
        => _wrapped = wrapped;

    public override System.Threading.HostExecutionContext CreateCopy()
    {
        var x = _wrapped?.CreateCopy() ?? base.CreateCopy();
        var ct = Thread.CurrentThread;
        ct.CurrentCulture = CultureInfo.InvariantCulture;
        ct.CurrentUICulture = CultureInfo.InvariantCulture;
        return x;
    }

    public override void Dispose(bool disposing)
        => _wrapped?.Dispose(disposing);
}
