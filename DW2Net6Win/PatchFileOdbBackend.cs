using HarmonyLib;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[PublicAPI]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "RedundantAssignment")]
[HarmonyPatch(typeof(Xenko.Core.Storage.FileOdbBackend), MethodType.Constructor, new[] { typeof(string), typeof(string), typeof(bool) })]
public static class PatchFileOdbBackend
{
    public static bool Prefix(string vfsRootUrl, string indexName, ref bool isReadOnly)
    {
        if (!isReadOnly)
        {
            var writeablePaths = new[] { "/local", "/cache", "/tmp", "/roaming" };

            foreach (var path in writeablePaths)                
            {
                if (vfsRootUrl.StartsWith(path))
                    return true;
            }
            isReadOnly = true;
        }
        return true;
    }
}