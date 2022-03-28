using YamlDotNet.Core.Events;
using YamlDotNet.RepresentationModel;

namespace DistantWorlds2.ModLoader;

public static class YamlNodeToEventStreamConverter
{
    public static IEnumerable<ParsingEvent> ConvertToEventStream(this YamlStream stream)
    {
        yield return new StreamStart();
        foreach (var document in stream.Documents)
        {
            foreach (var evt in ConvertToEventStream(document))
                yield return evt;
        }
        yield return new StreamEnd();
    }

    public static IEnumerable<ParsingEvent> ConvertToEventStream(this YamlDocument document)
    {
        yield return new DocumentStart();
        foreach (var evt in ConvertToEventStream(document.RootNode))
            yield return evt;
        yield return new DocumentEnd(false);
    }

    public static IEnumerable<ParsingEvent> ConvertToEventStream(this YamlNode node)
        => node switch
        {
            YamlScalarNode scalar => ConvertToEventStream(scalar),
            YamlSequenceNode sequence => ConvertToEventStream(sequence),
            YamlMappingNode mapping => ConvertToEventStream(mapping),
            _ => throw new NotSupportedException($"Unsupported node type: {node.GetType().Name}")
        };

    private static IEnumerable<ParsingEvent> ConvertToEventStream(this YamlScalarNode scalar)
    {
        yield return new Scalar(scalar.Anchor, scalar.Tag, scalar.Value!, scalar.Style, false, false, scalar.Start, scalar.End);
    }

    private static IEnumerable<ParsingEvent> ConvertToEventStream(this YamlSequenceNode sequence)
    {
        yield return new SequenceStart(sequence.Anchor, sequence.Tag, false, sequence.Style, sequence.Start, sequence.End);
        foreach (var node in sequence.Children)
        {
            foreach (var evt in ConvertToEventStream(node))
                yield return evt;
        }
        yield return new SequenceEnd();
    }

    private static IEnumerable<ParsingEvent> ConvertToEventStream(this YamlMappingNode mapping)
    {
        yield return new MappingStart(mapping.Anchor, mapping.Tag, false, mapping.Style, mapping.Start, mapping.End);
        foreach (var pair in mapping.Children)
        {
            foreach (var evt in ConvertToEventStream(pair.Key))
                yield return evt;
            foreach (var evt in ConvertToEventStream(pair.Value))
                yield return evt;
        }
        yield return new MappingEnd();
    }
}
