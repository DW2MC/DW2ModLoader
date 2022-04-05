using JetBrains.Annotations;
using StringToExpression.Parser;
using StringToExpression.Tokenizer;

namespace StringToExpression.GrammarDefinitions;

/// <summary>
/// Represents the grammar that separates items in a list.
/// </summary>
/// <seealso cref="StringToExpression.GrammarDefinitions.GrammarDefinition" />
public class ListDelimiterDefinition : GrammarDefinition
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListDelimiterDefinition"/> class.
    /// </summary>
    /// <param name="name">The name of the definition.</param>
    /// <param name="regex">The regex to match tokens.</param>
    public ListDelimiterDefinition(string name, [RegexPattern] string regex)
        : base(name, regex) { }

    /// <summary>
    /// Applies the token to the parsing state. Adds an error operator, it is expected that a close bracket will consume the
    /// error operator before it gets executed.
    /// </summary>
    /// <param name="token">The token to apply.</param>
    /// <param name="state">The state to apply the token to.</param>
    public override void Apply(Token token, ParseState state)
        => state.Operators.Push(new(this, token.SourceMap,
            // if we ever executed this its because the corresponding close bracket didn't
            // pop it from the operators
            () => throw new ListDelimiterNotWithinBrackets(token.SourceMap)));
}
