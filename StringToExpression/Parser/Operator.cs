using StringToExpression.GrammarDefinitions;

namespace StringToExpression;

/// <summary>
/// Represents an Operator.
/// </summary>
public class Operator
{
    /// <summary>
    /// Applies the operator, updating the ParseState.
    /// </summary>
    public readonly Action Execute;

    /// <summary>
    /// The original string and position this entire operand is from.
    /// </summary>
    public readonly Substring SourceMap;

    /// <summary>
    /// The grammar that defined this Operator.
    /// </summary>
    public readonly GrammarDefinition Definition;

    /// <summary>
    /// Initializes a new instance of the <see cref="Operator"/> class.
    /// </summary>
    /// <param name="definition">The grammar that defined this Operator.</param>
    /// <param name="sourceMap">The original string and position this entire operand is from.</param>
    /// <param name="execute">The action to run when applying this operator.</param>
    public Operator(GrammarDefinition definition, Substring sourceMap, Action execute)
    {
        Execute = execute;
        SourceMap = sourceMap;
        Definition = definition;
    }

    public override string ToString()
        => SourceMap;
}
