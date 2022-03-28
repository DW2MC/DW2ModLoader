using System;
using System.IO;
using System.Reflection;
using System.Runtime;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using HarmonyLib;
using MonoMod.Utils;

public static class Program
{
    private static readonly Version V4 = new(4, 0, 0, 0);
    private static readonly Version V6 = new(6, 0, 0, 0);

    private static readonly Type[] TypeRefs =
    {
        typeof(Form)
    };
    
    public static Assembly EntryAssembly = null!;
    private static object? _dwGame;
    private static readonly Harmony Harmony = new Harmony("DW2Net6Win");
    private static object? _modLoader;
    
    public static int Main(string[] args)
    {
        GCSettings.LatencyMode = GCSettings.IsServerGC ? GCLatencyMode.SustainedLowLatency : GCLatencyMode.LowLatency;

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

        var mlPath = Path.Combine(Environment.CurrentDirectory, "DistantWorlds2.ModLoader.dll");

        if (File.Exists(mlPath))
        {
            var mlAsm = Assembly.LoadFile(mlPath)!;
            var modMgrType = mlAsm.GetType("DistantWorlds2.ModLoader.ModManager");
            {
                if (modMgrType is not null)
                    _modLoader = Activator.CreateInstance(modMgrType);
            }
        }

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
