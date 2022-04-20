using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime;
using JetBrains.Annotations;

namespace DistantWorlds2.ModLoader;

[PublicAPI]
public sealed class AppDomainManager : System.AppDomainManager
{
    public static readonly string ModLoaderVersion
        = typeof(AppDomainManager).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!
            .InformationalVersion;

    public static readonly string? Dw2Version
        = FileVersionInfo.GetVersionInfo("DistantWorlds2.exe").ProductVersion;

    private static int QuickStringHash(int hc, string? str)
    {
        if (str is null)
            hc *= 31;
        else
            foreach (var ch in str)
                hc = hc * 31 + ch;
        return hc;
    }


    private static readonly Type[] TypeRefs =
    {
        typeof(ModLoader), typeof(ModManager), typeof(Patches)
    };

    static AppDomainManager()
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

        var ct = Thread.CurrentThread;
        ct.CurrentCulture = CultureInfo.InvariantCulture;
        ct.CurrentUICulture = CultureInfo.InvariantCulture;
    }

    public override void InitializeNewDomain(AppDomainSetup appDomainInfo)
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

        var ct = Thread.CurrentThread;
        ct.CurrentCulture = CultureInfo.InvariantCulture;
        ct.CurrentUICulture = CultureInfo.InvariantCulture;

        if (!Debugger.IsAttached)
        {
            var hc = 17;
            hc = QuickStringHash(hc, Dw2Version);
            hc = QuickStringHash(hc, ModLoaderVersion);
            ProfileOptimization.SetProfileRoot("tmp");
            ProfileOptimization.StartProfile("DW2-" + hc.ToString("X8"));
        }

        StartUp.InitializeModLoader();
    }

    public override System.Threading.HostExecutionContextManager HostExecutionContextManager { get; }
        = new HostExecutionContextManager();
}
