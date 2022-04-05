using StringToExpression.Parser;
using StringToExpression.Tokenizer;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace StringToExpression.GrammarDefinitions;

/// <summary>
/// Represents a single piece of grammar and defines how it behaves within the system.
/// </summary>
public class GrammarDefinition : IEquatable<GrammarDefinition>
{
    private static readonly Regex NameValidation = new("^[a-zA-Z0-9_]+$");

    /// <summary>
    /// Name of the definition.
    /// </summary>
    public readonly string Name;

    /// <summary>
    /// Regex to match tokens.
    /// </summary>
    public readonly string Regex;

    /// <summary>
    /// Indicates whether this grammar should be ignored during tokenization.
    /// </summary>
    public readonly bool Ignore;

    /// <summary>
    /// Initializes a new instance of the <see cref="GrammarDefinition" /> class.
    /// </summary>
    /// <param name="name">The name of the definition.</param>
    /// <param name="regex">The regex to match tokens.</param>
    /// <param name="ignore">if set to <c>true</c> will ignore grammar during tokenization.</param>
    /// <exception cref="System.ArgumentNullException">
    /// name
    /// or
    /// regex
    /// </exception>
    /// <exception cref="StringToExpression.GrammarDefinitionInvalidNameException">When the name contains characters other than [a-zA-Z0-9_]</exception>
    public GrammarDefinition(string name, [RegexPattern] string regex, bool ignore = false)
    {
        if (name is null) throw new ArgumentNullException(nameof(name));
        if (!NameValidation.IsMatch(name))
            throw new GrammarDefinitionInvalidNameException(name);

        Name = name;
        Regex = regex ?? throw new ArgumentNullException(nameof(regex));
        Ignore = ignore;
    }

    /// <summary>
    /// Applies the token to the parsing state.
    /// </summary>
    /// <param name="token">The token to apply.</param>
    /// <param name="state">The state to apply the token to.</param>
    public virtual void Apply(Token token, ParseState state) { }

    public bool Equals(GrammarDefinition? other)
        => !ReferenceEquals(null, other)
            && (ReferenceEquals(this, other)
                || Name == other.Name);

    public override bool Equals(object? obj)
        => !ReferenceEquals(null, obj)
            && (ReferenceEquals(this, obj)
                || obj.GetType() == GetType()
                && Equals((GrammarDefinition)obj));

    public override int GetHashCode()
        => Name.GetHashCode();

    public static bool operator ==(GrammarDefinition? left, GrammarDefinition? right)
        => Equals(left, right);

    public static bool operator !=(GrammarDefinition? left, GrammarDefinition? right)
        => !Equals(left, right);
}
