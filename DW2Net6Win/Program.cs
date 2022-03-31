using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
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

    private static readonly Version V4 = new(4, 0, 0, 0);
    private static readonly Version V6 = new(6, 0, 0, 0);

    [UsedImplicitly]
    private static readonly Type[] TypeRefs =
    {
        typeof(Form)
    };

    public static Assembly EntryAssembly = null!;
    private static object? _dwGame;
    private static readonly Harmony Harmony = new("DW2Net6Win");

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

            if (name.StartsWith("System.") && (an.Version?.Equals(V4) ?? false))
            {
                an.Version = V6;
                return Assembly.Load(an);
            }

            var p = Path.Combine(Environment.CurrentDirectory, name + ".dll");

            if (File.Exists(p))
                return Assembly.LoadFile(p);

            return null;
        };

        EntryAssembly = Assembly.LoadFile(Path.Combine(Environment.CurrentDirectory, "DistantWorlds2.exe"));

        var mlPath = "DistantWorlds2.ModLoader.dll";

        if (File.Exists(mlPath))
        {
            var mlAsm = Assembly.LoadFile(Path.Combine(Environment.CurrentDirectory, mlPath));
            var startUpType = mlAsm.GetType("DistantWorlds2.ModLoader.StartUp");
            startUpType?.InvokeMember("InitializeModLoader",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.InvokeMethod,
                null, null, null);

            mlAsm.GetType("DistantWorlds2.ModLoader.DelegateHttpClientFactory")!
                .InvokeMember("Inject",
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly | BindingFlags.InvokeMethod,
                    null, null, new object[]
                    {
                        (Func<HttpClient>)(() => new()
                        {
                            DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher,
                            DefaultRequestVersion = HttpVersion.Version30
                        }),
                        (Func<HttpMessageHandler, HttpClient>)(h => new(h)
                        {
                            DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher,
                            DefaultRequestVersion = HttpVersion.Version30
                        }),
                        (Func<HttpMessageHandler, bool, HttpClient>)((h, d) => new(h, d)
                        {
                            DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher,
                            DefaultRequestVersion = HttpVersion.Version30
                        })
                    });

        }

        Console.WriteLine(
            $"DW2Net6Win v{Version}");

        Harmony.PatchAll();

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
}
