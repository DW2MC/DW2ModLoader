using System.Linq.Expressions;
using System.Reflection;
using JetBrains.Annotations;
using StringToExpression;
using StringToExpression.GrammerDefinitions;
using StringToExpression.Util;

namespace DistantWorlds2.ModLoader;

using static LanguageHelpers;

[PublicAPI]
public abstract class MathDslBase
{
    public readonly Language Language;

    protected MathDslBase()
        => Language = new(AllDefinitions().ToArray());

    /// <summary>
    /// Parses the specified text converting it into a expression action.
    /// </summary>
    /// <param name="text">The text to parse.</param>
    /// <returns></returns>
    public Expression<Func<double>> Parse(string text)
    {
        var body = Language.Parse(text);
        body = ExpressionConversions.Convert(body, typeof(double));
        return Expression.Lambda<Func<double>>(body);
    }

    /*
    /// <summary>
    /// Parses the specified text converting it into an expression. The expression can take a single parameter
    /// </summary>
    /// <typeparam name="T">the type of the parameter.</typeparam>
    /// <param name="text">The text to parse.</param>
    /// <returns></returns>
    public Expression<Func<T, double>> Parse<T>(string text)
    {
        var parameters = new[] { Expression.Parameter(typeof(T)) };
        var body = Language.Parse(text, parameters);
        body = ExpressionConversions.Convert(body, typeof(double));
        return Expression.Lambda<Func<T, double>>(body, parameters);
    }
    */

    /// <summary>
    /// Returns all the definitions used by the language.
    /// </summary>
    /// <returns></returns>
    private IEnumerable<GrammerDefinition> AllDefinitions()
    {
        IEnumerable<FunctionCallDefinition> functions;
        var definitions = new List<GrammerDefinition>();
        definitions.AddRange(TypeDefinitions());
        definitions.AddRange(functions = FunctionDefinitions());
        definitions.AddRange(BracketDefinitions(functions));
        definitions.AddRange(OperatorDefinitions());
        definitions.AddRange(PropertyDefinitions());
        definitions.AddRange(WhitespaceDefinitions());
        return definitions;
    }

    /// <summary>
    /// Returns the definitions for types used within the language.
    /// </summary>
    /// <returns></returns>
    protected virtual IEnumerable<GrammerDefinition> TypeDefinitions()
    {
        //Only have double to make things easier for casting
        yield return new OperandDefinition(
            @"NUMBER",
            Rx(@"(?i)(?<![\w\)])[-+]?[0-9]*\.?[0-9]+(?:e[-+]?[0-9]+)?"),
            x => Expression.Constant(double.Parse(x)));
        yield return new OperandDefinition(
            @"STRING",
            Rx(@"(?<![""\\])""(?:[^""\\]|\\.)*?""(?!=[""\\])"),
            (value, _) => Expression.Constant(StringUtils.Unescape(value.Substring(1, value.Length - 2))));
        yield return new OperandDefinition(
            @"CONST_PI",
            Rx(@"(?i)(?<=\b)pi(?=\b)"),
            x => Expression.Constant(Math.PI));
        yield return new OperandDefinition(
            @"CONST_PINF",
            Rx(@"(?i)(?<![\w\)])\+inf(?=\b)"),
            x => Expression.Constant(double.PositiveInfinity));
        yield return new OperandDefinition(
            @"CONST_NINF",
            Rx(@"(?i)(?<![\w\)])\-inf(?=\b)"),
            x => Expression.Constant(double.NegativeInfinity));
        yield return new OperandDefinition(
            @"CONST_NAN",
            Rx(@"(?i)(?<=\b)nan(?=\b)"),
            x => Expression.Constant(double.NaN));
        yield return new OperandDefinition(
            @"CONST_E",
            Rx(@"(?i)(?<=\b)e(?=\b)"),
            x => Expression.Constant(Math.E));
    }

    /// <summary>
    /// Returns the definitions for arithmetic operators used within the language.
    /// </summary>
    /// <returns></returns>
    protected virtual IEnumerable<GrammerDefinition> OperatorDefinitions()
    {
        yield return new BinaryOperatorDefinition(
            @"ADD", Rx(@"\+"), 2, Expression.Add);

        yield return new BinaryOperatorDefinition(
            @"SUB", Rx(@"\-"), 2, Expression.Subtract);

        yield return new BinaryOperatorDefinition(
            @"MUL", Rx(@"\*"), 1, Expression.Multiply);

        yield return new BinaryOperatorDefinition(
            @"DIV", Rx(@"\/"), 1, Expression.Divide);

        yield return new BinaryOperatorDefinition(
            @"MOD", Rx(@"%"), 1, Expression.Modulo);
    }

    /// <summary>
    /// Returns the definitions for brackets used within the language.
    /// </summary>
    /// <param name="functionCalls">The function calls in the language. (used as opening brackets)</param>
    /// <returns></returns>
    protected virtual IEnumerable<GrammerDefinition> BracketDefinitions(IEnumerable<FunctionCallDefinition> functionCalls)
    {
        BracketOpenDefinition openBrace;
        ListDelimiterDefinition delim;

        yield return openBrace = new(@"OPEN_BRACE", Rx(@"\("));
        yield return delim = new(@"COMMA", Rx(@","));

        yield return new BracketCloseDefinition(
            @"CLOSE_BRACE",
            Rx(@"\)"),
            new[] { openBrace }.Concat(functionCalls),
            delim);
    }

    /// <summary>
    /// Returns the definitions for functions used within the language.
    /// </summary>
    /// <returns></returns>
    protected virtual IEnumerable<FunctionCallDefinition> FunctionDefinitions()
    {
        yield return new FunctionCallDefinition(
            @"FN_ABS",
            Rx(@"(?i)(?<=\b)abs\("),
            new[] { typeof(double) },
            parameters => Expression.Call(
                null,
                Type<object>.Method(x => Math.Abs(0d)),
                parameters[0]));

        yield return new FunctionCallDefinition(
            @"FN_SIN",
            Rx(@"(?i)(?<=\b)sin\("),
            new[] { typeof(double) },
            parameters => Expression.Call(
                null,
                Type<object>.Method(x => Math.Sin(0d)),
                parameters[0]));

        yield return new FunctionCallDefinition(
            @"FN_ASIN",
            Rx(@"(?i)(?<=\b)asin\("),
            new[] { typeof(double) },
            parameters => Expression.Call(
                null,
                Type<object>.Method(x => Math.Asin(0d)),
                parameters[0]));

        yield return new FunctionCallDefinition(
            @"FN_COS",
            Rx(@"(?i)(?<=\b)cos\("),
            new[] { typeof(double) },
            parameters => Expression.Call(
                null,
                Type<object>.Method(x => Math.Cos(0d)),
                parameters[0]));

        yield return new FunctionCallDefinition(
            @"FN_ACOS",
            Rx(@"(?i)(?<=\b)acos\("),
            new[] { typeof(double) },
            parameters => Expression.Call(
                null,
                Type<object>.Method(x => Math.Acos(0d)),
                parameters[0]));

        yield return new FunctionCallDefinition(
            @"FN_TAN",
            Rx(@"(?i)(?<=\b)tan\("),
            new[] { typeof(double) },
            parameters => Expression.Call(
                null,
                Type<object>.Method(x => Math.Tan(0d)),
                parameters[0]));

        yield return new FunctionCallDefinition(
            @"FN_ATAN2",
            Rx(@"(?i)(?<=\b)atan2\("),
            new[] { typeof(double) },
            parameters => Expression.Call(
                null,
                Type<object>.Method(x => Math.Atan2(0d, 0d)),
                parameters[0], parameters[1]));

        yield return new FunctionCallDefinition(
            @"FN_POW",
            Rx(@"(?i)(?<=\b)pow\("),
            new[] { typeof(double), typeof(double) },
            parameters => Expression.Call(
                null,
                Type<object>.Method(x => Math.Pow(0d, 0d)),
                parameters[0], parameters[1]));

        yield return new FunctionCallDefinition(
            @"FN_SQRT",
            Rx(@"(?i)(?<=\b)sqrt\("),
            new[] { typeof(double) },
            parameters => Expression.Call(
                null,
                Type<object>.Method(x => Math.Sqrt(0d)),
                parameters[0]));

        yield return new FunctionCallDefinition(
            @"FN_EXP",
            Rx(@"(?i)(?<=\b)exp\("),
            new[] { typeof(double) },
            parameters => Expression.Call(
                null,
                Type<object>.Method(x => Math.Exp(0d)),
                parameters[0]));

        yield return new FunctionCallDefinition(
            @"FN_LOG",
            Rx(@"(?i)(?<=\b)log\("),
            new[] { typeof(double) },
            parameters => Expression.Call(
                null,
                Type<object>.Method(x => Math.Log(0d)),
                parameters[0]));

        yield return new FunctionCallDefinition(
            @"FN_ROUND",
            Rx(@"(?i)(?<=\b)round\("),
            new[] { typeof(double) },
            parameters => Expression.Call(
                null,
                Type<object>.Method(x => Math.Round(0d, default(MidpointRounding))),
                parameters[0], Expression.Constant(MidpointRounding.AwayFromZero)));

        yield return new FunctionCallDefinition(
            @"FN_FLOOR",
            Rx(@"(?i)(?<=\b)floor\("),
            new[] { typeof(double) },
            parameters => Expression.Call(
                null,
                Type<object>.Method(x => Math.Floor(0d)),
                parameters[0]));

        yield return new FunctionCallDefinition(
            @"FN_CEILING",
            Rx(@"(?i)(?<=\b)ceil\("),
            new[] { typeof(double) },
            parameters => Expression.Call(
                null,
                Type<object>.Method(x => Math.Ceiling(0d)),
                parameters[0]));

        yield return new FunctionCallDefinition(
            @"FN_TRUNC",
            Rx(@"(?i)(?<=\b)trunc\("),
            new[] { typeof(double) },
            parameters => Expression.Call(
                null,
                Type<object>.Method(x => Math.Truncate(0d)),
                parameters[0]));

        yield return new FunctionCallDefinition(
            "FN_IS_DEF",
            Rx(@"(?i)(?<=\b)isInf\("),
            new[] { typeof(double) },
            parameters => Expression.Call(
                null,
                Type<object>.Method(x => IsInfinity(0d)),
                parameters[0]));

        yield return new FunctionCallDefinition(
            "FN_IS_NAN",
            Rx(@"(?i)(?<=\b)isNaN\("),
            new[] { typeof(double) },
            parameters => Expression.Call(
                null,
                Type<object>.Method(x => IsNaN(0d)),
                parameters[0]));
    }

    private double IsInfinity(double d)
        => double.IsInfinity(d) ? 1 : 0;

    private double IsNaN(double d)
        => double.IsNaN(d) ? 1 : 0;

    /// <summary>
    /// Returns the definitions for property names used within the language.
    /// </summary>
    /// <returns></returns>
    protected virtual IEnumerable<GrammerDefinition> PropertyDefinitions()
    {
        yield return new OperandDefinition(
            "PROPERTY_PATH",
            Rx(@"\.([A-Za-z_][A-Za-z0-9_]*)\b"),
            (value, parameters) => value.Split('.')
                .Aggregate((Expression)parameters[0], (exp, prop)
                    => Expression.MakeMemberAccess(exp, exp.Type.GetRuntimeProperties()
                        .First(x => x.Name.Equals(prop,
                            StringComparison.OrdinalIgnoreCase)))));
        yield break;
    }

    /// <summary>
    /// Returns the definitions for whitespace used within the language.
    /// </summary>
    /// <returns></returns>
    protected virtual IEnumerable<GrammerDefinition> WhitespaceDefinitions()
    {
        yield return new GrammerDefinition("SPACE", Rx(@"\s+"), true);
    }
}
