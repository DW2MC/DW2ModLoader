/*
using System.Reflection;
using HarmonyLib;
using Xenko.Core.Serialization;
using Xenko.Graphics;

namespace DistantWorlds2.ModLoader;

[HarmonyPatch]
public static class PatchTextureContentSerializerSerialize
{
    public static MethodBase TargetMethod()
    {
        var type = Type.GetType("Xenko.Graphics.Data.TextureContentSerializer, Xenko.Graphics, Version=3.2.0.1, Culture=neutral, PublicKeyToken=null");
        if (type is null) throw new NotSupportedException("Can't find TextureContentSerializer");
        var method = type.GetMethod("Serialize", new Type[] { typeof(ArchiveMode), typeof(SerializationStream), typeof(Texture), typeof(bool) });
        if (method is null) throw new NotSupportedException("Can't find TextureContentSerializer.Serialize");
        return method;
    }

    [HarmonyPatch("Serialize", new Type[] {})]
    public static bool PrefixSerialize(SerializationStream stream)
    {
        var objectIdGetter = stream.NativeStream.GetType().GetMethod("get_ObjectId");
        if (objectIdGetter is not null)
        {
            var objectId = (Xenko.Core.Storage.ObjectId)objectIdGetter.Invoke(stream.NativeStream, null);
            foreach (var provider in Xenko.Core.IO.VirtualFileSystem.Providers.OfType<Xenko.Core.IO.DatabaseFileProvider>())
            {
                foreach (var matchKv in provider.ObjectDatabase.ContentIndexMap.SearchValues(kv => kv.Value == objectId))
                {
                    matchKv.Key
                    break;
                }
            }
        }
    }
}
*/