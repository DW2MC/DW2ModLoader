using System.Diagnostics;
using System.Reflection;
using System.Runtime;
using System.Text;
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

    static AppDomainManager()
    {
        if (Debugger.IsAttached) return;
        var hc = 17;
        hc = QuickStringHash(hc, Dw2Version);
        hc = QuickStringHash(hc, ModLoaderVersion);
        ProfileOptimization.SetProfileRoot("tmp");
        ProfileOptimization.StartProfile("DW2-" + hc.ToString("X8"));
    }
    
    private static readonly Type[] TypeRefs =
    {
        typeof(ModLoader), typeof(ModManager), typeof(Patches)
    };
    public override void InitializeNewDomain(AppDomainSetup appDomainInfo)
        => StartUp.InitializeModLoader();
}
