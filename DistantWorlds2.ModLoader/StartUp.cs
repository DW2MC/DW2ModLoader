using System.Text;
using JetBrains.Annotations;
using Xenko.Core.MicroThreading;
using Xenko.Engine;

namespace DistantWorlds2.ModLoader;

[PublicAPI]
public static class StartUp
{
    private static bool _initialized;
    private static bool _started;

    public static void StartModLoader()
    {
        if (_started) return;
        _started = true;

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

        Console.WriteLine($"Started {DateTime.UtcNow}");

        (ModLoader.Unblocker = new Unblocker())
            .UnblockFile(new Uri(typeof(StartUp).Assembly.CodeBase).LocalPath);
        ModLoader.Patches = new Patches();
        ModLoader.ModManager = new ModManager();
    }
    public static void InitializeModLoader()
    {
        if (_initialized) return;
        _initialized = true;

        AppDomain.CurrentDomain.AssemblyLoad += AssemblyLoadHandler;
    }
    private static void AssemblyLoadHandler(object sender, AssemblyLoadEventArgs args)
    {
        var asmName = args.LoadedAssembly.GetName();
        if (asmName.Name != "DistantWorlds2") return;
        AppDomain.CurrentDomain.AssemblyLoad -= AssemblyLoadHandler;
        StartModLoader();
    }
}
