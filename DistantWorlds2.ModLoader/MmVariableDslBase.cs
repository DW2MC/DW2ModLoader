using System.Collections.Concurrent;
using System.Linq.Expressions;
using JetBrains.Annotations;
using StringToExpression.GrammerDefinitions;
using StringToExpression.Util;

namespace DistantWorlds2.ModLoader;

using static LanguageHelpers;

[PublicAPI]
public sealed class MmVariableDsl : DslBase
{
    public object? this[string symbol]
    {
        get => Variables.TryGetValue(symbol, out var e)
            ? e is ConstantExpression ce
                ? ce.Value
                : e
            : null;

        set {
            if (value is null)
                Variables.TryRemove(symbol, out _);
            else
                Variables[symbol] = value is Expression e
                    ? e
                    : Expression.Constant(value);
        }
    }

    private static ConcurrentDictionary<string, object> StaticVariableSource
        => ModManager.Instance.SharedVariables;


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
