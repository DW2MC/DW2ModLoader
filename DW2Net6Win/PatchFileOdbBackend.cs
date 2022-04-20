using HarmonyLib;
using JetBrains.Annotations;
using System.Diagnostics.CodeAnalysis;

[PublicAPI]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "RedundantAssignment")]
[HarmonyPatch(typeof(Xenko.Core.Storage.FileOdbBackend))]
public static class PatchFileOdbBackend
{
    [HarmonyPatch(MethodType.Constructor, typeof(string), typeof(string), typeof(bool))]
    public static bool Prefix(string vfsRootUrl, string indexName, ref bool isReadOnly)
    {
        if (isReadOnly) return true;

        var writeablePaths = new[] { "/local", "/cache", "/tmp", "/roaming" };

        foreach (var path in writeablePaths)
        {
            if (vfsRootUrl.StartsWith(path))
                return true;
        }

        isReadOnly = true;
        return true;
    }
}
