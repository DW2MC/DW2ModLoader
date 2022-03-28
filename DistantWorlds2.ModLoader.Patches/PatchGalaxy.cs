using System.Diagnostics.CodeAnalysis;
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
    [HarmonyPatch(nameof(Galaxy.Generate))]
    [HarmonyPrefix]
    public static void PostfixGenerate(Galaxy __instance, GameStartSettings settings, int randomSeed, Game game, GameSettings gameSettings,
        bool previewMode, bool isBackgroundGalaxy)
    {

        foreach (var dataPath in ModLoader.ModManager.PatchedDataQueue)
        {
            try
            {
                GameDataDefinitionPatching.ApplyContentPatches(dataPath, __instance);
            }
            catch (Exception ex)
            {
                ModLoader.ModManager.OnUnhandledException(ExceptionDispatchInfo.Capture(ex));
            }
        }
    }
}
