using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using HarmonyLib;
using JetBrains.Annotations;
using Microsoft.Win32.SafeHandles;

[PublicAPI]
[HarmonyPatch(typeof(RandomAccess))]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public static class PatchRandomAccess
{
    [HarmonyFinalizer]
    [HarmonyPatch("ReadAtOffset")]
    public static Exception? ReadAtOffset(Exception? __exception, ref int __result, SafeFileHandle handle, Span<byte> buffer, long fileOffset)
    {
        if (__exception is null)
            return null;

        if (__exception.Message.StartsWith("The operation completed successfully."))
            return null;

        return __exception;
    }
}
