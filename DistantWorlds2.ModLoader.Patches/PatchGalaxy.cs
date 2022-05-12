using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using DistantWorlds.Types;
using HarmonyLib;
using JetBrains.Annotations;
using Xenko.Engine;

namespace DistantWorlds2.ModLoader;

[PublicAPI]
[HarmonyPatch(typeof(Galaxy))]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public static class PatchGalaxy
{
    [HarmonyPatch(nameof(Galaxy.LoadApplyStaticBaseDataVariable))]
    [HarmonyPostfix]
    public static void PostfixLoadApplyStaticBaseDataVariable(Galaxy galaxy)
    {
        GameDataDefinitionPatching.ApplyLateContentPatches(galaxy);
    }

    [HarmonyPatch(nameof(Galaxy.Generate))]
    [HarmonyPrefix]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void PostfixGenerate(Galaxy __instance, GameStartSettings settings, int randomSeed, Game game, GameSettings gameSettings,
        bool previewMode, bool isBackgroundGalaxy)
    {
        if (!ModLoader.MaybeWaitForLoaded()) return;

        GameDataDefinitionPatching.ApplyDynamicDefinitions(__instance);
    }
}
