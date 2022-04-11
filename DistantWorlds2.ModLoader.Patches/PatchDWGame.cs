/* maybe just disable MessageLog.xml?
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using DistantWorlds.Types;
using HarmonyLib;
using JetBrains.Annotations;
using Xenko.Core.IO;

namespace DistantWorlds2.ModLoader;

[PublicAPI]
[HarmonyPatch(typeof(DWGame))]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public class PatchDWGame
{
    [HarmonyPatch(nameof(DWGame.SaveGame))]
    [HarmonyPrefix]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool PrefixSaveGame(DWGame __instance, out bool __state)
    {
        if (!ModLoader.IsIsolated)
        {
            __state = false;
            return true;
        }

        __state = __instance.SaveMessageLog;
        __instance.SaveMessageLog = false;
        return true;
    }

    [HarmonyPatch(nameof(DWGame.SaveGame))]
    [HarmonyPostfix]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void PostfixSaveGame(DWGame __instance, bool __state, ref Galaxy ____Galaxy)
    {
        if (!__state) return;

        using var s = VirtualFileSystem.ApplicationLocal.OpenStream("MessageLog.xml", VirtualFileMode.Create, VirtualFileAccess.ReadWrite);
        EmpireMessageList.XmlSerializer.WriteXml(s, ____Galaxy.Empires.GetPlayer().Messages);
    }
}
*/