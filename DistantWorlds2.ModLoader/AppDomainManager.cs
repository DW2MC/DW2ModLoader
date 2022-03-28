using System.Text;
using JetBrains.Annotations;

namespace DistantWorlds2.ModLoader;

[PublicAPI]
public sealed class AppDomainManager : System.AppDomainManager
{
    private static readonly Type[] TypeRefs =
    {
        typeof(ModLoader), typeof(ModManager), typeof(Patches)
    };
    public override void InitializeNewDomain(AppDomainSetup appDomainInfo)
        => StartUp.InitializeModLoader();
}
