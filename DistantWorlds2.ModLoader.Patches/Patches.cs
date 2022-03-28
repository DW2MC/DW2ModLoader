using HarmonyLib;
using JetBrains.Annotations;

namespace DistantWorlds2.ModLoader;

[PublicAPI]
public class Patches : IPatches
{
    private readonly Harmony Harmony = new("DistantWorlds2.ModLoader.Patches");
    public void Run()
        => Harmony.PatchAll();
}
