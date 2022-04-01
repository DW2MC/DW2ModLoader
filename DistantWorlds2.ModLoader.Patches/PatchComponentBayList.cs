using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using DistantWorlds.Types;
using HarmonyLib;
using JetBrains.Annotations;
using Xenko.Core.IO;
using Xenko.Core.Serialization.Contents;
using Xenko.Engine;
using Xenko.Graphics;
using ReflectionHelper = MonoMod.Utils.ReflectionHelper;

namespace DistantWorlds2.ModLoader;

[PublicAPI]
[HarmonyPatch(typeof(ModelEffectHelper))]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public static class PatchShipHull
{
    [HarmonyPatch(nameof(ModelEffectHelper.ProcessShipHullModel))]
    [HarmonyTranspiler]
    [SuppressMessage("ReSharper", "CommentTypo")]
    [MethodImpl(MethodImplOptions.NoInlining)]
    static IEnumerable<CodeInstruction> TranspileProcessShipHullModel(IEnumerable<CodeInstruction> instructions)
    {
        using var e = instructions.GetEnumerator();

        while (e.MoveNext())
        {
            var instr = e.Current!;
            // ReSharper disable once ObjectCreationAsStatement
            if (instr.Is(OpCodes.Newobj, ReflectionUtils.Constructor(() => new ComponentBayList())))
            {
                yield return new(OpCodes.Ldarg_0); // ldarg.0 // ShipHull shipHull
                yield return new(OpCodes.Ldfld, ReflectionUtils<ShipHull>.Field(sh => sh.ComponentBays));
                break;
            }

            yield return instr;
        }

        // TODO: RunningLightList, EmitterList, ModelModuleList, ModelVertexData

        while (e.MoveNext())
            yield return e.Current!;

    }
}

[PublicAPI]
[HarmonyPatch(typeof(ComponentBayList))]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public static class PatchComponentBayList
{
    [HarmonyPatch(nameof(ComponentBayList.ReadFromStream))]
    [HarmonyPrefix]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool PrefixReadFromStream(ComponentBayList __instance, BinaryReader reader)
    {
        ComponentBay ReadComponentBay(byte id)
        {
            var meshInfoList = new MeshInfoList();
            var weaponBayFiringArc = new WeaponBayFiringArc();
            var cb = new ComponentBay
            {
                ComponentBayId = id,
                Type = (ComponentBayType)reader.ReadByte(),
                MaximumComponentSize = reader.ReadInt32(),
                MeshName = SerializationHelper.ReadStringShort(reader),
                RotationHalfArcRange = reader.ReadSingle(),
                DisplayEffectRescaleFactor = SerializationHelper.ReadVector3(reader),
                DisplayEffectOffset = SerializationHelper.ReadVector3(reader),
                Meshes = meshInfoList.ReadFromStream(reader),
                WeaponBayFiringArc = weaponBayFiringArc.ReadFromStream(reader)
            };
            return cb;
        }

        static void SetIfDefault<T>(ref T r, T value)
        {
            if (EqualityComparer<T>.Default.Equals(default!, r))
                r = value;
        }

        var count = (int)reader.ReadByte();

        if (__instance.Count == 0)
        {
            for (var i = 0; i < count; i++)
                __instance.Add(ReadComponentBay(reader.ReadByte()));
            return false;
        }

        for (var i = 0; i < count; i++)
        {
            var id = reader.ReadByte();
            var cb = __instance.FirstOrDefault(x => x.ComponentBayId == id);
            if (cb is not null)
            {
                var meshInfoList = new MeshInfoList();
                var cbWeaponBayFiringArc = new WeaponBayFiringArc();
                SetIfDefault(ref cb.Type, (ComponentBayType)reader.ReadByte());
                SetIfDefault(ref cb.MaximumComponentSize, reader.ReadInt32());
                SetIfDefault(ref cb.MeshName, SerializationHelper.ReadStringShort(reader));
                SetIfDefault(ref cb.RotationHalfArcRange, reader.ReadSingle());
                SetIfDefault(ref cb.DisplayEffectRescaleFactor, SerializationHelper.ReadVector3(reader));
                SetIfDefault(ref cb.DisplayEffectOffset, SerializationHelper.ReadVector3(reader));
                cb.Meshes = meshInfoList.ReadFromStream(reader);
                cb.WeaponBayFiringArc = cbWeaponBayFiringArc.ReadFromStream(reader);
            }
            else
                __instance.Add(ReadComponentBay(id));
        }
        return false;
    }
}
