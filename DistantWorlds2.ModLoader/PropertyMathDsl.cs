using System.Linq.Expressions;
using JetBrains.Annotations;
using StringToExpression.GrammerDefinitions;
using StringToExpression.Util;

namespace DistantWorlds2.ModLoader;

using static LanguageHelpers;

[PublicAPI]
public sealed class PropertyMathDsl<T> : VariableMathDslBase where T : class
{
    public PropertyMathDsl() { }

    public PropertyMathDsl(double value) : base(value) { }

    public PropertyMathDsl(double value, T old) : base(value)
        => Old = old;

    public PropertyMathDsl(double value, T old, T @new) : base(value)
    {
        Old = old;
        New = @new;
    }

    public T? Old { get; set; }
    public T? New { get; set; }

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
            Rx(@"(?i)(?<=\b)old\(\)"),
            x => {
                return Expression.Call(
                    Expression.Constant(this),
                    Type<object>.Method(o => GetOld()));
            });
        yield return new OperandDefinition(
            @"INTRIN_NEW_OBJ",
            Rx(@"(?i)(?<=\b)new\(\)"),
            x => {
                return Expression.Call(
                    Expression.Constant(this),
                    Type<object>.Method(o => GetNew()));
            });
    }

    private T GetOld() => Old;

    private T GetNew() => New;
}
