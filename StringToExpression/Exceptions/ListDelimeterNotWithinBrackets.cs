using JetBrains.Annotations;

namespace StringToExpression;

/// <summary>
/// Exception when a list delimiter is not within brackets.
/// </summary>
[PublicAPI]
public class ListDelimiterNotWithinBrackets : ParseException
{
    /// <summary>
    /// string segment that contains the delimiter that is unconstrained.
    /// </summary>
    public readonly Substring DelimiterSubstring;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListDelimiterNotWithinBrackets"/> class.
    /// </summary>
    /// <param name="delimiterSubstring">The location where the delimiter was found.</param>
    public ListDelimiterNotWithinBrackets(Substring delimiterSubstring)
        : base(delimiterSubstring, $"List delimiter '{delimiterSubstring}' is not within brackets")
        => DelimiterSubstring = delimiterSubstring;
}
