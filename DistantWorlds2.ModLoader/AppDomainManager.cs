using System.Diagnostics;
using System.Text;
using HarmonyLib;
using JetBrains.Annotations;

namespace DistantWorlds2.ModLoader;

[PublicAPI]
public sealed class AppDomainManager : System.AppDomainManager
{
    public static ModManager ModManager { get; private set; } = null!;

    public override void InitializeNewDomain(AppDomainSetup appDomainInfo)
    {
        ConsoleHelper.CreateConsole();

        var debug = Environment.GetEnvironmentVariable("DW2MC_DEBUG");
        
        if (debug is not null && debug is not "")
        {
            var fs = new FileStream("debug.log", FileMode.Append, FileAccess.Write, FileShare.Read);
            var stdOut = Console.OpenStandardOutput();
            var stdErr = Console.OpenStandardError();
            var logger = new StreamWriter(fs, Encoding.UTF8, 4096, false) { AutoFlush = true };
            var conOut = new StreamWriter(stdOut, Encoding.UTF8, 4096, false) { AutoFlush = true };
            var conErr = new StreamWriter(stdErr, Encoding.UTF8, 4096, false) { AutoFlush = true };
            Console.SetOut(new TeeTextWriter(conOut, logger));
            Console.SetError(new TeeTextWriter(conErr, logger));
        }
        
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
