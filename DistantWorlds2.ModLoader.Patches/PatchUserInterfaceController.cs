/* maybe redirect some save/load file stuff to local instead of data?
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using DistantWorlds.Types;
using DistantWorlds.UI;
using HarmonyLib;
using JetBrains.Annotations;
using Xenko.Core.IO;

namespace DistantWorlds2.ModLoader;

[PublicAPI]
[HarmonyPatch(typeof(UserInterfaceController))]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public class PatchUserInterfaceController
{
    // UserInterfaceController.ShowFileDialogCentered
    [HarmonyPatch(nameof(UserInterfaceController.ShowFileDialogCentered))]
    [HarmonyPrefix]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool PrefixShowFileDialogCentered(string title, string text, ref IVirtualFileProvider fileProvider, string subFolder,
        FileType fileTypeFilter)
    {
        if (!ModLoader.IsIsolated) return true;

        switch (fileTypeFilter)
        {
            case FileType.FleetTemplates:
            case FileType.Designs: {
                if (fileProvider == VirtualFileSystem.ApplicationData)
                    fileProvider = VirtualFileSystem.ApplicationLocal;
                break;
            }
        }

        return true;
    }
}
*/