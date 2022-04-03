using JetBrains.Annotations;

namespace DistantWorlds2.ModLoader;

[PublicAPI]
public interface IUnblocker
{
    void UnblockFile(string filePath);
}