using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using JetBrains.Annotations;

namespace DistantWorlds2.ModLoader;

[PublicAPI]
[HarmonyPatch(typeof(DWGame))]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public static class PatchRemoveSessionActive
{
    [HarmonyPatch(nameof(DWGame.CreateSessionFile))]
    [HarmonyPrefix]
    public static bool PrefixCreateSessionFile()
    {
        return false;
    }

    [HarmonyPatch(nameof(DWGame.CheckSessionActive))]
    [HarmonyPrefix]
    public static bool PrefixCheckSessionActive(ref bool __result)
    {
        __result = false;
        return false;
    }

    [HarmonyPatch(nameof(DWGame.RemoveSessionFile))]
    [HarmonyPrefix]
    public static bool PrefixRemoveSessionFile()
    {
        return false;
    }
}
