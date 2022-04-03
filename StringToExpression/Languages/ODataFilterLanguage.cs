using StringToExpression.GrammarDefinitions;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;

namespace StringToExpression.LanguageDefinitions;

/// <summary>
/// Provides the base class for parsing OData filter parameters.
/// </summary>
public class ODataFilterLanguage
{
    /// <summary>
    /// Access to common String Members
    /// </summary>
    protected static class StringMembers
    {
        /// <summary>
        /// The MethodInfo for the StartsWith method
        /// </summary>
        public static MethodInfo StartsWith = ReflectionUtil<string>.Method(x => x.StartsWith(""));

        /// <summary>
        /// The MethodInfo for the EndsWith method
        /// </summary>
        public static MethodInfo EndsWith = ReflectionUtil<string>.Method(x => x.EndsWith(""));

        /// <summary>
        /// The MethodInfo for the Contains method
        /// </summary>
        public static MethodInfo Contains = ReflectionUtil<string>.Method(x => x.Contains(""));

        /// <summary>
        /// The MethodInfo for the ToLower method
        /// </summary>
        public static MethodInfo ToLower = ReflectionUtil<string>.Method(x => x.ToLower());

        /// <summary>
        /// The MethodInfo for the ToUpper method
        /// </summary>
        public static MethodInfo ToUpper = ReflectionUtil<string>.Method(x => x.ToUpper());
    }

    /// <summary>
    /// Access to common DateTime Members
    /// </summary>
    protected static class DateTimeMembers
    {
        /// <summary>
        /// The MemberInfo for the Year property
        /// </summary>
        public static MemberInfo Year = ReflectionUtil<DateTime>.Member(x => x.Year);

        /// <summary>
        /// The MemberInfo for the Month property
        /// </summary>
        public static MemberInfo Month = ReflectionUtil<DateTime>.Member(x => x.Month);

        /// <summary>
        /// The MemberInfo for the Day property
        /// </summary>
        public static MemberInfo Day = ReflectionUtil<DateTime>.Member(x => x.Day);

        /// <summary>
        /// The MemberInfo for the Hour property
        /// </summary>
        public static MemberInfo Hour = ReflectionUtil<DateTime>.Member(x => x.Hour);

        /// <summary>
        /// The MemberInfo for the Minute property
        /// </summary>
        public static MemberInfo Minute = ReflectionUtil<DateTime>.Member(x => x.Minute);

        /// <summary>
        /// The MemberInfo for the Second property
        /// </summary>
        public static MemberInfo Second = ReflectionUtil<DateTime>.Member(x => x.Second);
    }

    private readonly Language _language;

    /// <summary>
    /// Initializes a new instance of the <see cref="ODataFilterLanguage"/> class.
    /// </summary>
    public ODataFilterLanguage()
        => _language = new(AllDefinitions().ToArray());

    /// <summary>
    /// Parses the specified text converting it into a predicate expression
    /// </summary>
    /// <typeparam name="T">The input type</typeparam>
    /// <param name="text">The text to parse.</param>
    /// <returns></returns>
    public Expression<Func<T, bool>> Parse<T>(string text)
    {
        var parameters = new[] { Expression.Parameter(typeof(T)) };
        var body = _language.Parse(text, parameters);

        ExpressionConversions.TryBoolean(ref body);

        return Expression.Lambda<Func<T, bool>>(body, parameters);
    }

    /// <summary>
    /// Returns all the definitions used by the language.
    /// </summary>
    /// <returns></returns>
    protected virtual IEnumerable<GrammarDefinition> AllDefinitions()
    {
        IEnumerable<FunctionCallDefinition> functions;
        var definitions = new List<GrammarDefinition>();
        definitions.AddRange(TypeDefinitions());
        definitions.AddRange(functions = FunctionDefinitions());
        definitions.AddRange(BracketDefinitions(functions));
        definitions.AddRange(LogicalOperatorDefinitions());
        definitions.AddRange(ArithmeticOperatorDefinitions());
        definitions.AddRange(PropertyDefinitions());
        definitions.AddRange(WhitespaceDefinitions());
        return definitions;
    }

    /// <summary>
    /// Returns the definitions for types used within the language.
    /// </summary>
    /// <returns></returns>
    protected virtual IEnumerable<GrammarDefinition> TypeDefinitions()
        => new[]
        {
            new OperandDefinition(
                "GUID",
                @"guid'[0-9A-Fa-f]{8}\-[0-9A-Fa-f]{4}\-[0-9A-Fa-f]{4}\-[0-9A-Fa-f]{4}\-[0-9A-Fa-f]{12}'",
                x => Expression.Constant(Guid.Parse(x.Substring("guid".Length).Trim('\'')))),

            new OperandDefinition(
                "STRING",
                @"'(?:\\.|[^'])*'",
                x => Expression.Constant(x.Trim('\'')
                    .Replace("\\'", "'")
                    .Replace("\\r", "\r")
                    .Replace("\\f", "\f")
                    .Replace("\\n", "\n")
                    .Replace("\\\\", "\\")
                    .Replace("\\b", "\b")
                    .Replace("\\t", "\t"))),
            new OperandDefinition(
                "BYTE",
                @"0x[0-9A-Fa-f]{1,2}",
                x => Expression.Constant(byte.Parse(x.Substring("0x".Length), NumberStyles.HexNumber | NumberStyles.AllowHexSpecifier))),
            new OperandDefinition(
                "NULL",
                @"null",
                _ => Expression.Constant(null)),
            new OperandDefinition(
                "BOOL",
                @"true|false",
                x => Expression.Constant(bool.Parse(x))),
            new OperandDefinition(
                "DATETIME",
                @"[Dd][Aa][Tt][Ee][Tt][Ii][Mm][Ee]'[^']+'",
                x => Expression.Constant(DateTime.Parse(x.Substring("datetime".Length).Trim('\'')))),
            new OperandDefinition(
                "DATETIMEOFFSET",
                @"datetimeoffset'[^']+'",
                x => Expression.Constant(DateTimeOffset.Parse(x.Substring("datetimeoffset".Length).Trim('\'')))),

            new OperandDefinition(
                "FLOAT",
                @"\-?\d+?\.\d*f",
                x => Expression.Constant(float.Parse(x.TrimEnd('f')))),
            new OperandDefinition(
                "DOUBLE",
                @"\-?\d+\.?\d*d",
                x => Expression.Constant(double.Parse(x.TrimEnd('d')))),
            new OperandDefinition(
                "DECIMAL_EXPLICIT",
                @"\-?\d+\.?\d*[m|M]",
                x => Expression.Constant(decimal.Parse(x.TrimEnd('m', 'M')))),
            new OperandDefinition(
                "DECIMAL",
                @"\-?\d+\.\d+",
                x => Expression.Constant(decimal.Parse(x))),

            new OperandDefinition(
                "LONG",
                @"\-?\d+L",
                x => Expression.Constant(long.Parse(x.TrimEnd('L')))),
            new OperandDefinition(
                "INTEGER",
                @"\-?\d+",
                x => Expression.Constant(int.Parse(x))),
        };

    /// <summary>
    /// Returns the definitions for logic operators used within the language.
    /// </summary>
    /// <returns></returns>
    protected virtual IEnumerable<GrammarDefinition> LogicalOperatorDefinitions()
        => new GrammarDefinition[]
        {
            new BinaryOperatorDefinition(
                "EQ",
                @"\b(eq)\b",
                11,
                ConvertEnumsIfRequired((left, right) => Expression.Equal(left, right))),
            new BinaryOperatorDefinition(
                "NE",
                @"\b(ne)\b",
                12,
                ConvertEnumsIfRequired((left, right) => Expression.NotEqual(left, right))),

            new BinaryOperatorDefinition(
                "GT",
                @"\b(gt)\b",
                13,
                (left, right) => Expression.GreaterThan(left, right)),
            new BinaryOperatorDefinition(
                "GE",
                @"\b(ge)\b",
                14,
                (left, right) => Expression.GreaterThanOrEqual(left, right)),

            new BinaryOperatorDefinition(
                "LT",
                @"\b(lt)\b",
                15,
                (left, right) => Expression.LessThan(left, right)),
            new BinaryOperatorDefinition(
                "LE",
                @"\b(le)\b",
                16,
                (left, right) => Expression.LessThanOrEqual(left, right)),

            new BinaryOperatorDefinition(
                "AND",
                @"\b(and)\b",
                17,
                (left, right) => Expression.And(left, right)),
            new BinaryOperatorDefinition(
                "OR",
                @"\b(or)\b",
                18,
                (left, right) => Expression.Or(left, right)),

            new UnaryOperatorDefinition(
                "NOT",
                @"\b(not)\b",
                19,
                RelativePosition.Right,
                (arg) => {
                    ExpressionConversions.TryBoolean(ref arg);
                    return Expression.Not(arg);
                })
        };

    /// <summary>
    /// Returns the definitions for arithmetic operators used within the language.
    /// </summary>
    /// <returns></returns>
    protected virtual IEnumerable<GrammarDefinition> ArithmeticOperatorDefinitions()
        => new[]
        {
            new BinaryOperatorDefinition(
                "ADD",
                @"\b(add)\b",
                2,
                (left, right) => Expression.Add(left, right)),
            new BinaryOperatorDefinition(
                "SUB",
                @"\b(sub)\b",
                2,
                (left, right) => Expression.Subtract(left, right)),
            new BinaryOperatorDefinition(
                "MUL",
                @"\b(mul)\b",
                1,
                (left, right) => Expression.Multiply(left, right)),
            new BinaryOperatorDefinition(
                "DIV",
                @"\b(div)\b",
                1,
                (left, right) => Expression.Divide(left, right)),
            new BinaryOperatorDefinition(
                "MOD",
                @"\b(mod)\b",
                1,
                (left, right) => Expression.Modulo(left, right)),
        };

    /// <summary>
    /// Returns the definitions for brackets used within the language.
    /// </summary>
    /// <param name="functionCalls">The function calls in the language. (used as opening brackets)</param>
    /// <returns></returns>
    protected virtual IEnumerable<GrammarDefinition> BracketDefinitions(IEnumerable<FunctionCallDefinition> functionCalls)
    {
        BracketOpenDefinition openBracket;
        ListDelimiterDefinition delimiter;
        return new GrammarDefinition[]
        {
            openBracket = new(
                "OPEN_BRACKET",
                @"\("),
            delimiter = new(
                "COMMA",
                ","),
            new BracketCloseDefinition(
                "CLOSE_BRACKET",
                @"\)",
                new[] { openBracket }.Concat(functionCalls),
                delimiter)
        };
    }

    /// <summary>
    /// Returns the definitions for functions used within the language.
    /// </summary>
    /// <returns></returns>
    protected virtual IEnumerable<FunctionCallDefinition> FunctionDefinitions()
        => new[]
        {
            new FunctionCallDefinition(
                "FN_STARTSWITH",
                @"startswith\(",
                new[] { typeof(string), typeof(string) },
                (parameters) => {
                    return Expression.Call(
                        parameters[0],
                        StringMembers.StartsWith, parameters[1]);
                }),
            new FunctionCallDefinition(
                "FN_ENDSWITH",
                @"endswith\(",
                new[] { typeof(string), typeof(string) },
                (parameters) => {
                    return Expression.Call(
                        parameters[0],
                        StringMembers.EndsWith, parameters[1]);
                }),
            new FunctionCallDefinition(
                "FN_SUBSTRINGOF",
                @"substringof\(",
                new[] { typeof(string), typeof(string) },
                (parameters) => {
                    return Expression.Call(
                        parameters[1],
                        StringMembers.Contains, parameters[0]);
                }),
            new FunctionCallDefinition(
                "FN_TOLOWER",
                @"tolower\(",
                new[] { typeof(string) },
                (parameters) => {
                    return Expression.Call(
                        parameters[0],
                        StringMembers.ToLower);
                }),
            new FunctionCallDefinition(
                "FN_TOUPPER",
                @"toupper\(",
                new[] { typeof(string) },
                (parameters) => {
                    return Expression.Call(
                        parameters[0],
                        StringMembers.ToUpper);
                }),

            new FunctionCallDefinition(
                "FN_DAY",
                @"day\(",
                new[] { typeof(DateTime) },
                (parameters) => {
                    return Expression.MakeMemberAccess(
                        parameters[0],
                        DateTimeMembers.Day);
                }),
            new FunctionCallDefinition(
                "FN_HOUR",
                @"hour\(",
                new[] { typeof(DateTime) },
                (parameters) => {
                    return Expression.MakeMemberAccess(
                        parameters[0],
                        DateTimeMembers.Hour);
                }),
            new FunctionCallDefinition(
                "FN_MINUTE",
                @"minute\(",
                new[] { typeof(DateTime) },
                (parameters) => {
                    return Expression.MakeMemberAccess(
                        parameters[0],
                        DateTimeMembers.Minute);
                }),
            new FunctionCallDefinition(
                "FN_MONTH",
                @"month\(",
                new[] { typeof(DateTime) },
                (parameters) => {
                    return Expression.MakeMemberAccess(
                        parameters[0],
                        DateTimeMembers.Month);
                }),
            new FunctionCallDefinition(
                "FN_YEAR",
                @"year\(",
                new[] { typeof(DateTime) },
                (parameters) => {
                    return Expression.MakeMemberAccess(
                        parameters[0],
                        DateTimeMembers.Year);
                }),
            new FunctionCallDefinition(
                "FN_SECOND",
                @"second\(",
                new[] { typeof(DateTime) },
                (parameters) => {
                    return Expression.MakeMemberAccess(
                        parameters[0],
                        DateTimeMembers.Second);
                }),
        };

    /// <summary>
    /// Returns the definitions for property names used within the language.
    /// </summary>
    /// <returns></returns>
    protected virtual IEnumerable<GrammarDefinition> PropertyDefinitions()
        => new[]
        {
            //Properties
            new OperandDefinition(
                "PROPERTY_PATH",
                @"(?<![0-9])([A-Za-z_][A-Za-z0-9_]*/?)+",
                (value, parameters) => {
                    return value.Split('/').Aggregate((Expression)parameters[0],
                        (exp, prop) => Expression.MakeMemberAccess(exp, TypeShim.GetProperty(exp.Type, prop)));
                }),
        };

    /// <summary>
    /// Returns the definitions for whitespace used within the language.
    /// </summary>
    /// <returns></returns>
    protected virtual IEnumerable<GrammarDefinition> WhitespaceDefinitions()
        => new[]
        {
            new GrammarDefinition("WHITESPACE", @"\s+", true)
        };


    /// <summary>
    /// Wraps the function to convert any constants to enums if required
    /// </summary>
    /// <param name="expFn">Function to wrap</param>
    /// <returns></returns>
    protected Func<Expression, Expression, Expression> ConvertEnumsIfRequired(Func<Expression, Expression, Expression> expFn)
        => (left, right) => {
            _ = ExpressionConversions.TryEnumNumberConvert(ref left, ref right)
                || ExpressionConversions.TryEnumStringConvert(ref left, ref right, true);

            return expFn(left, right);
        };
}
