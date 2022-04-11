using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using DistantWorlds.Types;
using HarmonyLib;
using JetBrains.Annotations;

namespace DistantWorlds2.ModLoader;

[PublicAPI]
[HarmonyPatch(typeof(FleetTemplateList))]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public class PatchFleetTemplateList
{
    [HarmonyPatch(nameof(FleetTemplateList.LoadFromFile))]
    [HarmonyPrefix]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public bool PrefixLoadFromFile(Galaxy galaxy, bool overwriteAll, out FleetTemplateList? __state)
    {
        __state = overwriteAll ? galaxy.FleetTemplates : null;

        return true;
    }

    [HarmonyPatch(nameof(FleetTemplateList.LoadFromFile))]
    [HarmonyPostfix]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void PostfixLoadFromFile(Galaxy galaxy, bool overwriteAll, FleetTemplateList? __state)
    {
        if (__state is null || __state == galaxy.FleetTemplates) return;
        __state.Clear();
        __state.AddRange(galaxy.FleetTemplates);
        galaxy.FleetTemplates = __state;
    }
}
