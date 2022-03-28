using System.Text;

namespace DistantWorlds2.ModLoader;

public static class StartUp
{
    public static void InitializeModLoader()
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

        Console.WriteLine($"Started {DateTime.UtcNow}");

        (ModLoader.Unblocker = new Unblocker())
            .UnblockFile(new Uri(typeof(StartUp).Assembly.CodeBase).LocalPath);
        ModLoader.Patches = new Patches();
        ModLoader.ModManager = new ModManager();
    }
}