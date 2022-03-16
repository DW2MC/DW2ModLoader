using System.Runtime.ExceptionServices;
using JetBrains.Annotations;
using Trinet.Core.IO.Ntfs;

namespace DistantWorlds2.ModLoader;

[PublicAPI]
internal static class UnblockUtil
{
    internal static void UnblockFile(string filePath)
    {
        var f = new FileInfo(filePath);
        try
        {
            if (!f.DeleteAlternateDataStream("Zone.Identifier"))
                Console.WriteLine($"No PersistentZoneIdentifier for {filePath}");
        }
        catch (Exception ex)
        {
            ModManager.OnUnhandledException(ExceptionDispatchInfo.Capture(ex));

            Console.WriteLine($"Failed to  PersistentZoneIdentifier for {filePath}");

            return;
        }
        Console.WriteLine($"Removed PersistentZoneIdentifier for {filePath}");
    }

    internal static void UnblockDirectory(string path)
    {
        foreach (var filePath in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
            UnblockFile(filePath);
    }
}
