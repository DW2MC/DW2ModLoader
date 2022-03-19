using JetBrains.Annotations;

namespace DistantWorlds2.ModLoader;

[PublicAPI]
public sealed class AppDomainManager : System.AppDomainManager
{
    public static ModManager ModManager { get; private set; } = null!;

    public override void InitializeNewDomain(AppDomainSetup appDomainInfo)
    {
        ConsoleHelper.CreateConsole();

        //ConsoleHelper.TryEnableVirtualTerminalProcessing();
        ModManager = new();
    }
}
