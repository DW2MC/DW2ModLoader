using HarmonyLib;

namespace DistantWorlds2.ModLoader;

public interface IPatches
{
    void Run();
    
    internal Harmony Harmony { get; }
}
