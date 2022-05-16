using System.Diagnostics.CodeAnalysis;
using DistantWorlds.Types;
using DistantWorlds.UI;
using HarmonyLib;
using JetBrains.Annotations;

namespace DistantWorlds2.ModLoader;

[PublicAPI]
[HarmonyPatch(typeof(StartNewGameDialog), nameof(StartNewGameDialog.DisableUnavailableRaces))]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public class EnableCustomRacesPatch
{
    [HarmonyPrefix]
    public static bool Prefix()
    {
        return false;
    }
}

[HarmonyPatch(typeof(StartNewGameDialog), nameof(StartNewGameDialog.RaceChanged))]
public class HandleNonExistantRaceCrash
{
    [HarmonyPrefix]
    public static bool Prefix(StartNewGameDialog __instance, ref Race ____SelectedRace, ScrollablePanel ___RaceSummary, ToggleButtonGroup ___Race, object sender, DWEventArgs args)
    {
        if (___Race.ToggledButtonIndex > 0)
            return true;

        __instance.YourEmpireGovernment_IndexSelected(-1);
        ____SelectedRace = null;
        ___RaceSummary.Clear();

        return false;
    }
}
