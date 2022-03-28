using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

namespace DistantWorlds2.ModLoader;

public static class YamlDeserializerExtensions
{
    public static object? Deserialize(this IDeserializer d, YamlNode node, Type type)
        => d.Deserialize(new YamlEventStreamParserAdapter(node), type);

    public static T? Deserialize<T>(this IDeserializer d, YamlNode node)
        => d.Deserialize<T>(new YamlEventStreamParserAdapter(node));
}
