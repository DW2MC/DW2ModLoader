using JetBrains.Annotations;

namespace DistantWorlds2.ModLoader;

[PublicAPI]
public sealed class AppDomainManager : System.AppDomainManager
{
    private static ModManager? _modManager;
    
    public override void InitializeNewDomain(AppDomainSetup appDomainInfo)
        => _modManager = new();
}
