using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using DistantWorlds.Types;
using DistantWorlds.UI;
using HarmonyLib;
using JetBrains.Annotations;
using Xenko.Core.IO;
using Xenko.Input;

namespace DistantWorlds2.ModLoader;

[PublicAPI]
[HarmonyPatch(typeof(InputSystem))]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public static class PatchInputSystem
{
    [HarmonyPatch(nameof(InputSystem.Update))]
    [HarmonyPostfix]
    public static void PostfixInputSystemUpdate(InputSystem __instance, InputManager inputManager)
    {
        if (inputManager.HasPressedKeys)
        {
            if (inputManager.DownKeys.Contains(Keys.LeftCtrl) || inputManager.DownKeys.Contains(Keys.RightCtrl))
            {
                if (inputManager.PressedKeys.Contains(Keys.F5))
                {
                    DWGame game = (DWGame)ModLoader.ModManager.Game;
                    var galaxy = game.Galaxy;
                    if (galaxy != null)
                    {
                        Galaxy.LoadStaticBaseData("");
                        galaxy.ReloadComponentsAndShipHulls(game, Galaxy.Assets, game.GraphicsDevice, galaxy);
                        galaxy.ReloadResearch(galaxy, galaxy.GameStartSettings.GalaxyTechLevel, false);
                        galaxy.LoadImageForAllItems(game, Galaxy.Assets);
                        GameDataDefinitionPatching.ApplyContentPatches();
                        //GameDataDefinitionPatching.ApplyLateContentPatches(galaxy);
                        Galaxy.CopyStaticBaseDataToGalaxyInstance(galaxy);
                        //GameDataDefinitionPatching.ApplyDynamicDefinitions(galaxy);
                        galaxy.GenerateResearchAllEmpires(galaxy.ResearchProjects, galaxy.GameStartSettings.GalaxyTechLevel, false);
                        foreach (var empire in galaxy.Empires)
                        {
                            foreach (var project in empire.Research.Projects)
                            {
                                project.Visible = true;
                            }
                        }
                    }
                }
                if (inputManager.PressedKeys.Contains(Keys.F6))
                {                    
                    UserInterfaceController.ResearchScreen.GodMode = !UserInterfaceController.ResearchScreen.GodMode;
                }
            }
        }
    }

    [PublicAPI]
    [HarmonyPatch(typeof(ResearchTree))]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static class PatchBuggyLoopCondition
    {
        public static void LoopReplacement(List<bool> bools, ResearchProject project)
        {
            bools.AddRange(Enumerable.Repeat(false, (int)project.Definition.Row - bools.Count + 2));
        }

        [HarmonyPatch("ResolveVisibleProjectRows")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> ReplaceBadLoop(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo replacementMI = AccessTools.Method(typeof(PatchBuggyLoopCondition), nameof(PatchBuggyLoopCondition.LoopReplacement));
            MethodInfo countMI = AccessTools.Method(typeof(List<bool>), "get_Count");
            FieldInfo rowFI = AccessTools.Field(typeof(ResearchProjectDefinition), nameof(ResearchProjectDefinition.Row));

            var code = instructions.ToList();
            for(int i = 0; i < code.Count; i++)
            {
                if (code[i].LoadsField(rowFI) &&
                    /*code[i + 1].IsLdloc() &&*/
                    code[i + 2].Calls(countMI) &&
                    /*code[i + 4].Is(OpCodes.Sub) &&*/
                    code[i + 5].LoadsConstant(2.0f)
                    )
                {
                    code[i - 14] = new CodeInstruction(OpCodes.Ldloc_0);
                    code[i - 13] = code[i - 2];
                    code[i - 12] = new CodeInstruction(OpCodes.Call, replacementMI);
                    for(int j = i - 11; j <= i + 7; j++)
                    {
                        code[j] = new CodeInstruction(OpCodes.Nop);
                    }
                }
            }
            return code.AsEnumerable();
        }        
    }
}