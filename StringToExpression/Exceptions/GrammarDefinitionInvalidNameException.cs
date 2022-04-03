using JetBrains.Annotations;

namespace StringToExpression;

/// <summary>
/// Exception when a grammar definition is configured with an invalid name.
/// </summary>
[PublicAPI]
public class GrammarDefinitionInvalidNameException : Exception
{
    /// <summary>
    /// The name that was invalid.
    /// </summary>
    public readonly string GrammarDefinitionName;

    /// <summary>
    /// Initializes a new instance of the <see cref="GrammarDefinitionInvalidNameException"/> class.
    /// </summary>
    /// <param name="grammarDefinitionName">Name of the grammar definition that was invalid.</param>
    public GrammarDefinitionInvalidNameException(string grammarDefinitionName) : base(
        $"Invalid grammar definition name '{grammarDefinitionName}' name may only contain [a-zA-Z0-9_]")
        => GrammarDefinitionName = grammarDefinitionName;
}
