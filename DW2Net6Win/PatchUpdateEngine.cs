using System;
using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using JetBrains.Annotations;
using Xenko.Updater;

[PublicAPI]
[HarmonyPatch(typeof(UpdateEngine))]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "RedundantAssignment")]
public static class PatchUpdateEngine
{
    // this thing has invalid IL, just disable it
    [HarmonyPrefix]
    [HarmonyPatch(nameof(UpdateEngine.Run))]
    public static bool Run(object target, CompiledUpdate compiledUpdate, IntPtr updateData, UpdateObjectData[] updateObjects)
        => false;
}
