using JetBrains.Annotations;

namespace StringToExpression;

/// <summary>
/// Exception when a mutlple grammars are configured with same name.
/// </summary>
[PublicAPI]
public class GrammarDefinitionDuplicateNameException : Exception
{
    /// <summary>
    /// The name that was duplicated.
    /// </summary>
    public readonly string GrammarDefinitionName;

    /// <summary>
    /// Initializes a new instance of the <see cref="GrammarDefinitionDuplicateNameException"/> class.
    /// </summary>
    /// <param name="grammarDefinitionName">Name of the duplicated grammar definition.</param>
    public GrammarDefinitionDuplicateNameException(string grammarDefinitionName) : base(
        $"Grammar definition name '{grammarDefinitionName}' has been defined multiple times")
        => GrammarDefinitionName = grammarDefinitionName;
}
