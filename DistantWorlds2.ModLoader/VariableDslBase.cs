using System.Linq.Expressions;
using JetBrains.Annotations;
using StringToExpression.GrammerDefinitions;
using StringToExpression.Util;

namespace DistantWorlds2.ModLoader;

using static LanguageHelpers;

[PublicAPI]
public abstract class VariableDslBase : DslBase
{
    protected VariableDslBase()
        => Value = 0;
    protected VariableDslBase(double value)
        => Value = value;

    public double Value { get; set; }

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
            x => {
                return Expression.Call(
                    Expression.Constant(this),
                    Type<object>.Method(o => GetValue()));
            });

        yield return new OperandDefinition(
            @"MM_VARIABLE",
            Rx(@"(?<=\b)(?<![A-Za-z0-9_\.])([A-Za-z][A-Za-z0-9_]*)(?=\b)"),
            (value, _) => {
                if (!ModManager.Instance.SharedVariables.TryGetValue(value, out var val))
                    return Expression.Constant(double.NaN);

                return val switch
                {
                    string s => Expression.Constant(s),
                    double d => Expression.Constant(d),
                    IConvertible c => Expression.Constant(c.ToDouble(null)),
                    _ => Expression.Constant(double.NaN)
                };
            });
    }

    private double GetValue() => Value;

    private double GetSharedVariable(string name)
    {
        if (!ModManager.Instance.SharedVariables.TryGetValue(name, out var o))
            return double.NaN;
        try
        {
            if (o is IConvertible c)
                return c.ToDouble(null);
        }
        catch
        {
            // darn
        }

        return double.NaN;
    }
}
