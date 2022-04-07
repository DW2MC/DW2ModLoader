using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.Remoting.Contexts;
using HarmonyLib;
using JetBrains.Annotations;

namespace DistantWorlds2.ModLoader;

[PublicAPI]
public class Patches : IPatches
{
    private static readonly Harmony Harmony = new("DistantWorlds2.ModLoader.Patches");
    public void Run()
    {
        if (Harmony.HasAnyPatches("DistantWorlds2.ModLoader.Patches"))
        {
            Console.Error.WriteLine("DistantWorlds2.ModLoader.Patches already has patches applied!");
            return;
        }

        //PatchHarmonyLogging();

        if (ModLoader.DebugMode)
        {
            Environment.SetEnvironmentVariable("HARMONY_LOG_FILE", "CONOUT$");
            Harmony.DEBUG = true;
        }

        try
        {
            Harmony.PatchAll();
        }
        catch (Exception ex)
        {
            ModLoader.OnUnhandledException(ExceptionDispatchInfo.Capture(ex));
        }

        foreach (var method in Harmony.GetPatchedMethods())
        {
            Console.WriteLine($"Patched: {method.FullDescription()}");
            var info = Harmony.GetPatchInfo(method);
            Console.WriteLine($" - {string.Join(", ", info.Owners)}");
        }
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool HarmonyFileLogPatch(string str)
    {
        Console.WriteLine(new string(FileLog.indentChar, FileLog.indentLevel) + str);
        return false;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool HarmonyFileLogListPatch(List<string> strings)
    {
        foreach (var line in strings)
            Console.WriteLine(new string(FileLog.indentChar, FileLog.indentLevel) + line);
        return false;
    }

    public void PatchHarmonyLogging()
    {
        Console.WriteLine("Attempting to direct Harmony logging output to console.");
        Exception? exs = null;
        try
        {
            var fileLogType = typeof(FileLog);
            try
            {
                Harmony.Patch(fileLogType.TypeInitializer,
                    finalizer: new(typeof(Patches), nameof(FinalizerDiscard)));
            }
            catch (Exception ex)
            {
                exs = ex;
            }
            try
            {
                Harmony.Patch(fileLogType.GetMethod(nameof(FileLog.Log)),
                    new(typeof(Patches), nameof(HarmonyFileLogPatch)));
            }
            catch (Exception ex)
            {
                exs = exs is null ? ex : new AggregateException(exs, ex);
            }
            try
            {
                Harmony.Patch(fileLogType.GetMethod(nameof(FileLog.LogBuffered), new[] { typeof(string) }),
                    new(typeof(Patches), nameof(HarmonyFileLogPatch)));
            }
            catch (Exception ex)
            {
                exs = exs is null
                    ? ex
                    : exs is AggregateException ae
                        ? new(ae.InnerExceptions.Concat(new[] { ex }))
                        : new AggregateException(exs, ex);
            }
            try
            {
                Harmony.Patch(fileLogType.GetMethod(nameof(FileLog.LogBuffered), new[] { typeof(List<string>) }),
                    new(typeof(Patches), nameof(HarmonyFileLogListPatch)));
            }
            catch (Exception ex)
            {
                exs = exs is null
                    ? ex
                    : exs is AggregateException ae
                        ? new(ae.InnerExceptions.Concat(new[] { ex }))
                        : new AggregateException(exs, ex);
            }
            try
            {
                Harmony.Patch(fileLogType.GetMethod(nameof(FileLog.FlushBuffer)),
                    new(typeof(Patches), nameof(DoNothing)));
            }
            catch (Exception ex)
            {
                exs = exs is null
                    ? ex
                    : exs is AggregateException ae
                        ? new(ae.InnerExceptions.Concat(new[] { ex }))
                        : new AggregateException(exs, ex);
            }
            try
            {
                Harmony.Patch(fileLogType.GetMethod(nameof(FileLog.Reset)),
                    new(typeof(Patches), nameof(DoNothing)));
            }
            catch (Exception ex)
            {
                exs = exs is null
                    ? ex
                    : exs is AggregateException ae
                        ? new(ae.InnerExceptions.Concat(new[] { ex }))
                        : new AggregateException(exs, ex);
            }

            if (exs is not null) throw exs;

            FileLog.Log("Logging Harmony output to console success.");
            FileLog.FlushBuffer();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Logging Harmony output to console failed.");
            ModLoader.OnUnhandledException(ExceptionDispatchInfo.Capture(ex));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool DoNothing()
        => false;

    [MethodImpl(MethodImplOptions.NoInlining)]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static Exception? FinalizerDiscard(Exception __exception)
        => null;
}
