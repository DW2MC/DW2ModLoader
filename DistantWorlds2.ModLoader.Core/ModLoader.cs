using System.Diagnostics;
using System.Runtime.ExceptionServices;
using JetBrains.Annotations;

namespace DistantWorlds2.ModLoader;

[PublicAPI]
public static class ModLoader
{
    public static IModManager ModManager { get; internal set; } = null!;

    public static IUnblocker Unblocker { get; internal set; } = null!;

    public static IPatches Patches { get; internal set; } = null!;

    public static IHttpClientFactory HttpClientFactory { get; internal set; } = new DefaultHttpClientFactory();

    public static bool DebugMode { get; internal set; }

    public static bool IntentionallyFail { get; internal set; }

    public static bool IsIsolated { get; internal set; }

    public static event Action<ExceptionDispatchInfo>? UnhandledException;

    public static void OnUnhandledException(ExceptionDispatchInfo edi)
        => UnhandledException?.Invoke(edi);

    public static readonly ManualResetEventSlim Ready = new();
    public static readonly ManualResetEventSlim Loaded = new();
    public static readonly ManualResetEventSlim GameStarted = new();

    public static void WaitForReadyAndLoaded()
    {
        for (var i = 0;; ++i)
        {
            if (Ready.Wait(1000))
                break;
            Console.Error.WriteLine($"Waited {i}s on Ready event.");
            Console.Error.WriteLine(EnhancedStackTrace.Current());
        }
        if (!Loaded.IsSet)
            ModManager.LoadContent();
        for (var i = 0;; ++i)
        {
            if (Loaded.Wait(1000))
                break;
            Console.Error.WriteLine($"Waited {i}s on Loaded event.");
            Console.Error.WriteLine(EnhancedStackTrace.Current());
        }
    }
    public static bool WaitForLoaded()
    {
        if (!Ready.IsSet) return false;
        if (!Loaded.IsSet)
            ModManager.LoadContent();
        for (var i = 0;; ++i)
        {
            if (Loaded.Wait(1000))
                break;
            Console.Error.WriteLine($"Waited {i}s on Loaded event.");
            Console.Error.WriteLine(EnhancedStackTrace.Current());
        }
        return true;
    }
}
