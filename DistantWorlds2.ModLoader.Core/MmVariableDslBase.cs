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


    public override Expression? ResolveGlobalSymbol(string symbol)
    {
        var expr = base.ResolveGlobalSymbol(symbol);
        return expr ?? (
            StaticVariableSource.TryGetValue(symbol, out var obj)
                ? Expression.Constant(obj, obj.GetType())
                : expr
        );
    }
}
