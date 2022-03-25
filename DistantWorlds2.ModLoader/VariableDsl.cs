using JetBrains.Annotations;

namespace DistantWorlds2.ModLoader;

[PublicAPI]
public sealed class VariableDsl : VariableDslBase
{
    public static readonly VariableDsl Zero = new(0);
    public static readonly VariableDsl NaN = new(0);

    public VariableDsl(double value) : base(value) { }
}
