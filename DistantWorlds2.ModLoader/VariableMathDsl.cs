using JetBrains.Annotations;

namespace DistantWorlds2.ModLoader;

[PublicAPI]
public sealed class VariableMathDsl : VariableMathDslBase
{
    public static readonly VariableMathDsl Zero = new(0);
    public static readonly VariableMathDsl NaN = new(0);

    public VariableMathDsl(double value) : base(value) { }
}
