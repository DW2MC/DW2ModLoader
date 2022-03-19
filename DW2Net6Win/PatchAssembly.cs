using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using HarmonyLib;

[HarmonyPatch(typeof(Assembly))]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public static class PatchAssembly
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(Assembly.GetEntryAssembly))]
    public static bool GetEntryAssembly(ref Assembly __result)
    {
        __result = Program.EntryAssembly;

        return false;
    }
}