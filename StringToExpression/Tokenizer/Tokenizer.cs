using StringToExpression.GrammarDefinitions;
using System.Text.RegularExpressions;

namespace StringToExpression.Tokenizer;

/// <summary>
/// Converts a string into a stream of tokens
/// </summary>
public class Tokenizer
{
    /// <summary>
    /// Configuration of the tokens.
    /// </summary>
    public readonly IReadOnlyList<GrammarDefinition> GrammarDefinitions;

    /// <summary>
    /// Regex to identify tokens.
    /// </summary>
    protected readonly Regex TokenRegex;

    /// <summary>
    /// Initializes a new instance of the <see cref="Tokenizer"/> class.
    /// </summary>
    /// <param name="grammarDefinitions">The configuration for this language.</param>
    /// <exception cref="GrammarDefinitionDuplicateNameException">Thrown when two definitions have the same name.</exception>
    public Tokenizer(params GrammarDefinition[] grammarDefinitions)
    {
        //throw if we have any duplicates
        var duplicateKey = grammarDefinitions.GroupBy(x => x.Name)
            .FirstOrDefault(g => g.Count() > 1)?.Key;

        if (duplicateKey is not null)
            throw new GrammarDefinitionDuplicateNameException(duplicateKey);

        GrammarDefinitions = grammarDefinitions.ToList();

        var pattern = string.Join("|", GrammarDefinitions.Select(x => $"(?<{x.Name}>{x.Regex})"));
        TokenRegex = new(pattern, RegexOptions.Compiled|RegexOptions.CultureInvariant);
    }

    /// <summary>
    /// Convert text into a stream of tokens.
    /// </summary>
    /// <param name="text">text to tokenize.</param>
    /// <returns>stream of tokens.</returns>
    public IEnumerable<Token> Tokenize(string text)
    {
        var matches = TokenRegex.Matches(text).OfType<Match>();

        var expectedIndex = 0;
        foreach (var match in matches)
        {
            if (match.Index > expectedIndex)
                throw new GrammarUnknownException(new(text, expectedIndex, match.Index - expectedIndex));
            expectedIndex = match.Index + match.Length;

            var matchedTokenDefinition = GrammarDefinitions.First(x => match.Groups[x.Name].Success);
            
            if (matchedTokenDefinition.Ignore)
                continue;

            yield return new(
                matchedTokenDefinition,
                match.Value,
                new(text, match.Index, match.Length));
        }
        ;

    }
}
