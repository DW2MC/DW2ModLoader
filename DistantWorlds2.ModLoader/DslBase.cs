using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using StringToExpression;
using StringToExpression.GrammerDefinitions;
using StringToExpression.Util;

namespace DistantWorlds2.ModLoader;

using static LanguageHelpers;

[PublicAPI]
public abstract class DslBase
{
    public Language Language => LangCache.GetOrAdd(GetType(), _ => new(AllDefinitions().ToArray()));

    private static readonly MethodInfo MiStringConcat =
        typeof(string).GetMethod(nameof(string.Concat), new[] { typeof(string), typeof(string) })!;

    private static readonly MethodInfo MiStringContains =
        typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) })!;

    private static readonly MethodInfo MiStringStartsWith =
        typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) })!;

    private static readonly MethodInfo MiStringEndsWith =
        typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) })!;

    private static readonly MethodInfo MiRegexMatch =
        typeof(DslBase).GetMethod(nameof(RegexMatch), new[] { typeof(string), typeof(string) })!;

    private static readonly ConcurrentDictionary<string, Regex> RegexCache = new();

    public static bool RegexMatch(string str, string rx)
        => RegexCache.GetOrAdd(rx, r => new(r, RegexOptions.CultureInvariant | RegexOptions.Compiled))
            .IsMatch(str);

    public static ConcurrentDictionary<Type, Language> LangCache = new();

    private static readonly MethodInfo MiIConvertibleToDouble
        = Type<IConvertible>.Method(o => o.ToDouble(null));

    private static MethodInfo Method(Expression<Action> a)
    {
        var body = a.Body;
        return body is MethodCallExpression mce
            ? mce.Method
            : throw new InvalidCastException("No method");
    }
    private static ConstructorInfo Constructor(Expression<Action> a)
    {
        var body = a.Body;
        return body is NewExpression ne
            ? ne.Constructor
            : throw new InvalidCastException("No constructor");
    }

    // ReSharper disable ReturnValueOfPureMethodIsNotUsed
    private static readonly MethodInfo MiMathAbs = Method(() => Math.Abs(0d));
    private static readonly MethodInfo MiMathSin = Method(() => Math.Sin(0d));
    private static readonly MethodInfo MiMathCos = Method(() => Math.Cos(0d));
    private static readonly MethodInfo MiMathAsin = Method(() => Math.Asin(0d));
    private static readonly MethodInfo MiMathAcos = Method(() => Math.Acos(0d));
    private static readonly MethodInfo MiMathTan = Method(() => Math.Tan(0d));
    private static readonly MethodInfo MiMathAtan = Method(() => Math.Atan(0d));
    private static readonly MethodInfo MiMathSqrt = Method(() => Math.Sqrt(0d));
    private static readonly MethodInfo MiMathExp = Method(() => Math.Exp(0d));
    private static readonly MethodInfo MiMathLog = Method(() => Math.Log(0d));
    private static readonly MethodInfo MiMathRound = Method(() => Math.Round(0d, MidpointRounding.AwayFromZero));
    private static readonly MethodInfo MiMathFloor = Method(() => Math.Floor(0d));
    private static readonly MethodInfo MiMathCeiling = Method(() => Math.Ceiling(0d));
    private static readonly MethodInfo MiMathTruncate = Method(() => Math.Truncate(0d));
    private static readonly MethodInfo MiDoubleIsInfinity = Method(() => double.IsInfinity(0d));
    private static readonly MethodInfo MiDoubleIsNaN = Method(() => double.IsNaN(0d));

    private static readonly MethodInfo MiMathPow = Method(() => Math.Pow(0d, 0d));
    private static readonly MethodInfo MiMathAtan2 = Method(() => Math.Atan2(0d, 0d));
    // ReSharper restore ReturnValueOfPureMethodIsNotUsed

    protected DslBase() { }

    /// <summary>
    /// Parses the specified text converting it into a expression action.
    /// </summary>
    /// <param name="text">The text to parse.</param>
    /// <returns></returns>
    public Expression<Func<object>> Parse(string text)
    {
        var body = Language.Parse(text);
        body = ExpressionConversions.Convert(body, typeof(object));
        return Expression.Lambda<Func<object>>(body);
    }

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

        yield return new BinaryOperatorDefinition(
            @"POW", Rx(@"\^"), 1, Expression.Power);

        yield return new BinaryOperatorDefinition(
            @"GT", Rx(@">"), 3, Expression.GreaterThan);

        yield return new BinaryOperatorDefinition(
            @"LT", Rx(@"<"), 3, Expression.LessThan);

        yield return new BinaryOperatorDefinition(
            @"LTE", Rx(@">="), 3, Expression.GreaterThanOrEqual);

        yield return new BinaryOperatorDefinition(
            @"GTE", Rx(@">="), 3, Expression.LessThanOrEqual);

        yield return new BinaryOperatorDefinition(
            @"IS", Rx(@"\bis\b"), 4, Expression.Equal);

        yield return new BinaryOperatorDefinition(
            @"AND", Rx(@"\band\b"), 4, Expression.AndAlso);

        yield return new BinaryOperatorDefinition(
            @"OR", Rx(@"\bor\b"), 4, Expression.OrElse);

        yield return new BinaryOperatorDefinition(
            @"CONCAT", Rx(@"(?<!\.)\.\.(?!\.)"), 2,
            (a, b) => Expression.Call(MiStringConcat, a, b));

        yield return new BinaryOperatorDefinition(
            @"CONTAINS", Rx(@"\bcontains\b"), 3,
            (a, b) => Expression.Call(a, MiStringContains, b));

        yield return new BinaryOperatorDefinition(
            @"STARTS", Rx(@"\bstarts\b"), 3,
            (a, b) => Expression.Call(a, MiStringStartsWith, b));

        yield return new BinaryOperatorDefinition(
            @"ENDS", Rx(@"\bends\b"), 3,
            (a, b) => Expression.Call(a, MiStringEndsWith, b));

        yield return new BinaryOperatorDefinition(
            @"REGEX_MATCH", Rx(@"\bmatches\b"), 3,
            (a, b) => Expression.Call(a, MiRegexMatch, b));

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

    private static Expression UnaryDoubleArgMethodFuncDef(Expression arg, MethodInfo unaryMethod)
        => Expression.Call(
            null,
            unaryMethod,
            ToDoubleFuncDef(arg));

    private static Expression ToDoubleFuncDef(Expression arg)
        => Expression.IfThenElse(
            Expression.TypeIs(arg, typeof(double)),
            Expression.Convert(arg, typeof(double)),
            Expression.Call(Expression.Convert(arg, typeof(IConvertible)),
                MiIConvertibleToDouble, null));

    private static Expression ToStringFuncDef(Expression arg)
        => Expression.IfThenElse(
            Expression.TypeIs(arg, typeof(string)),
            Expression.Convert(arg, typeof(string)),
            Expression.Call(arg, nameof(ToString), Type.EmptyTypes));

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
                MiMathAbs,
                parameters[0]));

        yield return new FunctionCallDefinition(
            @"FN_SIN",
            Rx(@"(?i)(?<=\b)sin\("),
            new[] { typeof(double) },
            parameters => Expression.Call(
                null,
                MiMathSin,
                parameters[0]));

        yield return new FunctionCallDefinition(
            @"FN_ASIN",
            Rx(@"(?i)(?<=\b)asin\("),
            new[] { typeof(double) },
            parameters => Expression.Call(
                null,
                MiMathAsin,
                parameters[0]));

        yield return new FunctionCallDefinition(
            @"FN_COS",
            Rx(@"(?i)(?<=\b)cos\("),
            new[] { typeof(double) },
            parameters => Expression.Call(
                null,
                MiMathCos,
                parameters[0]));

        yield return new FunctionCallDefinition(
            @"FN_ACOS",
            Rx(@"(?i)(?<=\b)acos\("),
            new[] { typeof(double) },
            parameters => Expression.Call(
                null,
                MiMathAcos,
                parameters[0]));

        yield return new FunctionCallDefinition(
            @"FN_TAN",
            Rx(@"(?i)(?<=\b)tan\("),
            new[] { typeof(double) },
            parameters => Expression.Call(
                null,
                MiMathTan,
                parameters[0]));

        yield return new FunctionCallDefinition(
            @"FN_ATAN2",
            Rx(@"(?i)(?<=\b)atan2\("),
            new[] { typeof(double) },
            parameters => Expression.Call(
                null,
                MiMathAtan2,
                parameters[0], parameters[1]));

        yield return new FunctionCallDefinition(
            @"FN_POW",
            Rx(@"(?i)(?<=\b)pow\("),
            new[] { typeof(double), typeof(double) },
            parameters => Expression.Call(
                null,
                MiMathPow,
                parameters[0], parameters[1]));

        yield return new FunctionCallDefinition(
            @"FN_SQRT",
            Rx(@"(?i)(?<=\b)sqrt\("),
            new[] { typeof(double) },
            parameters => Expression.Call(
                null,
                MiMathSqrt,
                parameters[0]));

        yield return new FunctionCallDefinition(
            @"FN_EXP",
            Rx(@"(?i)(?<=\b)exp\("),
            new[] { typeof(double) },
            parameters => Expression.Call(
                null,
                MiMathExp,
                parameters[0]));

        yield return new FunctionCallDefinition(
            @"FN_LOG",
            Rx(@"(?i)(?<=\b)log\("),
            new[] { typeof(double) },
            parameters => Expression.Call(
                null,
                MiMathLog,
                parameters[0]));

        yield return new FunctionCallDefinition(
            @"FN_ROUND",
            Rx(@"(?i)(?<=\b)round\("),
            new[] { typeof(double) },
            parameters => Expression.Call(
                null,
                MiMathRound,
                parameters[0], Expression.Constant(MidpointRounding.AwayFromZero)));

        yield return new FunctionCallDefinition(
            @"FN_FLOOR",
            Rx(@"(?i)(?<=\b)floor\("),
            new[] { typeof(double) },
            parameters => Expression.Call(
                null,
                MiMathFloor,
                parameters[0]));

        yield return new FunctionCallDefinition(
            @"FN_CEILING",
            Rx(@"(?i)(?<=\b)ceil\("),
            new[] { typeof(double) },
            parameters => Expression.Call(
                null,
                MiMathCeiling,
                parameters[0]));

        yield return new FunctionCallDefinition(
            @"FN_TRUNC",
            Rx(@"(?i)(?<=\b)trunc\("),
            new[] { typeof(double) },
            parameters => Expression.Call(
                null,
                MiMathTruncate,
                parameters[0]));

        yield return new FunctionCallDefinition(
            "FN_IS_INF",
            Rx(@"(?i)(?<=\b)isInf\("),
            new[] { typeof(double) },
            parameters => Expression.Call(
                null,
                MiDoubleIsInfinity,
                parameters[0]));

        yield return new FunctionCallDefinition(
            "FN_IS_NAN",
            Rx(@"(?i)(?<=\b)isNaN\("),
            new[] { typeof(double) },
            parameters => Expression.Call(
                null,
                MiDoubleIsNaN,
                parameters[0]));

        yield return new FunctionCallDefinition(
            @"FN_TO_NUM",
            Rx(@"(?i)(?<=\b)num\("),
            new[] { typeof(object) },
            parameters => ToDoubleFuncDef(parameters[0]));

        yield return new FunctionCallDefinition(
            @"FN_TO_NUM_FROM_STR",
            Rx(@"(?i)(?<=\b)num\("),
            new[] { typeof(string) },
            parameters => ToDoubleFuncDef(parameters[0]));

        yield return new FunctionCallDefinition(
            @"FN_TO_STR",
            Rx(@"(?i)(?<=\b)txt\("),
            new[] { typeof(object) },
            parameters => ToStringFuncDef(parameters[0]));

        yield return new FunctionCallDefinition(
            @"FN_TO_STR_FROM_NUM",
            Rx(@"(?i)(?<=\b)txt\("),
            new[] { typeof(double) },
            parameters => ToStringFuncDef(parameters[0]));
    }

    /// <summary>
    /// Returns the definitions for property names used within the language.
    /// </summary>
    /// <returns></returns>
    protected virtual IEnumerable<GrammerDefinition> PropertyDefinitions()
    {
        yield return new OperandDefinition(
            "PROPERTY_PATH",
            Rx(@"(?<!\.)\.([A-Za-z_][A-Za-z0-9_]*)\b"),
            (value, parameters) => value.Split('.')
                .Aggregate((Expression)parameters[0], (exp, prop)
                    => Expression.MakeMemberAccess(exp, exp.Type.GetRuntimeProperties()
                        .First(x => x.Name.Equals(prop,
                            StringComparison.OrdinalIgnoreCase)))));
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
