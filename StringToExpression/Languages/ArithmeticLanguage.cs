using System.Diagnostics.CodeAnalysis;
using StringToExpression.GrammarDefinitions;
using System.Linq.Expressions;

namespace StringToExpression.LanguageDefinitions;

/// <summary>
/// Represents a language to that handles basic mathematics.
/// </summary>
public class ArithmeticLanguage
{
    private readonly Language _language;

    /// <summary>
    /// Initializes a new instance of the <see cref="ArithmeticLanguage"/> class.
    /// </summary>
    [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
    public ArithmeticLanguage()
        => _language = new(AllDefinitions().ToArray());

    /// <summary>
    /// Parses the specified text converting it into a expression action.
    /// </summary>
    /// <param name="text">The text to parse.</param>
    /// <returns></returns>
    public Expression<Func<double>> Parse(string text)
    {
        var body = _language.Parse(text);
        body = ExpressionConversions.Convert(body, typeof(double));
        return Expression.Lambda<Func<double>>(body);
    }

    /// <summary>
    /// Parses the specified text converting it into an expression. The expression can take a single parameter
    /// </summary>
    /// <typeparam name="T">the type of the parameter.</typeparam>
    /// <param name="text">The text to parse.</param>
    /// <returns></returns>
    public Expression<Func<T, double>> Parse<T>(string text)
    {
        var parameters = new[] { Expression.Parameter(typeof(T)) };
        var body = _language.Parse(text, parameters);
        body = ExpressionConversions.Convert(body, typeof(double));
        return Expression.Lambda<Func<T, double>>(body, parameters);
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
        definitions.AddRange(ArithmaticOperatorDefinitions());
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
            //Only have double to make things easier for casting
            new OperandDefinition(
                "DECIMAL",
                @"\-?\d+(\.\d+)?",
                x => Expression.Constant(double.Parse(x))),
            new OperandDefinition(
                "PI",
                @"[Pp][Ii]",
                _ => Expression.Constant(Math.PI)),
        };

    /// <summary>
    /// Returns the definitions for arithmetic operators used within the language.
    /// </summary>
    /// <returns></returns>
    protected virtual IEnumerable<GrammarDefinition> ArithmaticOperatorDefinitions()
        => new[]
        {
            new BinaryOperatorDefinition(
                "ADD",
                @"\+",
                2,
                Expression.Add),
            new BinaryOperatorDefinition(
                "SUB",
                @"\-",
                2,
                Expression.Subtract),
            new BinaryOperatorDefinition(
                "MUL",
                @"\*",
                1,
                Expression.Multiply),
            new BinaryOperatorDefinition(
                "DIV",
                @"\/",
                1,
                Expression.Divide),
            new BinaryOperatorDefinition(
                "MOD",
                @"%",
                1,
                Expression.Modulo),
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
                new[] { openBracket }
                    .Concat(functionCalls),
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
                "FN_SIN",
                @"[Ss][Ii][Nn]\(",
                new[] { typeof(double) },
                (parameters) => {
                    return Expression.Call(
                        null,
                        ReflectionUtil<object>.Method(x => Math.Sin(0)), parameters[0]);
                }),
            new FunctionCallDefinition(
                "FN_COS",
                @"[Cc][Oo][Ss]\(",
                new[] { typeof(double) },
                (parameters) => {
                    return Expression.Call(
                        null,
                        ReflectionUtil<object>.Method(x => Math.Cos(0)), parameters[0]);
                }),
            new FunctionCallDefinition(
                "FN_TAN",
                @"[Tt][Aa][Nn]\(",
                new[] { typeof(double) },
                (parameters) => {
                    return Expression.Call(
                        null,
                        ReflectionUtil<object>.Method(x => Math.Tan(0)), parameters[0]);
                }),
            new FunctionCallDefinition(
                "FN_SQRT",
                @"[Ss][Qq][Rr][Tt]\(",
                new[] { typeof(double) },
                (parameters) => {
                    return Expression.Call(
                        null,
                        ReflectionUtil<object>.Method(x => Math.Sqrt(0)), parameters[0]);
                }),
            new FunctionCallDefinition(
                "FN_POW",
                @"[Pp][Oo][Ww]\(",
                new[] { typeof(double), typeof(double) },
                (parameters) => {
                    return Expression.Call(
                        null,
                        ReflectionUtil<object>.Method(x => Math.Pow(0, 0)),
                        new[] { parameters[0], parameters[1] });
                }),

        };

    /// <summary>
    /// Returns the definitions for property names used within the language.
    /// </summary>
    /// <returns></returns>
    protected virtual IEnumerable<GrammarDefinition> PropertyDefinitions()
        => new[]
        {
            new OperandDefinition(
                "PROPERTY_PATH",
                @"(?<![0-9])([A-Za-z_][A-Za-z0-9_]*\.?)+",
                (value, parameters) => {
                    return value.Split('.')
                        .Aggregate((Expression)parameters[0], (exp, prop)
                            => Expression.MakeMemberAccess(exp, TypeShim.GetProperty(exp.Type, prop)));
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
}
