using System.Reflection;
using System.Runtime.ExceptionServices;

namespace DistantWorlds2.ModLoader;

public static class ModLoader
{
    public static IModManager ModManager { get; internal set; } = null!;

    public static IUnblocker Unblocker { get; internal set; } = null!;

    public static IPatches Patches { get; internal set; } = null!;


    public static event Action<ExceptionDispatchInfo>? UnhandledException;

    public static void OnUnhandledException(ExceptionDispatchInfo edi)
        => UnhandledException?.Invoke(edi);

    public static IHttpClientFactory HttpClientFactory { get; internal set; } = new DefaultHttpClientFactory();
}