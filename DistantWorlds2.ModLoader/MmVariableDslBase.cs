using System.Collections.Concurrent;
using System.Linq.Expressions;
using JetBrains.Annotations;
using StringToExpression.GrammerDefinitions;
using StringToExpression.Util;

namespace DistantWorlds2.ModLoader;

using static LanguageHelpers;

[PublicAPI]
public abstract class MmVariableDslBase : DslBase
{
    protected MmVariableDslBase()
        => Value = 0;

    protected MmVariableDslBase(object? value)
        => Value = value;

    public object? Value { get; set; }

    /// <summary>
    /// Returns the definitions for types used within the language.
    /// </summary>
    /// <returns></returns>
    protected override IEnumerable<GrammerDefinition> TypeDefinitions()
    {
        foreach (var gd in base.TypeDefinitions())
            yield return gd;

        yield return new OperandDefinition(
            @"INTRIN_CUR_VAL",
            Rx(@"(?i)(?<=\b)value\(\)"),
            _ => {
                return Value switch
                {
                    double d => Expression.Constant(d),
                    string s => Expression.Constant(s),
                    sbyte or byte or short or ushort or int or uint or long or ulong or float
                        => Expression.Constant(((IConvertible)Value).ToDouble(null)),
                    _ => Expression.Constant(Value)
                };
            });

        yield return new OperandDefinition(
            @"MM_VARIABLE",
            Rx(@"(?<=\b)(?<![A-Za-z0-9_\.])([A-Za-z][A-Za-z0-9_]*)(?=\b)"),
            value => {
                if (!StaticVariableSource.TryGetValue(value, out var val))
                    return Expression.Constant(double.NaN);

                return val switch
                {
                    string s => Expression.Constant(s),
                    double d => Expression.Constant(d),
                    IConvertible c => Expression.Constant(c.ToDouble(null)),
                    _ => Expression.Constant(val)
                };
            });
    }

    private static ConcurrentDictionary<string, object> StaticVariableSource
        => ModManager.Instance.SharedVariables;

    private object? GetValue() => Value;
}
