using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace ModDevToolsMod;

public class StreamAndDocumentSkipperEmitter : IEmitter {

  private readonly IEmitter _emitter;

  public StreamAndDocumentSkipperEmitter(IEmitter emitter)
    => _emitter = emitter;

  public void Emit(ParsingEvent @event) {
    if (@event is DocumentStart or DocumentEnd or StreamStart or StreamEnd) return;

    _emitter.Emit(@event);
  }

}