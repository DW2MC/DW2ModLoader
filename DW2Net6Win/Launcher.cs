using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
using System.Windows.Forms;
using DistantWorlds.Types;
using DW2Net6Win.Isolation;
using HarmonyLib;
using JetBrains.Annotations;
using MonoMod.Utils;
using NtApiDotNet;
using Xenko.Engine;
using MessageBox = System.Windows.Forms.MessageBox;

public static class Launcher
{
    public static readonly string Version
        = typeof(Launcher).Assembly
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

    [UsedImplicitly]
    private static readonly Type[] TypeRefs =
    {
        typeof(Form)
    };

    public static Assembly EntryAssembly = null!;
    private static object? _dwGame;
    internal static readonly Harmony Harmony = new("DW2Net6Win");

    [SuppressMessage("ReSharper", "CognitiveComplexity")]
    public static int Run(string[] args)
    {
        RestartRun:
        var invarCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentCulture = invarCulture;
        CultureInfo.DefaultThreadCurrentUICulture = invarCulture;
        CultureInfo.CurrentCulture = invarCulture;
        CultureInfo.CurrentUICulture = invarCulture;
        Thread.CurrentThread.CurrentCulture = invarCulture;
        Thread.CurrentThread.CurrentUICulture = invarCulture;

        var tmpExists = CreateTmpDir();

        StartLoadProfileOptimization(tmpExists);

        var disableConsole = CheckForDisabledConsoleRequested();

        var disableIsolation = CheckForDisabledIsolationRequested();

        if (disableIsolation)
        {
            if (disableConsole || !Environment.UserInteractive || !Console.IsInputRedirected)
            {
                var result = MessageBox.Show("By having the DW2MC_DISABLE_ISOLATION environment variable " +
                    "set or by running the game with a debugger attached, you are disabling a critical security feature that protects " +
                    "you from malicious modifications known as AppContainer isolation. It is highly recommended that you do not play " +
                    "the game casually in this state as it is only allowed for development's sake, Debugging is difficult in isolation.\n" +
                    "If you'd like to continue launching the game and you know what you're doing, press the Ignore button.\n\n" +
                    "Press Retry to enable AppContainer isolation.\nPress Abort to exit.",
                    "WARNING!",
                    MessageBoxButtons.AbortRetryIgnore,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button3);
                switch (result)
                {
                    case DialogResult.Retry:
                        disableIsolation = false;
                        break;
                    case DialogResult.Abort: return 0;
                }
            }
            else
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
                if (Console.ReadKey(true).Key != ConsoleKey.F5)
                    disableIsolation = false;
            }
        }

        var isProcessIsolated = false;
        var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        var wd = SetAppWorkingDirectory(isWindows);
        
        if (!disableIsolation && isWindows && !(isProcessIsolated = Windows.IsProcessIsolated()))
        {
            // AppContainer Isolation implementation
            // ReSharper disable InconsistentNaming
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
            // ReSharper restore InconsistentNaming

            var fileAccess = new List<(string Path, FileSystemRights DirRights, FileSystemRights FileRights, bool Inherit)>
            {
                (wd, DRO, RO, false),
                (Path.Combine(wd, "x64"), DRO, RX, false),
                (Path.Combine(wd, "data"), DRO, RO, true),
                (Path.Combine(wd, "mods"), DRO, RO, true),
            };

            if (tmpExists)
                fileAccess.Add((Path.Combine(wd, "tmp"), DRW, RW, true));
            else
                Console.Error.WriteLine("Warning: No tmp directory for AppContainer!");

            foreach (var path in new[]
                     {
                         Path.Combine(wd, "cache"),
                         Path.Combine(wd, "log"),
                         Path.Combine(wd, "roaming"),
                         Path.Combine(wd, "data", "Logs"),
                         Path.Combine(wd, "data", "SavedGames"),
                         Path.Combine(wd, "data", "FleetTemplates"),
                         Path.Combine(wd, "local", "db")
                     })
            {
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                fileAccess.Add((path, DRW, RW, true));
            }

            foreach (var path in new[]
                     {
                         Path.Combine(wd, "debug.log"),
                         Path.Combine(wd, "data", "SessionActive"),
                         Path.Combine(wd, "data", "TourItemsSeen"),
                         Path.Combine(wd, "data", "GameStartSettings"),
                         Path.Combine(wd, "data", "GameSettings"),
                     })
            {
                try
                {
                    if (!File.Exists(path))
                    {
                        if (path.EndsWith("GameSettings"))
                        {
                            using var fs = File.Create(path, 4096, FileOptions.WriteThrough);
                            fs.Write(Defaults.GameSettings);
                        }
                        else if (path.EndsWith("GameStartSettings"))
                        {
                            using var fs = File.Create(path, 4096, FileOptions.WriteThrough);
                            fs.Write(Defaults.GameStartSettings);
                        }
                        else if (path.EndsWith("TourItemsSeen"))
                        {
                            using var fs = File.Create(path, 4096, FileOptions.WriteThrough);
                            fs.Write(Defaults.TourItemsSeen);
                        }
                        else
                            File.WriteAllBytes(path, Array.Empty<byte>());
                    }
                    fileAccess.Add((path, DRW, RW, true));
                }
                catch
                {
                    Console.WriteLine($"Failed to touch: {path}");
                }
            }

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
                    wd
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

                if (disableConsole || !Environment.UserInteractive || !Console.IsInputRedirected)
                {
                    var result = MessageBox.Show("Failed to create an AppContainer isolation environment!\n" +
                        "There was an exception that may be due to restrictive game directory permissions " +
                        "and this executable would need to be executed as admin to correctly add the AppContainer permissions, " +
                        "alternatively this could be due to being already under some form of isolation or emulation.\n" +
                        "Without AppContainer isolation, it may be unsafe to run the game with modifications.\n" +
                        "If you wish to continue without isolation, press the Ignore button.\n\n" +
                        "Press Retry to restart the application.\nPress Abort to exit safely.",
                        "WARNING!",
                        MessageBoxButtons.AbortRetryIgnore,
                        MessageBoxIcon.Warning,
                        MessageBoxDefaultButton.Button3);
                    switch (result)
                    {
                        case DialogResult.Ignore:
                            break;
                        case DialogResult.Retry:
                            goto RestartRun;
                        case DialogResult.Abort:
                            return 0;
                    }
                }
                else
                {
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
                    if (Console.ReadKey(true).Key != ConsoleKey.F5)
                        return 0;
                }
            }
        }

        GCSettings.LatencyMode
            = GCSettings.IsServerGC
                ? GCLatencyMode.SustainedLowLatency
                : GCLatencyMode.LowLatency;

        System.Windows.Forms.Application.SetHighDpiMode(HighDpiMode.PerMonitor);

        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2Support", true);
        AppContext.SetSwitch("System.Net.SocketsHttpHandler.Http2Support", true);
        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http3Support", true);
        AppContext.SetSwitch("System.Net.SocketsHttpHandler.Http3Support", true);
        //AppContext.SetSwitch("System.Net.Http.UseSocketsHttpHandler", false);

        TaskScheduler.UnobservedTaskException += (_, eventArgs) => {
            // oof
            eventArgs.SetObserved();
        };

        EntryAssembly = Assembly.LoadFile(Path.Combine(wd, "DistantWorlds2.exe"));

        Console.WriteLine($"DW2Net6Win v{Version}");

        LaunchDebuggerIfRequested(args);

#if DEBUG
        DisplayDotNetEnvVars();
#endif

        SetHarmonyLogToConsole();

        Harmony.PatchAll();

        //PatchSharpDx.ApplyIfNeeded();

        var mlAsm = TryLoadModLoader(wd, isProcessIsolated);

        try
        {
            EntryAssembly.EntryPoint!.Invoke(null, new object?[] { args });
        }
        catch (Exception ex)
        {
#if DEBUG
            Console.Error.WriteLine(ex);
#endif
        }

        // Oh No! Anyway...

        InitializeModLoader(mlAsm);

        var ss = new SplashScreen(EntryAssembly, "resources/dw2_splashscreen.jpg");

        ss.Show(true);

        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        var dwAsm = assemblies.FirstOrDefault
            (a => a.GetName().Name is "DistantWorlds2");
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

        Game.GameStarted += (_, _) => {
            var game = (Game)_dwGame!;
            Windows.BringWindowToTop(game.Window.NativeWindow.Handle);
        };

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
    private static void SetHarmonyLogToConsole()
    {
        Environment.SetEnvironmentVariable("HARMONY_LOG_FILE", "CONOUT$");
#if DEBUG
        Harmony.DEBUG = true;
#endif
    }
    private static void DisplayDotNetEnvVars()
    {
        foreach (var ev in Environment.GetEnvironmentVariables().Cast<DictionaryEntry>())
        {
            if (ev.Key.ToString()!.StartsWith("DOTNET_"))
                Console.WriteLine($"{ev.Key}={ev.Value}");
        }
    }
    private static void LaunchDebuggerIfRequested(string[] args)
    {
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
    }
    private static bool CheckForDisabledIsolationRequested()
        => Environment.GetEnvironmentVariable("DW2MC_DISABLE_ISOLATION") == "1"
            || Debugger.IsAttached;

    private static bool CheckForDisabledConsoleRequested()
        => Environment.GetEnvironmentVariable("DW2MC_NO_CONSOLE") == "1";

    private static void StartLoadProfileOptimization(bool tmpExists)
    {
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
    }
    private static string SetAppWorkingDirectory(bool isWindows)
    {
        var cwd = AppContext.BaseDirectory;
        try
        {
            if (cwd.Equals(Environment.CurrentDirectory,
                    isWindows
                        ? StringComparison.Ordinal
                        : StringComparison.OrdinalIgnoreCase))
                Directory.SetCurrentDirectory(cwd);
        }
        catch
        {
            Console.Error.WriteLine("Couldn't set current directory!");
            Console.Error.WriteLine($"Desired Directory: {AppContext.BaseDirectory}");
            Console.Error.WriteLine($"Current Directory: {Environment.CurrentDirectory}");
        }
        return cwd;
    }
    private static bool CreateTmpDir()
    {
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
        return tmpExists;
    }
    private static void InitializeModLoader(Assembly? mlAsm)
    {
        var forcedFailure = false;
        if (mlAsm is not null)
            try
            {
                InitializeModLoader(mlAsm, forcedFailure);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Failed to initialize Mod Loader.");
                Console.Error.WriteLine(ex.ToString());
            }
    }

    private static unsafe delegate * managed<bool, void> _pInitializeModLoaderFn;
    private static unsafe void InitializeModLoader(Assembly mlAsm, bool forcedFailure)
    {
        if (_pInitializeModLoaderFn is null)
        {
            var startUpType = mlAsm.GetType("DistantWorlds2.ModLoader.StartUp")!;
            _pInitializeModLoaderFn = (delegate * managed<bool, void>)
                startUpType.GetMethod("InitializeModLoader",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.InvokeMethod,
                    new[] { typeof(bool) }).GetLdftnPointer();
        }
        _pInitializeModLoaderFn(forcedFailure);
    }

    private static Assembly? TryLoadModLoader(string cwd, bool isProcessIsolated)
    {
        var mlPath = "DistantWorlds2.ModLoader.dll";

        Assembly? mlAsm = null;
        if (File.Exists(mlPath))
        {
            mlAsm = Assembly.LoadFile(Path.Combine(cwd, mlPath));
            InitializeModLoader(mlAsm, true);

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
                            DefaultRequestVersion = HttpVersion.Version11
                        }),
                        (Func<HttpMessageHandler, HttpClient>)(h => new(h)
                        {
                            DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher,
                            DefaultRequestVersion = HttpVersion.Version11
                        }),
                        (Func<HttpMessageHandler, bool, HttpClient>)((h, d) => new(h, d)
                        {
                            DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher,
                            DefaultRequestVersion = HttpVersion.Version11
                        })
                    });

            mlAsm.GetType("DistantWorlds2.ModLoader.StartUp")!
                .InvokeMember("NotifyIsolationStatus",
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly | BindingFlags.InvokeMethod,
                    null, null, new object[]
                        { isProcessIsolated });
        }
        return mlAsm;
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
