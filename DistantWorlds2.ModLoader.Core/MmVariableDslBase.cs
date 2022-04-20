using System.Collections.Concurrent;
using System.Linq.Expressions;
using JetBrains.Annotations;

namespace DistantWorlds2.ModLoader;

[PublicAPI]
public sealed class MmVariableDsl : DslBase
{
    public object? this[string symbol]
    {
        get => GetVariable(symbol);

        set => SetVariable(symbol, value);
    }

    private static ConcurrentDictionary<string, object> StaticVariableSource
        => ModLoader.ModManager.SharedVariables;

    public override object? ResolveGlobalSymbol(string symbol)
    {
        var obj = base.ResolveGlobalSymbol(symbol);
        return obj ?? (
            StaticVariableSource.TryGetValue(symbol, out obj)
                ? obj
                : null
        );
    }
}
