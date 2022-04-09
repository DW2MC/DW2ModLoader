using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using HarmonyLib;
using JetBrains.Annotations;
using Xenko.Core.IO;

namespace DistantWorlds2.ModLoader;

[PublicAPI]
[HarmonyPatch(typeof(DatabaseFileProvider))]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public class PatchDatabaseFileProvider
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(DatabaseFileProvider.OpenStream))]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool OpenStream(ref Stream __result, string url, VirtualFileMode mode, VirtualFileAccess access, VirtualFileShare share,
        StreamFlags streamFlags)
    {
        if (!ModLoader.WaitForLoaded()) return true;

        foreach (var prefix in ModLoader.ModManager.OverrideAssetsStack)
        {
            var overrideUrl = url[0] == '/'
                ? $"{prefix}{url}"
                : $"{prefix}/{url}";

            if (!VirtualFileSystem.FileExists(overrideUrl))
                continue;

            if (ModLoader.DebugMode)
                Console.WriteLine($"Redirected asset {url} -> {overrideUrl}");
            __result = VirtualFileSystem.OpenStream(overrideUrl, mode, access, share);
            return false;

        }
        return true;
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(DatabaseFileProvider.ListFiles))]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ListFiles(ref string[]? __result, string url, string searchPattern, VirtualSearchOption searchOption)
    {
        if (!ModLoader.WaitForLoaded()) return;
        
        // TODO: fix searchPattern = "[^/]*" handling
        try
        {

            var files = new SortedSet<string>(__result ?? Enumerable.Empty<string>());

            foreach (var prefix in ModLoader.ModManager.OverrideAssetsStack)
            {
                var overrideUrl = url[0] == '/'
                    ? $"{prefix}{url}"
                    : $"{prefix}/{url}";

                var overrides = VirtualFileSystem.ListFiles(overrideUrl, searchPattern, searchOption).GetAwaiter().GetResult();

                foreach (var o in overrides)
                    files.Add(o.Substring(10));
            }

            __result = files.ToArray();
        }
        catch
        {
            // oof
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(DatabaseFileProvider.FileExists))]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool FileExists(ref bool __result, string url)
    {
        if (!ModLoader.WaitForLoaded()) return true;

        foreach (var prefix in ModLoader.ModManager.OverrideAssetsStack)
        {
            var overrideUrl = url[0] == '/'
                ? $"{prefix}{url}"
                : $"{prefix}/{url}";

            if (!VirtualFileSystem.FileExists(overrideUrl))
                continue;

            __result = true;
            return false;
        }
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(DatabaseFileProvider.FileSize))]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool FileSize(ref long __result, string url)
    {
        if (!ModLoader.WaitForLoaded()) return true;

        foreach (var prefix in ModLoader.ModManager.OverrideAssetsStack)
        {
            var overrideUrl = url[0] == '/'
                ? $"{prefix}{url}"
                : $"{prefix}/{url}";

            if (!VirtualFileSystem.FileExists(overrideUrl))
                continue;

            __result = VirtualFileSystem.FileSize(overrideUrl);
            return false;
        }
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(DatabaseFileProvider.GetAbsolutePath))]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool GetAbsolutePath(ref string __result, string url)
    {
        if (!ModLoader.WaitForLoaded()) return true;

        foreach (var prefix in ModLoader.ModManager.OverrideAssetsStack)
        {
            var overrideUrl = url[0] == '/'
                ? $"{prefix}{url}"
                : $"{prefix}/{url}";

            if (!VirtualFileSystem.FileExists(overrideUrl))
                continue;

            __result = VirtualFileSystem.GetAbsolutePath(overrideUrl);
            return false;
        }
        return true;
    }
}
