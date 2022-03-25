using NuGet.Versioning;

namespace DistantWorlds2.ModLoader;

public interface IUpdateCheck
{
    SemanticVersion? NewVersion { get; }

    Task<bool> NewVersionCheck { get; }

    bool IsNewVersionAvailable { get; }

    bool Start();
}
