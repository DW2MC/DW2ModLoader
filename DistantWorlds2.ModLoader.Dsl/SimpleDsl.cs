using JetBrains.Annotations;

namespace DistantWorlds2.ModLoader;

[PublicAPI]
public sealed class SimpleDsl : DslBase
{
    public static readonly SimpleDsl Instance = new();
}
