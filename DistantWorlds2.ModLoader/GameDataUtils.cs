using System.IO.Hashing;
using System.IO.MemoryMappedFiles;
using System.Reflection;
using System.Runtime.InteropServices;
using DistantWorlds.Types;
using JetBrains.Annotations;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace DistantWorlds2.ModLoader;

public static class GameDataUtils
{
    public static readonly ISerializer Serializer = new SerializerBuilder()
        .WithNamingConvention(NullNamingConvention.Instance)
        .WithIndentedSequences()
        .Build();

    private static readonly DeserializerBuilder DeserializerBase = new DeserializerBuilder()
        .WithNamingConvention(NullNamingConvention.Instance)
        .IgnoreUnmatchedProperties();

    public static readonly IDeserializer Deserializer = DeserializerBase
        .WithNodeDeserializer(new FormulaScalarNodeDeserializer(new SimpleMathDsl()))
        .Build();

    public static YamlStream LoadYaml(Stream data)
    {
        using var sr = new StreamReader(data);
        var ys = new YamlStream();
        ys.Load(sr);
        return ys;
    }

    public static MemberInfo GetInstancePropertyOrField(Type type, string name)
        => type.GetMember(name, MemberTypes.Field | MemberTypes.Property,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
            .Single();

    public static Type GetType(MemberInfo m)
        => m switch
        {
            FieldInfo fi => fi.FieldType,
            PropertyInfo pi => pi.PropertyType,
            _ => throw new NotImplementedException()
        };

    public static object GetValue(object obj, MemberInfo m)
        => m switch
        {
            FieldInfo fi => fi.GetValue(obj),
            PropertyInfo pi => pi.GetValue(obj),
            _ => throw new NotImplementedException()
        };
    public static void SetValue(object obj, MemberInfo m, object value)
    {
        switch (m)
        {
            case FieldInfo fi:
                fi.SetValue(obj, value);
                break;
            case PropertyInfo pi:
                pi.SetValue(obj, value);
                break;
            default: throw new NotImplementedException();
        }
    }

    public static object GetRealNextId<T>(List<T> defs)
        => defs.Count;

    public static object GetRealNextId<T>(IndexedList<T> defs)
    {
        var nextId = defs.Count;
        while (defs.ContainsId(nextId))
            nextId += 1;
        //defs.SetNextId(nextId + 1);
        return nextId;
    }
    public static Type GetFieldOrPropertyType(MemberInfo idField)
        => idField is FieldInfo idFi ? idFi.FieldType : ((PropertyInfo)idField).PropertyType;
}

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
