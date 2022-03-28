using JetBrains.Annotations;
using Trinet.Core.IO.Ntfs;

namespace DistantWorlds2.ModLoader;

[PublicAPI]
public class Unblocker : IUnblocker
{
    public void UnblockFile(string filePath)
    {
        var f = new FileInfo(filePath);
        try
        {
            f.DeleteAlternateDataStream("Zone.Identifier");
        }
        catch
        {
            // ok
        }
    }

    public void UnblockDirectory(string path)
    {
        foreach (var filePath in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
            UnblockFile(filePath);
    }
}
