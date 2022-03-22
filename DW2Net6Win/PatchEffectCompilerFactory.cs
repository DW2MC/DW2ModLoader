using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using JetBrains.Annotations;
using Xenko.Core.IO;
using Xenko.Engine.Design;
using Xenko.Rendering;
using Xenko.Shaders.Compiler;

[PublicAPI]
[HarmonyPatch(typeof(EffectCompilerFactory))]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public static class PatchEffectCompilerFactory
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(EffectCompilerFactory.CreateEffectCompiler))]
    public static bool CreateEffectCompiler(ref IEffectCompiler __result, ref IVirtualFileProvider fileProvider, ref EffectSystem effectSystem,
        ref string packageName, ref EffectCompilationMode effectCompilationMode, ref bool recordEffectRequested,
        ref TaskSchedulerSelector taskSchedulerSelector)
    {
        effectCompilationMode &= ~EffectCompilationMode.Remote;
        recordEffectRequested = false;
        return true;
    }
}