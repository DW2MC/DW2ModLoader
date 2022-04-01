using System.Globalization;
using System.Runtime.InteropServices;

namespace DistantWorlds2.ModLoader;

public sealed class HostExecutionContextManager : System.Threading.HostExecutionContextManager
{
    public override object SetHostExecutionContext(System.Threading.HostExecutionContext hec)
    {
        var x = base.SetHostExecutionContext(new HostExecutionContext(hec));
        var ct = Thread.CurrentThread;
        ct.CurrentCulture = CultureInfo.InvariantCulture;
        ct.CurrentUICulture = CultureInfo.InvariantCulture;
        return x;
    }

    public override System.Threading.HostExecutionContext Capture()
    {
        var hec = base.Capture();
        var ct = Thread.CurrentThread;
        ct.CurrentCulture = CultureInfo.InvariantCulture;
        ct.CurrentUICulture = CultureInfo.InvariantCulture;
        return hec is not HostExecutionContext ? new HostExecutionContext(hec!) : hec;
    }

    public override void Revert(object previousState)
    {
        var ct = Thread.CurrentThread;
        ct.CurrentCulture = CultureInfo.InvariantCulture;
        ct.CurrentUICulture = CultureInfo.InvariantCulture;
        base.Revert(previousState);
    }
}