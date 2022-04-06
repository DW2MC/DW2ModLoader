using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Forms;
using DW2Net6Win;
using DW2Net6Win.Isolation;
using HarmonyLib;
using JetBrains.Annotations;
using MonoMod.Utils;
using NtApiDotNet;
using NtApiDotNet.Win32;
using OpenTK.Graphics.OpenGL;

public static class Program
{
    public static readonly string Version
        = typeof(Program).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!
            .InformationalVersion;

    public static readonly string? Dw2Version
        = FileVersionInfo.GetVersionInfo("DistantWorlds2.exe").ProductVersion;

    public static readonly string? ModLoaderVersion
        = File.Exists("DistantWorlds2.ModLoader.dll")
            ? FileVersionInfo.GetVersionInfo("DistantWorlds2.ModLoader.dll").ProductVersion
            : null;

    private static int QuickStringHash(int hc, string? str)
    {
        if (str is null)
            hc *= 31;
        else
            foreach (var ch in str)
                hc = hc * 31 + ch;
        return hc;
    }


    private static readonly Version V6 = new(6, 0, 0, 0);

    [UsedImplicitly]
    private static readonly Type[] TypeRefs =
    {
        typeof(Form)
    };

    public static Assembly EntryAssembly = null!;
    private static object? _dwGame;
    internal static readonly Harmony Harmony = new("DW2Net6Win");

    public static int Main(string[] args)
    {
        var invarCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentCulture = invarCulture;
        CultureInfo.DefaultThreadCurrentUICulture = invarCulture;
        CultureInfo.CurrentCulture = invarCulture;
        CultureInfo.CurrentUICulture = invarCulture;
        Thread.CurrentThread.CurrentCulture = invarCulture;
        Thread.CurrentThread.CurrentUICulture = invarCulture;
        
        var cwd = AppContext.BaseDirectory;
        try
        {
            Directory.SetCurrentDirectory(cwd);
        }
        catch
        {
            Console.Error.WriteLine("Couldn't set current directory!");
        }

        var tmpExists = Directory.Exists("tmp");
        if (!tmpExists)
        {
            Console.WriteLine("Attempting to create local tmp directory...");
            try
            {
                Directory.CreateDirectory("tmp");
                tmpExists = true;
            }
            catch
            {
                Console.Error.WriteLine("Couldn't create local tmp directory!");
            }
        }

        if (!Debugger.IsAttached)
        {
            if (!tmpExists)
                Console.Error.WriteLine("Missing local tmp directory, can't create load optimization profile.");
            else
            {
                var hc = 17;
                hc = QuickStringHash(hc, Version);
                hc = QuickStringHash(hc, Dw2Version);
                hc = QuickStringHash(hc, ModLoaderVersion);
                ProfileOptimization.SetProfileRoot("tmp");
                ProfileOptimization.StartProfile("DW2-" + hc.ToString("X8"));
            }
        }

        var disableIsolation
            = Environment.GetEnvironmentVariable("DW2MC_DISABLE_ISOLATION") == "1"
            || Debugger.IsAttached;
        if (disableIsolation)
        {
            Console.WriteLine(
                "=== === === === === === WARNING === === === === === ===\n" +
                " By having the DW2MC_DISABLE_ISOLATION environment variable\n" +
                " set or by running the game with a debugger attached, you\n" +
                " are disabling a critical security feature that protects\n" +
                " you from malicious modifications known as AppContainer\n" +
                " isolation. It is highly recommended that you do not play\n" +
                " the game casually in this state asit is only allowed for\n" +
                " development's sake, Debugging is not allowed in isolation.\n" +
                " If you'd like to continue launching the game and you know\n" +
                " what you're doing, press the [F5] key, otherwise pressing\n" +
                " any other key will ignore the environment variable and\n" +
                " continue safely.\n" +
                "=== === === === === === WARNING === === === === === ===\n\n" +
                "              Press any key to continue.\n\n");
            Console.WriteLine();
            var k = Console.ReadKey(true);
            if (k.Key != ConsoleKey.F5)
                disableIsolation = true;
        }

        var isProcessIsolated = false;
#if DEBUG
        if (!disableIsolation && RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && !(isProcessIsolated = Windows.IsProcessIsolated()))
#else
        if (!disableIsolation && RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && !(isProcessIsolated = Windows.IsProcessIsolated()))
#endif
        {
            // AppContainer Isolation implementation
            const FileSystemRights RO = FileSystemRights.Read
                | FileSystemRights.Synchronize;

            const FileSystemRights RX = RO
                | FileSystemRights.ReadAndExecute;

            const FileSystemRights RW = RO
                | FileSystemRights.Write;

            const FileSystemRights DRO = FileSystemRights.Read
                | FileSystemRights.Synchronize;

            const FileSystemRights DRW = DRO | FileSystemRights.Write
                | FileSystemRights.Delete
                | FileSystemRights.DeleteSubdirectoriesAndFiles;

            var fileAccess = new List<(string Path, FileSystemRights DirRights, FileSystemRights FileRights, bool Inherit)>
            {
                (cwd, DRO, RO, false),
                (Path.Combine(cwd, "x64"), DRO, RX, false),
                (Path.Combine(cwd, "data"), DRO, RO, true),
                (Path.Combine(cwd, "mods"), DRO, RO, true),
                (Path.Combine(cwd, "data", "Logs"), DRW, RW, true),
                (Path.Combine(cwd, "data", "SavedGames"), DRW, RW, true),
                (Path.Combine(cwd, "debug.log"), default, RW, true),
            };
            if (tmpExists)
                fileAccess.Add((Path.Combine(cwd, "tmp"), DRW, RW, true));
            else
                Console.Error.WriteLine("Warning: No tmp directory for AppContainer!");

            using var h = NtProcess.Current;
            args = new[] { $"\"{Environment.ProcessPath!}\"" }
                .Concat(args.Select(s => s.Contains(' ') && s[0] != '"' && s[^1] != '"' ? $"\"{s}\"" : s)).ToArray();

            try
            {
                var (process, container) = Windows.StartIsolatedProcess(
                    "DW2Net6Win",
                    Environment.ProcessPath ?? throw new NotImplementedException("Can't determine process path!"),
                    args,
                    new[]
                    {
                        KnownSids.CapabilityPrivateNetworkClientServer,
                        KnownSids.CapabilityInternetClient
                    },
                    true,
                    fileAccess,
                    cwd
                );

                Console.WriteLine($"Created AppContainer isolated process {process.Pid}");

                if (Debugger.IsAttached && Debugger.IsLogging())
                    Debugger.Log(0, "ChildProcess", process.Pid.ToString());

                using (container)
                using (process)
                using (var proc = Process.GetProcessById(process.Pid))
                    proc.WaitForExit();

                return 0;
            }
            catch (Exception ex)
            {
                try
                {
                    Console.WriteLine(ex.GetType().AssemblyQualifiedName);
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                }
                catch
                {
                    // ah crap
                }
                Console.Error.WriteLine(
                    "=== === === === === === WARNING === === === === === ===\n" +
                    " Failed to create AppContainer isolation environment!\n" +
                    " You may need to run this executable as admin in order to\n" +
                    " run the game isolated. Without AppContainer isolation,\n" +
                    " it may be unsafe to run the game with modifications.\n" +
                    " If you wish to continue without isolation, press the\n" +
                    " [F5] key. The exception above has some clue as to why\n" +
                    " the failure occurred, it is likely that directory\n" +
                    " permissions are already more restricted than the\n" +
                    " launcher expected for the current user.\n" +
                    "=== === === === === === WARNING === === === === === ===\n\n" +
                    "              Press any key to exit.\n\n");
                var k = Console.ReadKey(true);
                if (k.Key != ConsoleKey.F5)
                    return 0;
            }
        }

        GCSettings.LatencyMode = GCSettings.IsServerGC ? GCLatencyMode.SustainedLowLatency : GCLatencyMode.LowLatency;

        System.Windows.Forms.Application.SetHighDpiMode(HighDpiMode.PerMonitor);

        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2Support", true);
        AppContext.SetSwitch("System.Net.SocketsHttpHandler.Http2Support", true);
        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http3Support", true);
        AppContext.SetSwitch("System.Net.SocketsHttpHandler.Http3Support", true);
        //AppContext.SetSwitch("System.Net.Http.UseSocketsHttpHandler", false);

        AppDomain.CurrentDomain.AssemblyResolve += (_, eventArgs) => {
            var an = new AssemblyName(eventArgs.Name);
            var name = an.Name!;

            var isSystem = name.StartsWith("System.");

            if (isSystem)
            {
                var v = an.Version;
                if (v is not null && v.CompareTo(V6) >= 0)
                    return null;

                an.Version = V6;
                return Assembly.Load(an);
            }
            var dll = name + ".dll";

            var p = Path.Combine(cwd, dll);

            if (File.Exists(dll))
                return Assembly.LoadFile(p);

            return null;
        };

        TaskScheduler.UnobservedTaskException += (_, args) => {
            // oof
            args.SetObserved();
        };

        EntryAssembly = Assembly.LoadFile(Path.Combine(cwd, "DistantWorlds2.exe"));

        Console.WriteLine($"DW2Net6Win v{Version}");

        if (args.Length > 0)
        {
            Console.WriteLine($"Arguments: {string.Join(" ", args)}");
            if (args.Contains("-debugger"))
            {
                Console.WriteLine("Debugger requested. Waiting for debugger...");
                Debugger.Launch();
                while (!Debugger.IsAttached)
                {
                    Thread.Sleep(15);
                    Debugger.Break();
                }
            }
        }

        foreach (var ev in Environment.GetEnvironmentVariables().Cast<DictionaryEntry>())
        {
            if (ev.Key.ToString()!.StartsWith("DOTNET_"))
                Console.WriteLine($"{ev.Key}={ev.Value}");
        }

        Harmony.PatchAll();

        PatchSharpDx.ApplyIfNeeded();

        var mlPath = "DistantWorlds2.ModLoader.dll";

        Assembly? mlAsm = null;
        if (File.Exists(mlPath))
        {
            mlAsm = Assembly.LoadFile(Path.Combine(cwd, mlPath));
            var startUpType = mlAsm.GetType("DistantWorlds2.ModLoader.StartUp");
            startUpType?.InvokeMember("InitializeModLoader",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.InvokeMethod,
                null, null, new object?[] { true });

            var httpHandler = new SocketsHttpHandler
            {
                SslOptions =
                {
                    EnabledSslProtocols = SslProtocols.Tls13,
                    EncryptionPolicy = EncryptionPolicy.RequireEncryption
                },
                AllowAutoRedirect = true,
                MaxAutomaticRedirections = 12,
                AutomaticDecompression = DecompressionMethods.All,
                UseProxy = false
            };

            try
            {
                SpinUpSockets().GetAwaiter().GetResult();
            }
            catch
            {
                Console.Error.WriteLine("Network spin-up failed.");
            }

            mlAsm.GetType("DistantWorlds2.ModLoader.DelegateHttpClientFactory")!
                .InvokeMember("Inject",
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly | BindingFlags.InvokeMethod,
                    null, null, new object[]
                    {
                        (Func<HttpClient>)(() => new(httpHandler)
                        {
                            DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher,
                            DefaultRequestVersion = HttpVersion.Version20
                        }),
                        (Func<HttpMessageHandler, HttpClient>)(h => new(h)
                        {
                            DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher,
                            DefaultRequestVersion = HttpVersion.Version20
                        }),
                        (Func<HttpMessageHandler, bool, HttpClient>)((h, d) => new(h, d)
                        {
                            DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher,
                            DefaultRequestVersion = HttpVersion.Version20
                        })
                    });

            mlAsm.GetType("DistantWorlds2.ModLoader.StartUp")!
                .InvokeMember("NotifyIsolationStatus",
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly | BindingFlags.InvokeMethod,
                    null, null, new object[]
                        { isProcessIsolated });
        }

        bool ohNo;

        try
        {
            return (int)EntryAssembly.EntryPoint?.Invoke(null, new object[] { args })!;
        }
        catch (TargetInvocationException)
        {
            ohNo = true;
        }

        if (!ohNo) return 0;

        // Oh No! Anyway...

        if (mlAsm is not null)
            try
            {
                var startUpType = mlAsm.GetType("DistantWorlds2.ModLoader.StartUp");
                startUpType?.InvokeMember("InitializeModLoader",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.InvokeMethod,
                    null, null, new object?[] { false });
            }
            catch
            {
                // ok
            }

        var ss = new SplashScreen(EntryAssembly, "resources/dw2_splashscreen.jpg");

        ss.Show(true);

        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        var dwAsm = assemblies.FirstOrDefault(a => a.GetName().Name == "DistantWorlds2");
        if (dwAsm is null)
        {
            Console.WriteLine("DistantWorlds2 assembly not loaded.");
            return 1;
        }

        var dwGameType = dwAsm.GetType("DistantWorlds2.DWGame");
        if (dwGameType is null)
        {
            Console.WriteLine("DistantWorlds2.DWGame not found.");
            return 1;
        }

        _dwGame = Activator.CreateInstance(dwGameType);

        var miRun = dwGameType.GetMethod("Run",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);

        if (miRun is null)
        {
            Console.WriteLine("DistantWorlds2.DWGame.Run not found.");
            return 1;
        }

        ss.Close(TimeSpan.FromSeconds(15));

        // just being fancy and reducing call stack depth
        unsafe { ((delegate* managed<object, object?, void>)miRun.GetLdftnPointer())(_dwGame!, null); }

        //miRun.Invoke(_dwGame, new object?[] { null });

        return 0;
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
            await d4.SendToAsync(Array.Empty<byte>(), SocketFlags.Broadcast, bc4);
        }
        catch
        {
            // oof
        }
        try
        {
            using var d6 = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
            await d6.SendToAsync(Array.Empty<byte>(), SocketFlags.Broadcast, bc6);
        }
        catch
        {
            // oof
        }
    }

    public static string GetSlnFilePath([CallerFilePath] string? filePath = null)
        => Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(filePath)!)!, "DistantWorlds2.ModLoader.sln");
}
