using Semver;

namespace DistantWorlds2.ModLoader;

public interface IUpdateCheck
{
    SemVersion? NewVersion { get; }

    Task<bool> NewVersionCheck { get; }

    bool IsNewVersionAvailable { get; }

    bool Start();
}
