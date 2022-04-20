using System.Diagnostics.CodeAnalysis;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using DistantWorlds.Types;
using HarmonyLib;
using JetBrains.Annotations;

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

        try
        {
            var count = (int)reader.ReadByte();

            if (__instance.Count == 0)
            {
                for (var i = 0; i < count; i++)
                {
                    try
                    {
                        __instance.Add(ReadComponentBay(reader.ReadByte()));
                    }
                    catch (Exception ex)
                    {
                        ModLoader.OnUnhandledException(ExceptionDispatchInfo.Capture(ex));
                    }
                }
                try
                {
                    __instance.RebuildIndexes();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed to rebuild indexes for component bays!");
                    ModLoader.OnUnhandledException(ExceptionDispatchInfo.Capture(ex));
                }
                return false;
            }

            if (__instance.Count > 1)
                try
                {
                    __instance.RebuildIndexes();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed to rebuild indexes for component bays!");
                    ModLoader.OnUnhandledException(ExceptionDispatchInfo.Capture(ex));
                }

            for (var i = 0; i < count; i++)
            {
                var id = reader.ReadByte();
                //__instance.RebuildIndexes();
                var cb = __instance.GetBayById(id);
                //var cb = __instance.FirstOrDefault(x => x.ComponentBayId == id);
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
                    try
                    {
                        __instance.Add(ReadComponentBay(id));
                    }
                    catch (Exception ex)
                    {
                        ModLoader.OnUnhandledException(ExceptionDispatchInfo.Capture(ex));
                    }
            }

            if (__instance.Count <= 1)
                return false;

            var inOrder = true;

            for (var i = 0; i < __instance.Count; ++i)
            {
                if (__instance[i].ComponentBayId == i)
                    continue;
                inOrder = false;
                break;
            }

            if (inOrder) return false;

            var comparer = Comparer<ComponentBay>.Create((a, b)
                => a.ComponentBayId.CompareTo(b.ComponentBayId));
            var set = new SortedSet<ComponentBay>(__instance, comparer);
            if (__instance.Count != set.Count)
                try
                {
                    Console.Error.WriteLine("A component bay has been added twice with the same ID!");
                    foreach (var item in __instance)
                        Console.Error.WriteLine($"ComponentBayId:{set.FirstOrDefault(x => x == item)?.ComponentBayId}");
                }
                catch (Exception ex)
                {
                    ModLoader.OnUnhandledException(ExceptionDispatchInfo.Capture(ex));
                }
            __instance.Clear();
            __instance.AddRange(set);
            try
            {
                __instance.RebuildIndexes();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to rebuild indexes for component bays!");
                ModLoader.OnUnhandledException(ExceptionDispatchInfo.Capture(ex));
            }

            return false;
        }
        catch (Exception ex)
        {
            ModLoader.OnUnhandledException(ExceptionDispatchInfo.Capture(ex));
            throw; // fatal
        }
    }
}
