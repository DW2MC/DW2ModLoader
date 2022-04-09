using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using DistantWorlds.Types;
using HarmonyLib;
using JetBrains.Annotations;

namespace DistantWorlds2.ModLoader;

[PublicAPI]
[HarmonyPatch(typeof(ResearchProjectList))]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public class PatchResearchProjectList
{
    //EvaluateProjectPaths(ResearchProject sourceProject, Random rnd, byte raceId, ShipHullList shipHulls, PlanetaryFacilityDefinitionList planetaryFacilities, TroopDefinitionList troops, bool includeAllPaths, ref ResearchProjectList projects, ref ResearchProjectList unreachableProjects)

    [HarmonyPatch(nameof(ResearchProjectList.EvaluateProjectPaths))]
    [HarmonyTranspiler]
    [SuppressMessage("ReSharper", "CommentTypo")]
    [MethodImpl(MethodImplOptions.NoInlining)]
    static IEnumerable<CodeInstruction> TranspileEvaluateProjectPaths(IEnumerable<CodeInstruction> instructions, MethodBase original)
    {
        var mb = original.GetMethodBody();
        var lvs = mb!.LocalVariables;
        var typeResPath = typeof(ResearchPath);
        var lvResearchPaths = lvs.Where(lv => lv.LocalType == typeResPath).ToArray();

        if (lvResearchPaths.Length != 2)
            throw new NotImplementedException(
                $"Can't apply EvaluateProjectPaths patch! Expected exactly 2 ResearchPath variables, found {lvResearchPaths.Length}.");

        var ciArray = instructions as CodeInstruction[] ?? instructions.ToArray();

        var ldResearchProjectIds = new List<int>(2);

        for (var i = 0; i < ciArray.Length; i++)
        {
            var ci = ciArray[i];
            if (ci.operand is FieldInfo fi
                && ci.opcode.Value == OpCodes.Ldfld.Value
                && fi.Name == nameof(ResearchPath.ResearchProjectId)
                && fi.ReflectedType == typeResPath)
                ldResearchProjectIds.Add(i);
        }

        if (ldResearchProjectIds.Count < 2)
        {
            throw new NotImplementedException(
                @$"Can't apply EvaluateProjectPaths patch! Expected at least 2 ldfld ResearchProjectId, found {ldResearchProjectIds.Count}.");
        }

        var hitCount = 0;

        foreach (var ldFldIndex in ldResearchProjectIds)
        {
            ref var ldFld = ref ciArray[ldFldIndex];
            ref var ldLocS = ref ciArray[ldFldIndex - 1];
            if (ldLocS.opcode.Value != OpCodes.Ldloc_S.Value
                || ldLocS.operand is not LocalBuilder lb
                || lb.LocalType != typeResPath)
                continue;
            ref var ldIndRef = ref ciArray[ldFldIndex - 2];
            if (ldIndRef.opcode.Value != OpCodes.Ldind_Ref.Value)
                continue;
            ref var ldArgS = ref ciArray[ldFldIndex - 3];
            if (ldArgS.opcode.Value != OpCodes.Ldarg_S.Value)
                continue;
            // ReSharper disable once IdentifierTypo
            ref var callVirt = ref ciArray[ldFldIndex + 1];
            if (callVirt.opcode.Value != OpCodes.Callvirt.Value
                || callVirt.operand is not MethodBase { Name: nameof(IndexedList<object>.ContainsId) })
                continue;
            ref var brTrue = ref ciArray[ldFldIndex + 2];
            if (brTrue.opcode.Value != OpCodes.Brtrue_S.Value)
                continue;
            ++hitCount;
            if (hitCount > 2)
                throw new NotImplementedException("Can't apply EvaluateProjectPaths patch! Found more than two instruction sequence matches!");

            ldArgS = new(OpCodes.Nop);
            ldIndRef = new(OpCodes.Nop);
            ldLocS = new(OpCodes.Nop);
            ldFld = new(OpCodes.Nop);
            callVirt = new(OpCodes.Nop);
            brTrue = new(OpCodes.Nop);
        }

        return ciArray;
    }
}
