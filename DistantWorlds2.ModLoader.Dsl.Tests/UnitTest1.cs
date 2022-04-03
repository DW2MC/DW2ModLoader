using NUnit.Framework;

namespace DistantWorlds2.ModLoader.Dsl.Tests;

public class Tests
{
    [SetUp]
    public void Setup() { }

    [Test]
    public void Test()
    {
        var s = "count(def.AllowableGovernmentIds) > 3 and def.AllowableGovernmentIds contains 0 and not def.AllowableGovernmentIds contains 6";
    }
}
