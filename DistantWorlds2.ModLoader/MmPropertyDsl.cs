using System.Linq.Expressions;
using JetBrains.Annotations;
using StringToExpression.GrammerDefinitions;
using StringToExpression.Util;

namespace DistantWorlds2.ModLoader;

using static LanguageHelpers;

[PublicAPI]
public sealed class MmPropertyDsl<T> : MmVariableDslBase where T : class
{
    public MmPropertyDsl() { }

    public MmPropertyDsl(double value) : base(value) { }

    public MmPropertyDsl(double value, T original) : base(value)
        => Original = original;


    public T? Original { get; set; }

    public object Context => ContextStack.Peek();

    public Stack<object> ContextStack { get; } = new(new[] { (object)null! });

    /// <summary>
    /// Returns the definitions for types used within the language.
    /// </summary>
    /// <returns></returns>
    protected override IEnumerable<GrammerDefinition> TypeDefinitions()
    {
        foreach (var gd in base.TypeDefinitions())
            yield return gd;

        yield return new OperandDefinition(
            @"INTRIN_OLD_OBJ",
            Rx(@"(?i)(?<=\b)orig\(\)"),
            x => {
                return Expression.Call(
                    Expression.Constant(this),
                    Type<object>.Method(o => GetOriginal()));
            });
    }

    private T? GetOriginal() => Original;
}
