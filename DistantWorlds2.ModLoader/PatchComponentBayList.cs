using System.Collections;
using System.Diagnostics.CodeAnalysis;
using DistantWorlds.Types;
using HarmonyLib;
using JetBrains.Annotations;
using Xenko.Core.IO;
using ReflectionHelper = MonoMod.Utils.ReflectionHelper;

namespace DistantWorlds2.ModLoader;

[PublicAPI]
[HarmonyPatch(typeof(ComponentBayList))]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public static class PatchComponentBayList
{
    [HarmonyPatch(nameof(ComponentBayList.ReadFromStream))]
    [HarmonyPrefix]
    public static bool PrefixReadFromStream(ComponentBayList __instance, BinaryReader reader)
    {
        var count = (int)reader.ReadByte();
        for (var i = 0; i < count; i++)
        {
            var id = reader.ReadByte();
            var cb = __instance.FirstOrDefault(x => x.ComponentBayId == id);
            if (cb is null)
            {
                // discard
                reader.ReadByte();
                reader.ReadInt32();
                SerializationHelper.ReadStringShort(reader);
                reader.ReadSingle();
                SerializationHelper.ReadVector3(reader);
                SerializationHelper.ReadVector3(reader);
                new MeshInfoList().ReadFromStream(reader);
                new WeaponBayFiringArc().ReadFromStream(reader);
            }
            else
            {
                reader.ReadByte(); // (ComponentBayType) Type
                reader.ReadInt32(); // MaximumComponentSize
                SerializationHelper.ReadStringShort(reader); // MeshName
                reader.ReadSingle(); // RotationHalfArcRange
                SerializationHelper.ReadVector3(reader); // DisplayEffectRescaleFactor
                SerializationHelper.ReadVector3(reader); // DisplayEffectOffset
                cb.Meshes.ReadFromStream(reader);
                var weaponBayFiringArc = new WeaponBayFiringArc();
                weaponBayFiringArc = weaponBayFiringArc.ReadFromStream(reader);
                cb.WeaponBayFiringArc = weaponBayFiringArc;
            }
        }
        return false;
    }
}
