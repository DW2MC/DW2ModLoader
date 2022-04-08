using HarmonyLib;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;


[PublicAPI]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "RedundantAssignment")]
[HarmonyPatch(typeof(Xenko.Core.Storage.ObjectDatabase), MethodType.Constructor, new[] { typeof(string), typeof(string), typeof(string), typeof(bool) })]
public static class PatchObjectDatabase
{
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> ForceReadonlyDatabase(IEnumerable<CodeInstruction> instructions)
    {
        var code = instructions.ToList();
        for (int i = 0; i < code.Count; i++)
        {
            if (code[i].opcode == OpCodes.Stloc_0)
            {
                code[i - 1] = new CodeInstruction(OpCodes.Nop);
                code[i - 2] = new CodeInstruction(OpCodes.Nop);
                code[i - 3] = new CodeInstruction(OpCodes.Nop);
                code[i - 5] = new CodeInstruction(OpCodes.Nop);
            }
        }
        return code.AsEnumerable();
    }
}

