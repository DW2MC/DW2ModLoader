using System.Diagnostics;
using JetBrains.Annotations;

namespace DistantWorlds2.ModLoader;

[PublicAPI]
public sealed class AppDomainManager : System.AppDomainManager
{
    private static ModManager? _modManager;

    public override void InitializeNewDomain(AppDomainSetup appDomainInfo)
    {
        ConsoleHelper.CreateConsole();
        AppDomain.CurrentDomain.UnhandledException += (_, args) => {
            var ex = (Exception)args.ExceptionObject;
            Console.Error.WriteLine("=== === === === === === === === === ===");
            Console.Error.WriteLine("===  AppDomain Unhandled Exception  ===");
            Console.Error.WriteLine("=== === === === === === === === === ===");
            Console.Error.WriteLine(ex.ToStringDemystified());
            Console.Error.WriteLine("=== === === === === === === === === === ===");
            Console.Error.WriteLine("===  End AppDomain Unhandled Exception  ===");
            Console.Error.WriteLine("=== === === === === === === === === === ===");
        };
        TaskScheduler.UnobservedTaskException += (_, args) => {
            var ex = (Exception)args.Exception;
            Console.Error.WriteLine("=== === === === === === === === ===");
            Console.Error.WriteLine("===  Unobserved Task Exception  ===");
            Console.Error.WriteLine("=== === === === === === === === ===");
            Console.Error.WriteLine(ex.ToStringDemystified());
            Console.Error.WriteLine("=== === === === === === === === === ===");
            Console.Error.WriteLine("===  End Unobserved Task Exception  ===");
            Console.Error.WriteLine("=== === === === === === === === === ===");
        };
        //ConsoleHelper.TryEnableVirtualTerminalProcessing();
        _modManager = new();
    }
}
