using JetBrains.Annotations;

namespace StringToExpression;

/// <summary>
/// Exception when an unknown grammar is encountered.
/// </summary>
[PublicAPI]
public class GrammarUnknownException : ParseException
{
    /// <summary>
    /// string segment where the token was found.
    /// </summary>
    public readonly Substring UnexpectedGrammarSubstring;

    /// <summary>
    /// Initializes a new instance of the <see cref="GrammarUnknownException"/> class.
    /// </summary>
    /// <param name="unexpectedGrammarSubstring">The location of the unknown grammar.</param>
    public GrammarUnknownException(Substring unexpectedGrammarSubstring)
        : base(unexpectedGrammarSubstring, $"Unexpected token '{unexpectedGrammarSubstring}' found")
        => UnexpectedGrammarSubstring = unexpectedGrammarSubstring;
}
