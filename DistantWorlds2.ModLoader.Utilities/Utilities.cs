using System.Diagnostics.CodeAnalysis;

namespace DistantWorlds2.ModLoader;

public static class Utilities
{
    [SuppressMessage("ReSharper", "UseDeconstructionOnParameter")]
    public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> kv, out TKey key, out TValue value)
    {
        key = kv.Key;
        value = kv.Value;
    }
}
