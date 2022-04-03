using System.Collections;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using NuGet.Versioning;
using StringToExpression;
using StringToExpression.GrammarDefinitions;

namespace DistantWorlds2.ModLoader;

[PublicAPI]
public abstract class DslBase
{
    public Language Language => new(AllDefinitions().ToArray());

    // ReSharper disable ReturnValueOfPureMethodIsNotUsed
    private static readonly ConstantExpression ExprConstNumFmtInfoInvariant = Expression.Constant(NumberFormatInfo.InvariantInfo);

    private static readonly MethodInfo MiStringConcat = ReflectionUtils.Method(() => string.Concat("", ""));
    private static readonly MethodInfo MiStringContains = ReflectionUtils.Method<string>(s => s.Contains(""));
    private static readonly MethodInfo MiStringStartsWith = ReflectionUtils.Method<string>(s => s.StartsWith(""));
    private static readonly MethodInfo MiStringEndsWith = ReflectionUtils.Method<string>(s => s.EndsWith(""));

    private static readonly MethodInfo MiNuGetVersionParse = ReflectionUtils.Method(() => NuGetVersion.Parse(""));

    private static readonly MethodInfo MiRegexMatch = ReflectionUtils.Method(() => RegexMatch("", ""));
    private static readonly MethodInfo MiRegexReplace = ReflectionUtils.Method(() => RegexReplace("", ""));
    private static readonly MethodInfo MiRegexReplaceWith = ReflectionUtils.Method(() => RegexReplaceWith(default, ""));
    private static readonly MethodInfo MiStringRepeat = ReflectionUtils.Method(() => StringRepeat("", 0));
    private static readonly MethodInfo MiGetTypeStr = ReflectionUtils.Method(() => GetTypeString(null!));
    private static readonly MethodInfo MiVersionInRange = ReflectionUtils.Method(() => VersionInRange(default!, ""));

    private static readonly MethodInfo MiIConvertibleToDouble = ReflectionUtils.Method<IConvertible>(o => o.ToDouble(null));
    private static readonly MethodInfo MiIConvertibleToBoolean = ReflectionUtils.Method<IConvertible>(o => o.ToBoolean(null));

    private static readonly MethodInfo MiMathAbs = ReflectionUtils.Method(() => Math.Abs(0d));
    private static readonly MethodInfo MiMathSin = ReflectionUtils.Method(() => Math.Sin(0d));
    private static readonly MethodInfo MiMathCos = ReflectionUtils.Method(() => Math.Cos(0d));
    private static readonly MethodInfo MiMathAsin = ReflectionUtils.Method(() => Math.Asin(0d));
    private static readonly MethodInfo MiMathAcos = ReflectionUtils.Method(() => Math.Acos(0d));
    private static readonly MethodInfo MiMathTan = ReflectionUtils.Method(() => Math.Tan(0d));
    private static readonly MethodInfo MiMathAtan = ReflectionUtils.Method(() => Math.Atan(0d));
    private static readonly MethodInfo MiMathSqrt = ReflectionUtils.Method(() => Math.Sqrt(0d));
    private static readonly MethodInfo MiMathExp = ReflectionUtils.Method(() => Math.Exp(0d));
    private static readonly MethodInfo MiMathLog = ReflectionUtils.Method(() => Math.Log(0d));
    private static readonly MethodInfo MiMathRound = ReflectionUtils.Method(() => Math.Round(0d, MidpointRounding.AwayFromZero));
    private static readonly MethodInfo MiMathFloor = ReflectionUtils.Method(() => Math.Floor(0d));
    private static readonly MethodInfo MiMathCeiling = ReflectionUtils.Method(() => Math.Ceiling(0d));
    private static readonly MethodInfo MiMathTruncate = ReflectionUtils.Method(() => Math.Truncate(0d));
    private static readonly MethodInfo MiDoubleIsInfinity = ReflectionUtils.Method(() => double.IsInfinity(0d));
    private static readonly MethodInfo MiDoubleIsNaN = ReflectionUtils.Method(() => double.IsNaN(0d));

    private static readonly MethodInfo MiMathPow = ReflectionUtils.Method(() => Math.Pow(0d, 0d));
    private static readonly MethodInfo MiMathAtan2 = ReflectionUtils.Method(() => Math.Atan2(0d, 0d));


    private static readonly MethodInfo MiCount = ReflectionUtils.Method(() => Count(null!));
    private static readonly MethodInfo MiContains = ReflectionUtils.Method(() => Contains(null!, null!));
    // ReSharper restore ReturnValueOfPureMethodIsNotUsed

    private static readonly ConcurrentDictionary<string, Regex> RegexCache = new();

    public static readonly Regex RxBrackets = new(@"\[[^\]]\]+",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);

    private static Regex CacheRegex(string rx)
        => RegexCache.GetOrAdd(rx, r => new(r, RegexOptions.CultureInvariant | RegexOptions.Compiled));


    public static bool RegexMatch(string str, string rx)
    {
        var r = CacheRegex(rx);
        var m = r.IsMatch(str);
        return m;
    }

    public static (string, Regex) RegexReplace(string str, string rx)
        => (str, CacheRegex(rx));

    public static string RegexReplaceWith((string s, Regex rx) r, string with)
        => r.rx.Replace(r.s, with);

    public static string StringRepeat(string str, double count)
    {
        var strLength = str.Length;
        if (strLength * count > 2048) throw new ArgumentOutOfRangeException(nameof(count));
        return strLength switch
        {
            0 => str, 1 => new(str[0], checked((int)count)),
            _ => string.Concat(Enumerable.Repeat(str, checked((int)count)))
        };
    }

    public static double Count(object container)
    {
        switch (container)
        {
            case null: return 0;
            case string s: return s.Length;
            case ICollection c: return c.Count;
            case IEnumerable e: return e.Cast<object>().Count();
            default: throw new NotImplementedException();
        }
    }
    public static bool Contains(object container, object contained)
    {
        switch (container)
        {
            case null: return false;
            case string s: return s.Contains(contained.ToString());
            case IEnumerable e: {
                if (contained is not IConvertible c)
                    return e.Cast<object>().Contains(contained);
                switch (e)
                {
                    case ICollection { Count: 0 }: return false;
                    case Array a: {
                        var elemType = a.GetType().GetElementType()!;
                        var cv = c.ToType(elemType, NumberFormatInfo.InvariantInfo);
                        var cmpType = typeof(EqualityComparer<>).MakeGenericType(elemType);
                        var cmp = cmpType.InvokeMember("Default",
                            BindingFlags.GetProperty | BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Static,
                            null, null, null);
                        var cmpMethod = cmpType.GetMethod("Equals", BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance)!;
                        var cmpParams = new[] { null, cv };
                        foreach (var item in a)
                        {
                            cmpParams[0] = item;
                            if ((bool)cmpMethod.Invoke(cmp, cmpParams))
                                return true;
                        }
                        return false;
                    }
                }
                var eInterfaces = e.GetType().GetInterfaces();
                var itemType = eInterfaces
                    .FirstOrDefault(t => t.IsGenericType && typeof(IEnumerable<>) == t.GetGenericTypeDefinition())
                    ?.GetGenericArguments()[0];
                if (itemType is null)
                    return e.Cast<object>().Contains(contained);
                var v = c.ToType(itemType, NumberFormatInfo.InvariantInfo);
                var itemInterfaces = itemType.GetInterfaces();
                var eqInterface = itemInterfaces
                    .FirstOrDefault(t => t.IsGenericType && typeof(IEquatable<>) == t.GetGenericTypeDefinition());
                if (eqInterface is not null)
                {
                    var eqMethod = eqInterface.GetMethod("Equals")!;
                    var invokeParams = new[] { v };
                    foreach (var item in e)
                    {
                        if ((bool)eqMethod.Invoke(item, invokeParams))
                            return true;
                    }
                    return false;
                }
                var cmpInterface = itemInterfaces
                    .FirstOrDefault(t => t.IsGenericType && typeof(IComparable<>) == t.GetGenericTypeDefinition());
                if (cmpInterface is not null)
                {
                    var cmpMethod = cmpInterface.GetMethod("CompareTo")!;
                    var invokeParams = new[] { v };
                    foreach (var item in e)
                    {
                        if ((int)cmpMethod.Invoke(item, invokeParams) == 0)
                            return true;
                    }
                    return false;
                }
                throw new NotImplementedException();
            }
            default: throw new NotImplementedException();
        }
    }

    public static string GetTypeString(object o)
        => o switch
        {
            null => "null",
            string => "text",
            bool => "boolean",
            sbyte or byte
                or short or ushort
                or int or uint
                or long or ulong
                or float or double
                => "number",
            Tuple<string, Regex> => "string replace subexpression",
            ValueTuple<string, Regex> => "string replace subexpression",
            _ => o.GetType().FullName
        };

    public static bool VersionInRange(NuGetVersion semVer, string range)
        => VersionRange.TryParse(range, out var verRange)
            && verRange.Satisfies(semVer);

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
    private IEnumerable<GrammarDefinition> AllDefinitions()
    {
        IEnumerable<FunctionCallDefinition> functions;
        var definitions = new List<GrammarDefinition>();
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
    protected virtual IEnumerable<GrammarDefinition> TypeDefinitions()
    {
        //Only have double to make things easier for casting
        yield return new OperandDefinition(
            @"NUMBER",
            @"(?i)(?<![\w\)])[-+]?[0-9]*\.?[0-9]+(?:e[-+]?[0-9]+)?",
            n => Expression.Constant(double.Parse(n, NumberFormatInfo.InvariantInfo)));
        yield return new OperandDefinition(
            @"STRING",
            @"(?<![""\\])""(?:[^""\\]|\\.)*?""(?!=[""\\])",
            (value, _) => Expression.Constant(StringUtils.Unescape(value.Substring(1, value.Length - 2))));
        yield return new OperandDefinition(
            @"CONST_PI",
            @"(?i)\bPi\b",
            _ => Expression.Constant(Math.PI));
        yield return new OperandDefinition(
            @"CONST_PI_GREEK",
            @"(?i)\bπ\b",
            _ => Expression.Constant(Math.PI));
        yield return new OperandDefinition(
            @"CONST_PINF",
            @"(?i)(?<![\w\)])\+Inf\b",
            _ => Expression.Constant(double.PositiveInfinity));
        yield return new OperandDefinition(
            @"CONST_NINF",
            @"(?i)(?<![\w\)])\-Inf\b",
            _ => Expression.Constant(double.NegativeInfinity));
        yield return new OperandDefinition(
            @"CONST_NAN",
            @"(?i)\bNaN\b",
            _ => Expression.Constant(double.NaN));
        yield return new OperandDefinition(
            @"CONST_E",
            @"(?i)\be\b",
            _ => Expression.Constant(Math.E));
        yield return new OperandDefinition(
            @"CONST_TRUE",
            @"(?i)\btrue\b",
            _ => Expression.Constant(true));
        yield return new OperandDefinition(
            @"CONST_FALSE",
            @"(?i)\bfalse\b",
            _ => Expression.Constant(false));
    }

    /// <summary>
    /// Returns the definitions for arithmetic operators used within the language.
    /// </summary>
    /// <returns></returns>
    protected virtual IEnumerable<GrammarDefinition> OperatorDefinitions()
    {
        yield return new BinaryOperatorDefinition(
            @"ADD", @"\+", 2, Expression.Add);

        yield return new BinaryOperatorDefinition(
            @"SUB", @"\-", 2, Expression.Subtract);

        yield return new BinaryOperatorDefinition(
            @"MUL", @"\*", 1, Expression.Multiply);

        yield return new BinaryOperatorDefinition(
            @"DIV", @"\/", 1, Expression.Divide);

        yield return new BinaryOperatorDefinition(
            @"MOD", @"%", 1, Expression.Modulo);

        yield return new BinaryOperatorDefinition(
            @"POW", @"\^", 1, Expression.Power);

        yield return new BinaryOperatorDefinition(
            @"GT", @">", 3, Expression.GreaterThan);

        yield return new BinaryOperatorDefinition(
            @"LT", @"<", 3, Expression.LessThan);

        yield return new BinaryOperatorDefinition(
            @"LTE", @">=", 3, Expression.GreaterThanOrEqual);

        yield return new BinaryOperatorDefinition(
            @"GTE", @">=", 3, Expression.LessThanOrEqual);

        yield return new BinaryOperatorDefinition(
            @"IS_NOT", @"\bis\s+not\b", 6, Expression.NotEqual);

        yield return new BinaryOperatorDefinition(
            @"IS", @"\bis\b", 6, Expression.Equal);

        yield return new BinaryOperatorDefinition(
            @"AND_NOT", @"\band\s+not\b", 7,
            (a, b) => Expression.AndAlso(a,
                Expression.NotEqual(Expression.Constant(true), b)));

        yield return new BinaryOperatorDefinition(
            @"AND", @"\band\b", 7, Expression.AndAlso);

        yield return new BinaryOperatorDefinition(
            @"OR_NOT", @"\bor\s+not\b", 7,
            (a, b) => Expression.OrElse(a,
                Expression.NotEqual(Expression.Constant(true), b)));

        yield return new BinaryOperatorDefinition(
            @"OR", @"\bor\b", 8, Expression.OrElse);

        yield return new BinaryOperatorDefinition(
            @"CONCAT", @"(?<!\.)\.\.(?!\.)", 2,
            (a, b) => Expression.Call(MiStringConcat, a, b));

        yield return new BinaryOperatorDefinition(
            @"CONTAINS", @"\bcontains\b", 3,
            (a, b) => Expression.Call(
                MiContains,
                Expression.Convert(a, typeof(object)),
                Expression.Convert(b, typeof(object))));

        yield return new BinaryOperatorDefinition(
            @"STARTS", @"\bstarts\b", 3,
            (a, b) => Expression.Call(a, MiStringStartsWith, b));

        yield return new BinaryOperatorDefinition(
            @"ENDS", @"\bends\b", 3,
            (a, b) => Expression.Call(a, MiStringEndsWith, b));

        yield return new BinaryOperatorDefinition(
            @"REGEX_MATCH", @"\bmatches\b", 3,
            (a, b)
                => Expression.Call(MiRegexMatch, a, b));

        yield return new BinaryOperatorDefinition(
            @"REGEX_REPLACE", @"\breplace\b", 4,
            (a, b)
                => Expression.Call(MiRegexReplace, a, b));

        yield return new BinaryOperatorDefinition(
            @"REGEX_REPLACE_WITH", @"\bwith\b", 5,
            (a, b)
                => Expression.Call(MiRegexReplaceWith, a, b));

        yield return new BinaryOperatorDefinition(
            @"REPEAT", @"\brepeat\b", 1, (a, b)
                => Expression.Call(MiStringRepeat, a, b));

        yield return new BinaryOperatorDefinition(
            @"VERION_IN_RANGE", @"\bin\s+versions\b", 1, (a, b)
                => Expression.Call(MiVersionInRange, a, b));

    }

    /// <summary>
    /// Returns the definitions for brackets used within the language.
    /// </summary>
    /// <param name="functionCalls">The function calls in the language. (used as opening brackets)</param>
    /// <returns></returns>
    protected virtual IEnumerable<GrammarDefinition> BracketDefinitions(IEnumerable<FunctionCallDefinition> functionCalls)
    {
        BracketOpenDefinition openBrace;
        ListDelimiterDefinition delim;

        yield return openBrace = new(@"OPEN_BRACE",
            @"\(");

        yield return delim = new(@"COMMA",
            @",");

        yield return new BracketCloseDefinition(
            @"CLOSE_BRACE",
            @"\)",
            new[] { openBrace }
                .Concat(functionCalls),
            delim);
    }

    private static Expression UnaryDoubleArgMethodFuncDef(Expression arg, MethodInfo unaryMethod)
        => Expression.Call(
            null,
            unaryMethod,
            ToDoubleFuncDef(arg));

    private static Expression ToDoubleFuncDef(Expression arg)
    {
        var resultType = typeof(double);
        var resultVar = Expression.Variable(resultType, "result");
        return Expression.Block(resultType,
            new[] { resultVar },
            Expression.IfThenElse(
                Expression.TypeIs(arg, resultType),
                Expression.Assign(resultVar, Expression.Convert(arg, resultType)),
                Expression.Assign(resultVar, Expression.Call(Expression.Convert(arg, typeof(IConvertible)),
                    MiIConvertibleToDouble, ExprConstNumFmtInfoInvariant))),
            resultVar);
    }

    private static Expression ToStringFuncDef(Expression arg)
    {
        var resultType = typeof(string);
        var resultVar = Expression.Variable(resultType, "result");
        return Expression.Block(resultType,
            new[] { resultVar },
            Expression.IfThenElse(
                Expression.TypeIs(arg, resultType),
                Expression.Assign(resultVar, Expression.Convert(arg, resultType)),
                Expression.Assign(resultVar, Expression.Call(arg, nameof(ToString), Type.EmptyTypes))),
            resultVar);
    }

    private static Expression ToBoolFuncDef(Expression arg)
    {
        var resultType = typeof(bool);
        var resultVar = Expression.Variable(resultType, "result");
        return Expression.Block(resultType,
            new[] { resultVar },
            Expression.IfThenElse(
                Expression.TypeIs(arg, resultType),
                Expression.Assign(resultVar, Expression.Convert(arg, resultType)),
                Expression.Assign(resultVar, Expression.Call(Expression.Convert(arg, typeof(IConvertible)),
                    MiIConvertibleToBoolean, ExprConstNumFmtInfoInvariant))),
            resultVar);
    }

    private static Expression GetTypeStrFuncDef(Expression arg)
        => Expression.Call(MiGetTypeStr, arg);

    private static Expression ToVersionFuncDef(Expression arg)
    {
        var resultType = typeof(NuGetVersion);
        var resultVar = Expression.Variable(resultType, "result");
        return Expression.Block(resultType,
            new[] { resultVar },
            Expression.IfThenElse(
                Expression.TypeIs(arg, resultType),
                Expression.Assign(resultVar, Expression.Convert(arg, resultType)),
                Expression.Assign(resultVar, Expression.Call(MiNuGetVersionParse,
                    ToStringFuncDef(arg)))),
            resultVar);
    }

    /// <summary>
    /// Returns the definitions for functions used within the language.
    /// </summary>
    /// <returns></returns>
    protected virtual IEnumerable<FunctionCallDefinition> FunctionDefinitions()
    {
        yield return new FunctionCallDefinition(
            @"FN_ABS",
            @"(?i)\babs\(",
            new[] { typeof(double) },
            parameters => Expression.Call(
                null,
                MiMathAbs,
                parameters[0]));

        yield return new FunctionCallDefinition(
            @"FN_SIN",
            @"(?i)\bsin\(",
            new[] { typeof(double) },
            parameters => Expression.Call(
                null,
                MiMathSin,
                parameters[0]));

        yield return new FunctionCallDefinition(
            @"FN_ASIN",
            @"(?i)\basin\(",
            new[] { typeof(double) },
            parameters => Expression.Call(
                null,
                MiMathAsin,
                parameters[0]));

        yield return new FunctionCallDefinition(
            @"FN_COS",
            @"(?i)\bcos\(",
            new[] { typeof(double) },
            parameters => Expression.Call(
                null,
                MiMathCos,
                parameters[0]));

        yield return new FunctionCallDefinition(
            @"FN_ACOS",
            @"(?i)\bacos\(",
            new[] { typeof(double) },
            parameters => Expression.Call(
                null,
                MiMathAcos,
                parameters[0]));

        yield return new FunctionCallDefinition(
            @"FN_TAN",
            @"(?i)\btan\(",
            new[] { typeof(double) },
            parameters => Expression.Call(
                null,
                MiMathTan,
                parameters[0]));

        yield return new FunctionCallDefinition(
            @"FN_ATAN2",
            @"(?i)\batan2\(",
            new[] { typeof(double) },
            parameters => Expression.Call(
                null,
                MiMathAtan2,
                parameters[0], parameters[1]));

        yield return new FunctionCallDefinition(
            @"FN_POW",
            @"(?i)\bpow\(",
            new[] { typeof(double), typeof(double) },
            parameters => Expression.Call(
                null,
                MiMathPow,
                parameters[0], parameters[1]));

        yield return new FunctionCallDefinition(
            @"FN_SQRT",
            @"(?i)\bsqrt\(",
            new[] { typeof(double) },
            parameters => Expression.Call(
                null,
                MiMathSqrt,
                parameters[0]));

        yield return new FunctionCallDefinition(
            @"FN_EXP",
            @"(?i)\bexp\(",
            new[] { typeof(double) },
            parameters => Expression.Call(
                null,
                MiMathExp,
                parameters[0]));

        yield return new FunctionCallDefinition(
            @"FN_LOG",
            @"(?i)\blog\(",
            new[] { typeof(double) },
            parameters => Expression.Call(
                null,
                MiMathLog,
                parameters[0]));

        yield return new FunctionCallDefinition(
            @"FN_ROUND",
            @"(?i)\bround\(",
            new[] { typeof(double) },
            parameters => Expression.Call(
                null,
                MiMathRound,
                parameters[0], Expression.Constant(MidpointRounding.AwayFromZero)));

        yield return new FunctionCallDefinition(
            @"FN_FLOOR",
            @"(?i)\bfloor\(",
            new[] { typeof(double) },
            parameters => Expression.Call(
                null,
                MiMathFloor,
                parameters[0]));

        yield return new FunctionCallDefinition(
            @"FN_CEILING",
            @"(?i)\bceil\(",
            new[] { typeof(double) },
            parameters => Expression.Call(
                null,
                MiMathCeiling,
                parameters[0]));

        yield return new FunctionCallDefinition(
            @"FN_TRUNC",
            @"(?i)\btrunc\(",
            new[] { typeof(double) },
            parameters => Expression.Call(
                null,
                MiMathTruncate,
                parameters[0]));

        yield return new FunctionCallDefinition(
            @"FN_COUNT",
            @"(?i)\bcount\(",
            new[] { typeof(object) },
            parameters => Expression.Call(
                null,
                MiCount,
                parameters[0]));

        yield return new FunctionCallDefinition(
            "FN_IS_INF",
            @"(?i)\bisInf\(",
            new[] { typeof(double) },
            parameters => Expression.Call(
                null,
                MiDoubleIsInfinity,
                parameters[0]));

        yield return new FunctionCallDefinition(
            "FN_IS_NAN",
            @"(?i)\bisNaN\(",
            new[] { typeof(double) },
            parameters => Expression.Call(
                null,
                MiDoubleIsNaN,
                parameters[0]));

        yield return new FunctionCallDefinition(
            @"FN_TO_NUM",
            @"(?i)\bnum\(",
            new[] { typeof(object) },
            parameters => ToDoubleFuncDef(parameters[0]));

        /*yield return new FunctionCallDefinition(
            @"FN_TO_STR",
            @"(?i)\btxt\(",
            new[] { typeof(object) },
            parameters => ToStringFuncDef(parameters[0]));

        /*
        yield return new FunctionCallDefinition(
            @"FN_TO_BOOL",
            @"(?i)\bbool\(",
            new[] { typeof(object) },
            parameters => ToBoolFuncDef(parameters[0]));

        /*
        yield return new FunctionCallDefinition(
            @"FN_GET_TYPE_STR",
            @"(?i)\btype\(",
            new[] { typeof(object) },
            parameters => GetTypeStrFuncDef(parameters[0]));

        /*
        yield return new FunctionCallDefinition(
            @"FN_TO_VER",
            @"(?i)\bv\(",
            new[] { typeof(string) },
            parameters => ToVersionFuncDef(parameters[0]));
    }

    /// <summary>
    /// Returns the definitions for property names used within the language.
    /// </summary>
    /// <returns></returns>
    protected virtual IEnumerable<GrammarDefinition> PropertyDefinitions()
    {
        yield return new OperandDefinition(
            "SYMBOL_PATH",
            @"(?:(?<!\.)\b[A-Za-z_][A-Za-z0-9_]*\b)(?:\[[^\]]+\])?(?:(?<!\.)\.\b[A-Za-z_][A-Za-z0-9_]*(?:\[[^\]]+\])?)*\b(?!\()",
            SymbolPathExpressionBuilder);
    }

    private Expression SymbolPathExpressionBuilder(string text)
    {
        // first evaluate brackets
        var bracketMatches = RxBrackets.Matches(text);
        var bracketMatchCount = bracketMatches.Count;
        if (bracketMatchCount <= 0)
            return SymbolPathExpressionBuilderSansBrackets(text);

        var bracketSubExps = new Queue<Expression>(bracketMatchCount);
        for (var i = 0; i < bracketMatchCount; ++i)
        {
            var m = bracketMatches[i];
            bracketSubExps.Enqueue(Parse(text.Substring(m.Index, m.Length)).Body);
        }
        var parts = text.Split('.');
        var leftSym = parts.First();
        var expr = ResolveGlobalSymbol(leftSym, bracketSubExps);
        if (expr is null)
            throw new InvalidOperationException($"{leftSym} not found.");
        foreach (var sym in parts.Skip(1))
        {
            var bracketIndex = sym.IndexOf('[');
            if (bracketIndex != -1)
            {
                var subSym = sym.Substring(0, bracketIndex);
                expr = ResolveSubscript(bracketSubExps, Expression.PropertyOrField(expr, subSym));
                continue;
            }
            expr = Expression.PropertyOrField(expr, sym);
        }
        return expr;
    }

    private Expression SymbolPathExpressionBuilderSansBrackets(string text)
    {
        var parts = text.Split('.');
        var leftSym = parts.First();
        var expr = ResolveGlobalSymbol(leftSym);
        if (expr is null)
            throw new InvalidOperationException($"{leftSym} not found.");
        foreach (var sym in parts.Skip(1))
            expr = Expression.PropertyOrField(expr, sym);
        return expr;
    }

    /// <summary>
    /// Returns the definitions for whitespace used within the language.
    /// </summary>
    /// <returns></returns>
    protected virtual IEnumerable<GrammarDefinition> WhitespaceDefinitions()
    {
        yield return new GrammarDefinition("SPACE", @"\s+", true);
    }

    public ConcurrentDictionary<string, Expression> Globals = new();

    public ConcurrentDictionary<string, Expression> Variables = new();


    public virtual Expression? ResolveGlobalSymbol(string symbol, Queue<Expression> subExps)
    {
        var bracketIndex = symbol.IndexOf('[');
        if (bracketIndex == -1)
            return ResolveGlobalSymbol(symbol);

        symbol = symbol.Substring(0, bracketIndex);
        var expr = ResolveGlobalSymbol(symbol);
        return expr == null ? null : ResolveSubscript(subExps, expr);

    }
    private static Expression ResolveSubscript(Queue<Expression> subExps, Expression expr)
    {
        if (subExps is null) throw new ArgumentNullException(nameof(subExps));
        if (expr is null) throw new ArgumentNullException(nameof(expr));
        var exprType = expr.Type;
        if (exprType.IsArray)
            return Expression.ArrayAccess(expr, subExps.Dequeue());
        var indexer = ReflectionUtils.Indexer(exprType);
        if (indexer is not null)
            return Expression.MakeIndex(expr, indexer, new[] { subExps.Dequeue() });
        throw new NotImplementedException($"Subscripting {exprType.FullName}");
    }

    public virtual Expression? ResolveGlobalSymbol(string symbol)
        => Globals.TryGetValue(symbol, out var expr)
            ? expr
            : Variables.TryGetValue(symbol, out expr)
                ? expr
                : null;

    public object? GetGlobal(string symbol)
        => Globals.TryGetValue(symbol, out var e)
            ? e is ConstantExpression ce
                ? ce.Value
                : e
            : null;

    public void SetGlobal(string symbol, object? value)
    {
        if (value is null)
            Globals.TryRemove(symbol, out _);
        else
            Globals[symbol] = value is Expression e
                ? e
                : Expression.Constant(value);
    }

    public object? GetVariable(string symbol)
        => Variables.TryGetValue(symbol, out var e)
            ? e is ConstantExpression ce
                ? ce.Value
                : e
            : null;

    public void SetVariable(string symbol, object? value)
    {
        if (value is null)
            Variables.TryRemove(symbol, out _);
        else
            Variables[symbol] = value is Expression e
                ? e
                : Expression.Constant(value);
    }
}
