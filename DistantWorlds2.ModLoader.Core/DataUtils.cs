using System.IO.Hashing;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace DistantWorlds2.ModLoader;

[PublicAPI]
public static class DataUtils
{
    public static byte[] GetDirectoryHash(NonCryptographicHashAlgorithm hasher, string dir)
    {
        ComputeDirectoryHash(hasher, dir);

        var hash = new byte[hasher.HashLengthInBytes];

        if (!hasher.TryGetCurrentHash(hash, out _))
            throw new NotImplementedException();

        return hash;
    }

    public static void ComputeDirectoryHash(NonCryptographicHashAlgorithm hasher, string dir)
    {
        var filePaths = Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories)
            .ToArray();

        Array.Sort(filePaths, StringComparer.OrdinalIgnoreCase);

        foreach (var filePath in filePaths)
        {
            var filePathChars = MemoryMarshal.AsBytes<char>(filePath.ToCharArray());
            hasher.Append(filePathChars);
            ComputeFileHash(hasher, filePath);
            break;
        }
    }

    public static void ComputeFileHash(NonCryptographicHashAlgorithm hasher, string filePath)
    {

        //using var fileStream = File.OpenRead(filePath);
        //var fileLength = fileStream.Length;

        /* for sufficiently big files, spare the memory pressure?
            if (fileLength > int.MaxValue)
            {
                hasher.Append(fileStream);
                return;
            }
            */

        // fast memory mapped file hashing
        var fileLength = new FileInfo(filePath).Length;
        using var mapping = MemoryMappedFile.CreateFromFile(filePath, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
        /*
        using var mapping = MemoryMappedFile.CreateFromFile(
            fileStream,
            null,
            fileLength,
            MemoryMappedFileAccess.Read,
            null,
            0,
            false
        );*/

        // fallback copies for files >2GB
        if (fileLength > int.MaxValue)
        {
            var mapStream = mapping.CreateViewStream(0, fileLength, MemoryMappedFileAccess.Read);

            hasher.Append(mapStream);
            return;
        }

        using var view = mapping.CreateViewAccessor(0, fileLength, MemoryMappedFileAccess.Read);

        unsafe
        {
            byte* p = default;

            view.SafeMemoryMappedViewHandle.AcquirePointer(ref p);
            try
            {
                if (view.PointerOffset != 0)
                    throw new NotImplementedException();

                var viewLength = view.SafeMemoryMappedViewHandle.ByteLength;

                if (viewLength < (ulong)fileLength)
                    throw new NotImplementedException();

                var span = new ReadOnlySpan<byte>(p, (int)fileLength);

                hasher.Append(span);
            }
            finally
            {
                view.SafeMemoryMappedViewHandle.ReleasePointer();
            }
        }
    }
}
