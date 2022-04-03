using System;
using System.Collections;
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
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using DW2Net6Win;
using HarmonyLib;
using JetBrains.Annotations;
using MonoMod.Utils;

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

    static Program()
    {
        if (Debugger.IsAttached) return;
        var hc = 17;
        hc = QuickStringHash(hc, Version);
        hc = QuickStringHash(hc, Dw2Version);
        hc = QuickStringHash(hc, ModLoaderVersion);
        ProfileOptimization.SetProfileRoot("tmp");
        ProfileOptimization.StartProfile("DW2-" + hc.ToString("X8"));
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

            var p = Path.Combine(Environment.CurrentDirectory, dll);

            if (File.Exists(dll))
                return Assembly.LoadFile(p);

            return null;
        };

        TaskScheduler.UnobservedTaskException += (_, args) => {
            // oof
            args.SetObserved();
        };

        EntryAssembly = Assembly.LoadFile(Path.Combine(Environment.CurrentDirectory, "DistantWorlds2.exe"));

        Console.WriteLine($"DW2Net6Win v{Version}");

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
            mlAsm = Assembly.LoadFile(Path.Combine(Environment.CurrentDirectory, mlPath));
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
}
