using System.Diagnostics.CodeAnalysis;
using System.IO;
using HarmonyLib;
using JetBrains.Annotations;
using Xenko.Core.IO;

[PublicAPI]
[HarmonyPatch(typeof(FileSystemProvider))]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "RedundantAssignment")]
public static class PatchFileSystemProvider
{
    // this thing has invalid IL, just disable it
    [HarmonyPrefix]
    [HarmonyPatch(nameof(FileSystemProvider.OpenStream))]
    public static bool OpenStream(ref Stream __result,
        string url,
        VirtualFileMode mode,
        ref VirtualFileAccess access,
        VirtualFileShare share,
        StreamFlags streamFlags)
    {
        if ((access & VirtualFileAccess.Write) != 0)
            if (url.StartsWith(@"ShipHullModelData"))
                access &= ~VirtualFileAccess.Write;

        return true;
    }
}
