using System.Collections;
using System.Collections.Immutable;

namespace ModDevToolsMod;

public sealed class ReadOnlyLinearDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue> where TKey : notnull {

  private readonly ImmutableArray<KeyValuePair<TKey, TValue>> _source;

  private readonly ImmutableDictionary<TKey, TValue> _dict;

  public ReadOnlyLinearDictionary(IEnumerable<KeyValuePair<TKey, TValue>> source) {
    _source = source.ToImmutableArray();
    _dict = _source.ToImmutableDictionary();
  }

  public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    => ((IEnumerable<KeyValuePair<TKey, TValue>>)_source).GetEnumerator();

  IEnumerator IEnumerable.GetEnumerator()
    => ((IEnumerable)_source).GetEnumerator();

  public int Count
    => _dict.Count;

  public bool ContainsKey(TKey key)
    => _dict.ContainsKey(key);

  public bool TryGetValue(TKey key, out TValue value)
    => _dict.TryGetValue(key, out value!);

  public TValue this[TKey key]
    => _dict[key]!;

  public IEnumerable<TKey> Keys
    => _source.Select(kv => kv.Key);

  public IEnumerable<TValue> Values
    => _source.Select(kv => kv.Value);

}