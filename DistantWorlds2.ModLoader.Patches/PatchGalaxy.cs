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
    [HarmonyPostfix]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void PostfixGenerate(Galaxy __instance, GameStartSettings settings, int randomSeed, Game game, GameSettings gameSettings,
        bool previewMode, bool isBackgroundGalaxy)
    {
        if (!ModLoader.MaybeWaitForLoaded()) return;

        GameDataDefinitionPatching.ApplyDynamicDefinitions(__instance);
    }

    [HarmonyPatch(nameof(Galaxy.LoadShipHullModelData))]
    [HarmonyPrefix]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool PrefixLoadShipHullModelData(bool ____ShipHullModelDataLoaded)
    {
        Console.WriteLine($"Loading ShipHullModelData with ShipHullModelDataLoaded == {____ShipHullModelDataLoaded}");
        return true;
    }

    [HarmonyPatch(nameof(Galaxy.ReloadComponentsAndShipHulls))]
    [HarmonyPrefix]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool PrefixReloadComponentsAndShipHulls()
    {
        Console.WriteLine($"ReloadComponentsAndShipHulls called.");
        return true;
    }
}
