using StringToExpression.GrammarDefinitions;

namespace StringToExpression;

/// <summary>
/// An individual piece of the complete input.
/// </summary>
public class Token
{
    /// <summary>
    /// The type of token and how it is defined.
    /// </summary>
    public readonly GrammarDefinition Definition;

    /// <summary>
    /// The value stored within the token.
    /// </summary>
    public readonly string Value;

    /// <summary>
    /// The original string and position this token was extracted from.
    /// </summary>
    public readonly Substring SourceMap;

    /// <summary>
    /// Initializes a new instance of the <see cref="Token"/> class.
    /// </summary>
    /// <param name="definition">The type of token and how it was defined.</param>
    /// <param name="value">The value stored within the token.</param>
    /// <param name="sourceMap">The original string and position this token was extracted from.</param>
    public Token(GrammarDefinition definition, string value, Substring sourceMap)
    {
        Definition = definition;
        Value = value;
        SourceMap = sourceMap;
    }
}
