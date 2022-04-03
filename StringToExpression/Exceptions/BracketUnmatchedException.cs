using JetBrains.Annotations;

namespace StringToExpression;

/// <summary>
/// Exception when a bracket does not have a match.
/// </summary>
[PublicAPI]
public class BracketUnmatchedException : ParseException
{
    /// <summary>
    /// String segment that contains the bracket that is unmatched.
    /// </summary>
    public readonly Substring BracketSubstring;

    /// <summary>
    /// Initializes a new instance of the <see cref="BracketUnmatchedException"/> class.
    /// </summary>
    /// <param name="bracketSubstring">The string segment that contains the bracket that is unmatched.</param>
    public BracketUnmatchedException(Substring bracketSubstring)
        : base(bracketSubstring, $"Bracket '{bracketSubstring}' is unmatched")
        => BracketSubstring = bracketSubstring;
}
