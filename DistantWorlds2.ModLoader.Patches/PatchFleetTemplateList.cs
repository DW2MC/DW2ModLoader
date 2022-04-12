using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using DistantWorlds.Types;
using DistantWorlds.UI;
using HarmonyLib;
using JetBrains.Annotations;
using Xenko.Core.IO;

namespace DistantWorlds2.ModLoader;

[PublicAPI]
[HarmonyPatch(typeof(FleetTemplateList))]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public class PatchFleetTemplateList
{
    static int FTIDENT = 'F' << 24 | 'T' << 16 | 'V' << 8 | '2';

    [HarmonyPatch(typeof(ScaledRenderer),nameof(ScaledRenderer.ShowSaveFleetTemplates))]
    [HarmonyPrefix]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool ShowSaveFleetTemplates(ScaledRenderer __instance)
    {
      string text = TextResolver.GetText("Save Fleet Templates Explanation");
      UserInterfaceController.ShowFileDialogCentered(null, TextResolver.GetText("Save Fleet Templates"), text, true, VirtualFileSystem.ApplicationData, "FleetTemplates", FileType.FleetTemplates, new DWButtonData(string.Empty, TextResolver.GetText("Save"), new EventHandler<DWEventArgs>(__instance.SaveFleetTemplatesDialogClick), null, null, UserInterfaceHelper.IconImages["SaveGame"]), null, new DWButtonData(string.Empty, TextResolver.GetText("Cancel"), new EventHandler<DWEventArgs>(__instance.CancelSaveFleetTemplatesClick), null), __instance.Width, __instance.Height, UserInterfaceHelper.LoadSaveDialogSize, string.Empty);
      return false;
    }

    [HarmonyPatch(typeof(ScaledRenderer), nameof(ScaledRenderer.ShowLoadFleetTemplates))]
    [HarmonyPrefix]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool ShowLoadFleetTemplates(ScaledRenderer __instance)
    {
        string text = TextResolver.GetText("Load Fleet Templates Explanation");
        UserInterfaceController.ShowFileDialogCentered(null, TextResolver.GetText("Load Fleet Templates"), text, true, VirtualFileSystem.ApplicationData, "FleetTemplates", FileType.FleetTemplates, new DWButtonData(string.Empty, TextResolver.GetText("Load"), new EventHandler<DWEventArgs>(__instance.LoadFleetTemplatesDialogClick), null, null, UserInterfaceHelper.IconImages["LoadGame"]), null, new DWButtonData(string.Empty, TextResolver.GetText("Cancel"), new EventHandler<DWEventArgs>(__instance.CancelLoadFleetTemplatesClick), null), __instance.Width, __instance.Height, UserInterfaceHelper.LoadSaveDialogSize, string.Empty);
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ScaledRenderer), nameof(ScaledRenderer.SaveFleetTemplatesDialogClick))]
    public static bool SaveFleetTemplatesDialogClick(ScaledRenderer __instance, Galaxy ____Galaxy, Empire ____PlayerEmpire, object sender, DWEventArgs args)
    {
        if (args.ExtraData is DWFile file)
        {
            DesignTemplateList usedTemplates = new DesignTemplateList();
            var usedDesignIDs = ____PlayerEmpire.FleetTemplates.SelectMany(x => x.DesignIdsPerRole).Distinct();
            var usedDesigns = ____PlayerEmpire.Designs.Where(x => usedDesignIDs.Contains(x.DesignId)).ToList();            
            
            using (Stream output = VirtualFileSystem.ApplicationData.OpenStream(file.FullPath, VirtualFileMode.Create, VirtualFileAccess.ReadWrite))
            {
                using (BinaryWriter writer = new BinaryWriter(output))
                {
                    writer.Write(FTIDENT);
                    ____PlayerEmpire.FleetTemplates.WriteToStream(writer);
                    writer.Write(usedDesigns.Count);
                    usedDesigns.ForEach(x => x.WriteToStream(writer));
                }
            }
            __instance.ShowFleetTemplatesDialog(null);
        }
        UserInterfaceController.HideFileDialog();
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ScaledRenderer), nameof(ScaledRenderer.LoadFleetTemplatesDialogClick))]
    public static bool LoadFleetTemplatesDialogClick(ScaledRenderer __instance, Galaxy ____Galaxy, Empire ____PlayerEmpire, object sender, DWEventArgs args)
    {
        if (args.ExtraData != null && args.ExtraData is DWFile)
        {
            DWFile extraData = (DWFile)args.ExtraData;
            if (VirtualFileSystem.ApplicationData.FileExists(extraData.FullPath))
            {
                FleetTemplateList.LoadFromFile(____Galaxy, ____PlayerEmpire, extraData.FullPath, false);
                DWRendererBase.FleetTemplateListDialog.SetSourceFleetTemplates(____Galaxy.FleetTemplates);
                __instance.ShowFleetTemplatesDialog(null);
            }
        }
        UserInterfaceController.HideFileDialog();
        return false;
    }

    [HarmonyPatch(nameof(FleetTemplateList.LoadFromFile))]
    [HarmonyPrefix]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool PrefixLoadFromFile(Galaxy galaxy, Empire empire, string filepath, bool overwriteAll)
    {
        if (overwriteAll)
            return true;
        
        if (VirtualFileSystem.ApplicationData.FileExists(filepath))
        {
            using (Stream input = VirtualFileSystem.ApplicationData.OpenStream(filepath, VirtualFileMode.Open, VirtualFileAccess.Read))
            {
                using (BinaryReader reader = new BinaryReader(input))
                {
                    int header = reader.ReadInt32();
                    if(header != FTIDENT)
                    {
                        Console.WriteLine("Error importing Fleet Templates.");
                        return false;
                    }

                    var loadedTemplates = new FleetTemplateList();
                    loadedTemplates.ReadFromStream(reader);
                    var loadedDesigns = new List<Design>(reader.ReadInt32());
                    Dictionary<int, int> designIdMap = new Dictionary<int, int>(loadedDesigns.Capacity+1);
                    designIdMap[-1] = -1;

                    for (int i = 0; i < loadedDesigns.Capacity; i++)
                    {
                        Design design = new Design();
                        design.ReadFromStream(galaxy, reader, false);

                        
                        var existing = empire.Designs.FirstOrDefault(x => x.IsEquivalent(design, galaxy));
                        if (existing != null)
                        {
                            designIdMap[design.DesignId] = existing.DesignId;
                        }
                        else if (design.GetShipHull(galaxy).RaceId != empire.DominantRaceId)
                        {
                            //No point importing/using designs you can't ever build?
                            designIdMap[design.DesignId] = -1;
                        }
                        else
                        {
                            var newID = galaxy.Designs.GetNextId();
                            designIdMap[design.DesignId] = newID;
                            design.DesignId = designIdMap[design.DesignId];                            
                            design.ResetAsNew(galaxy, empire);
                            galaxy.Designs.Add(design);
                            empire.Designs.Add(design);                            
                        }                        
                    }

                    FleetTemplateList playerTemplates = empire.FleetTemplates;
                    FleetTemplateList galaxyTemplates = galaxy.FleetTemplates;                    

                    foreach (FleetTemplate template in loadedTemplates)
                    {
                        for(int i = 0; i < template.DesignIdsPerRole.Length; i++)
                        {
                            template.DesignIdsPerRole[i] = designIdMap[template.DesignIdsPerRole[i]];
                        }

                        var oldIndex = playerTemplates.FindIndex(x => x.Name == template.Name);
                        //Template with the same name already exists, overwrite?!
                        if (oldIndex != -1)
                        {
                            var globalOldIndex = galaxyTemplates.IndexOf(playerTemplates[oldIndex]);
                            var templateID = playerTemplates[oldIndex].FleetTemplateId;
                            template.FleetTemplateId = templateID;
                            playerTemplates[oldIndex] = template;
                            galaxyTemplates[globalOldIndex] = template;
                            //TODO: Fleets using this template probably need to be told to update?
                        }
                        else
                        {
                            template.FleetTemplateId = (short)galaxyTemplates.GetNextId();
                            playerTemplates.Add(template);
                            galaxyTemplates.Add(template);
                        }
                    }
                }
            }
        }
        return false;
    }
}