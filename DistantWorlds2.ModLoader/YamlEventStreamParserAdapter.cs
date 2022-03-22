using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.RepresentationModel;

namespace DistantWorlds2.ModLoader;

public class YamlEventStreamParserAdapter : IParser
{
    private readonly IEnumerator<ParsingEvent> _enumerator;
    public YamlEventStreamParserAdapter(YamlNode node)
        : this(node.ConvertToEventStream()) { }
    public YamlEventStreamParserAdapter(IEnumerable<ParsingEvent> events)
        => _enumerator = events.GetEnumerator();

    public ParsingEvent Current => _enumerator.Current!;

    public bool MoveNext()
        => _enumerator.MoveNext();
}