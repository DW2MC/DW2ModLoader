using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Windows.Forms;
using HarmonyLib;
using JetBrains.Annotations;
using MonoMod.Utils;
using Xenko.Core.MicroThreading;
using Xenko.Engine;

namespace DistantWorlds2.ModLoader;

[PublicAPI]
public static class StartUp
{
    private static bool _initialized;
    private static bool _started;


    private static readonly string GacBaseDir = Environment.ExpandEnvironmentVariables(@"%WINDIR%\Microsoft.NET\assembly");


    private static Assembly? GacAssemblyResolver(object _, ResolveEventArgs args)
    {
        try
        {
            var an = new AssemblyName(args.Name);
            var name = an.Name!;

            var isSystem = name.StartsWith("System.");

            if (isSystem)
            {
                var v = an.Version;

                var token = an.GetPublicKeyToken();
                var sb = new StringBuilder(token.Length * 2);
                foreach (var b in token)
                {
                    sb.Append((b >> 4).ToString("x"));
                    sb.Append((b & 0xF).ToString("x"));
                }
                var dll = name + ".dll";
                var path = Path.Combine(GacBaseDir, "GAC_MSIL", name, $"v4.0_{v.ToString(4)}__{sb}", dll);
                if (File.Exists(path))
                    return Assembly.LoadFile(path);
                path = Path.Combine(GacBaseDir, "GAC_64", name, $"v4.0_{v.ToString(4)}__{sb}", dll);
                if (File.Exists(path))
                    return Assembly.LoadFile(path);
                Console.Error.WriteLine($"Could not resolve in GAC: {an}");
            }
        }
        catch (Exception ex)
        {
            ModLoader.OnUnhandledException(ExceptionDispatchInfo.Capture(ex));
        }
        return null;
    }

    public static void StartModLoader()
    {
        if (_started) return;
        _started = true;

        var ct = Thread.CurrentThread;
        ct.CurrentCulture = CultureInfo.InvariantCulture;
        ct.CurrentUICulture = CultureInfo.InvariantCulture;

        Console.CancelKeyPress += (_, args) => {
            args.Cancel = true;
            ConsoleHelper.TryDetachFromConsoleWindow();
        };

        ConsoleHelper.ConsoleControlEvent += _ => {
            ConsoleHelper.TryDetachFromConsoleWindow();
            return true;
        };

        var debug = Environment.GetEnvironmentVariable("DW2MC_DEBUG");

        if (debug is not null && debug is not "")
        {
            ConsoleHelper.CreateConsole();
            ModLoader.DebugMode = true;
            var fs = new FileStream("debug.log", FileMode.Append, FileAccess.Write, FileShare.Read, 4096);
            var stdOut = Console.OpenStandardOutput();
            var stdErr = Console.OpenStandardError();
            var logger = new StreamWriter(fs, Encoding.UTF8, 4096, false) { AutoFlush = true };
            var conOut = new StreamWriter(stdOut, Encoding.UTF8, 4096, false) { AutoFlush = true };
            var conErr = new StreamWriter(stdErr, Encoding.UTF8, 4096, false) { AutoFlush = true };
            Console.SetOut(new TeeTextWriter(conOut, logger));
            Console.SetError(new TeeTextWriter(conErr, logger));
        }
        else
            ConsoleHelper.TryDetachFromConsoleWindow();

        Console.WriteLine($"Started {DateTime.UtcNow}");

        if (ModLoader.DebugMode)
        {
            AppDomain.CurrentDomain.AssemblyLoad += (_, args) => {
                try
                {
                    var asm = args.LoadedAssembly;
                    if (asm is null) throw new NotImplementedException("AssemblyLoad event with no LoadedAssembly");
                    Console.WriteLine($"Loaded @ {DateTime.UtcNow}: {asm.FullName}\n"
                        + $" - Code Base: {GetCodeBaseLocalPath(asm)}\n"
                        + $" - Location: {GetLocationLocalPath(asm)}");
                }
                catch (Exception ex)
                {
                    ModLoader.OnUnhandledException(ExceptionDispatchInfo.Capture(ex));
                }
            };

            Console.WriteLine($"Assemblies loaded as of {DateTime.UtcNow}:\n");
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    if (asm is null) throw new NotImplementedException("AssemblyLoad event with no LoadedAssembly");

                    Console.WriteLine($"{asm.FullName}\n"
                        + $" - Code Base: {GetCodeBaseLocalPath(asm)}\n"
                        + $" - Location: {GetLocationLocalPath(asm)}");
                }
                catch (Exception ex)
                {
                    ModLoader.OnUnhandledException(ExceptionDispatchInfo.Capture(ex));
                }
            }
        }

        try
        {
            SpinUpSockets().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            ModLoader.OnUnhandledException(ExceptionDispatchInfo.Capture(ex));
        }

        (ModLoader.Unblocker = new Unblocker())
            .UnblockFile(new Uri(typeof(StartUp).Assembly.EscapedCodeBase).LocalPath);
        ModLoader.Patches = new Patches();
        ModLoader.ModManager = new ModManager();

    }
    private static string GetCodeBaseLocalPath(Assembly asm)
    {
        if (asm is null) throw new ArgumentNullException(nameof(asm));

        string localPath;
        try
        {
            localPath = !string.IsNullOrWhiteSpace(asm.EscapedCodeBase)
                ? new Uri(asm.EscapedCodeBase).LocalPath
                : "Unknown";
        }
        catch (NotSupportedException)
        {
            localPath = "Dynamic";
        }
        catch
        {
            localPath = "Unknown";
        }
        return localPath;
    }
    private static string GetLocationLocalPath(Assembly asm)
    {
        if (asm is null) throw new ArgumentNullException(nameof(asm));

        string localPath;
        try
        {
            localPath = !string.IsNullOrWhiteSpace(asm.Location)
                ? new Uri(asm.Location).LocalPath
                : "Unknown";
        }
        catch (NotSupportedException)
        {
            localPath = "Dynamic";
        }
        catch
        {
            localPath = "Unknown";
        }
        return localPath;
    }

    public static void InitializeModLoader()
    {
        if (_initialized) return;
        _initialized = true;

        SetUpGacAssemblyResolver();

        AppDomain.CurrentDomain.AssemblyLoad += AssemblyLoadHandler;
    }
    private static void SetUpGacAssemblyResolver()
    {
        var dom = AppDomain.CurrentDomain;

        try
        {
            var miAsmResolveEvent = typeof(AppDomain).GetMethod("OnAssemblyResolveEvent",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            if (miAsmResolveEvent is not null)
            {
                var hmPrefixAsmResolverPatch =
                    new HarmonyMethod(ReflectionUtils<Assembly>.Method(a => PrefixAssemblyResolverPatch(ref a, null!, null!)));
                new Harmony("DistantWorlds2.ModLoader").Patch(miAsmResolveEvent, hmPrefixAsmResolverPatch);
                return;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Could not patch AppDomain.OnAssemblyResolveEvent! Using fallback event...");
            ModLoader.OnUnhandledException(ExceptionDispatchInfo.Capture(ex));
        }

        try { dom.AssemblyResolve += GacAssemblyResolver; }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Could not use AppDomain.AssemblyResolve event!");
            ModLoader.OnUnhandledException(ExceptionDispatchInfo.Capture(ex));
        }

    }
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static bool PrefixAssemblyResolverPatch(ref Assembly __result, Assembly assembly, string assemblyFullName)
    {
        var asm = GacAssemblyResolver(null!, new(assemblyFullName, assembly));
        if (asm is null) return true;
        __result = asm;
        return false;
    }
    private static void AssemblyLoadHandler(object sender, AssemblyLoadEventArgs args)
    {
        var asmName = args.LoadedAssembly.GetName();
        if (asmName.Name != "DistantWorlds2") return;
        AppDomain.CurrentDomain.AssemblyLoad -= AssemblyLoadHandler;
        StartModLoader();
    }


    private static async Task SpinUpSockets()
    {
        // this just causes some pre-initialization to occur
        // failures occur with debuggers and PGO, not sure why
        // some premature free occurs somewhere, but only once?

        var bc4 = new IPEndPoint(IPAddress.Any, 0);
        var bc6 = new IPEndPoint(IPAddress.IPv6Any, 0);
        try
        {
            using var s4 = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            await s4.ConnectAsync(bc4);
        }
        catch
        {
            // oof
        }
        try
        {
            using var s6 = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
            await s6.ConnectAsync(bc6);
        }
        catch
        {
            // oof
        }
        try
        {
            using var d4 = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            await d4.SendToAsync(new(Array.Empty<byte>()), SocketFlags.Broadcast, bc4);
        }
        catch
        {
            // oof
        }
        try
        {
            using var d6 = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
            await d6.SendToAsync(new(Array.Empty<byte>()), SocketFlags.Broadcast, bc6);
        }
        catch
        {
            // oof
        }
    }
}
