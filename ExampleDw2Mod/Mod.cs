using DistantWorlds2;
using JetBrains.Annotations;

namespace ExampleDw2Mod;

[PublicAPI]
public class Mod
{
    public Mod(DWGame game)
        => DWGame.Version += " Example Mod";
}
