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
    [HarmonyPatch(nameof(Galaxy.Generate))]
    [HarmonyPrefix]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void PostfixGenerate(Galaxy __instance, GameStartSettings settings, int randomSeed, Game game, GameSettings gameSettings,
        bool previewMode, bool isBackgroundGalaxy)
    {

        foreach (var dataPath in ModLoader.ModManager.PatchedDataStack)
        {
            try
            {
                if (ModLoader.DebugMode)
                    Console.WriteLine($"Applying content patches from {dataPath}");
                GameDataDefinitionPatching.ApplyContentPatches(dataPath, __instance);
            }
            catch (Exception ex)
            {
                try
                {
                    Console.Error.WriteLine($"Failure applying content patches from {dataPath}");
                    ModLoader.ModManager.OnUnhandledException(ExceptionDispatchInfo.Capture(ex));
                }
                catch
                {
                    Console.Error.WriteLine("Failure reporting exception.");
                }
            }
        }
    }
}
