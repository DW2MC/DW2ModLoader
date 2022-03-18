using System.Diagnostics;
using HarmonyLib;
using JetBrains.Annotations;

namespace DistantWorlds2.ModLoader;

[PublicAPI]
public sealed class AppDomainManager : System.AppDomainManager
{
    public static ModManager ModManager { get; private set; } = null!;

    public override void InitializeNewDomain(AppDomainSetup appDomainInfo)
    {
        new Harmony("DistantWorlds2ModLoader").PatchAll();

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
        ModManager = new();
    }
}
