using JetBrains.Annotations;

namespace DistantWorlds2.ModLoader;

[PublicAPI]
public sealed class MmVariableDsl : MmVariableDslBase
{
    public static readonly MmVariableDsl Zero = new(0);
    public static readonly MmVariableDsl NaN = new(0);

    public MmVariableDsl(double value) : base(value) { }
}
