using JetBrains.Annotations;
using StringToExpression.Parser;
using StringToExpression.Tokenizer;

namespace StringToExpression.GrammarDefinitions;

/// <summary>
/// Represents a closing bracket.
/// </summary>
/// <seealso cref="StringToExpression.GrammarDefinitions.GrammarDefinition" />
public class BracketCloseDefinition : GrammarDefinition
{
    /// <summary>
    /// The definitions that can be considered as the matching opening bracket.
    /// </summary>
    public readonly IReadOnlyCollection<BracketOpenDefinition> BracketOpenDefinitions;

    /// <summary>
    /// The definition for the delimiter for a list of items.
    /// </summary>
    public readonly GrammarDefinition? ListDelimiterDefinition;

    /// <summary>
    /// Initializes a new instance of the <see cref="BracketCloseDefinition"/> class.
    /// </summary>
    /// <param name="name">The name of the definition.</param>
    /// <param name="regex">The regex to match tokens.</param>
    /// <param name="bracketOpenDefinitions">The definitions that can be considered as the matching opening bracket.</param>
    /// <param name="listDelimiterDefinition">The definition for the delimiter for a list of items.</param>
    /// <exception cref="System.ArgumentNullException">bracketOpenDefinitions</exception>
    public BracketCloseDefinition(string name, [RegexPattern] string regex,
        IEnumerable<BracketOpenDefinition> bracketOpenDefinitions,
        GrammarDefinition? listDelimiterDefinition = null)
        : base(name, regex)
    {
        if (bracketOpenDefinitions is null)
            throw new ArgumentNullException(nameof(bracketOpenDefinitions));
        BracketOpenDefinitions = bracketOpenDefinitions.ToList();
        ListDelimiterDefinition = listDelimiterDefinition;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BracketCloseDefinition"/> class.
    /// </summary>
    /// <param name="name">The name of the definition.</param>
    /// <param name="regex">The regex to match tokens.</param>
    /// <param name="bracketOpenDefinition">The definition that can be considered as the matching opening bracket.</param>
    /// <param name="listDelimiterDefinition">The definition for the delimiter for a list of items.</param>
    public BracketCloseDefinition(string name, [RegexPattern] string regex,
        BracketOpenDefinition bracketOpenDefinition,
        GrammarDefinition? listDelimiterDefinition = null)
        : this(name, regex, new[] { bracketOpenDefinition }, listDelimiterDefinition) { }

    /// <summary>
    /// Applies the token to the parsing state. Will pop the operator stack executing all the operators storing each of the operands
    /// When we reach an opening bracket it will pass the stored operands to the opening bracket to be processed.
    /// </summary>
    /// <param name="token">The token to apply.</param>
    /// <param name="state">The state to apply the token to.</param>
    /// <exception cref="OperandExpectedException">When there are delimiters but no operands between them.</exception>
    /// <exception cref="BracketUnmatchedException">When there was no matching closing bracket.</exception>
    public override void Apply(Token token, ParseState state)
    {
        var bracketOperands = new Stack<Operand>();
        var previousSeparator = token.SourceMap;
        var hasSeparators = false;

        while (state.Operators.Count > 0)
        {
            var currentOperator = state.Operators.Pop();
            if (BracketOpenDefinitions.Contains(currentOperator.Definition))
            {
                var operand = state.Operands.Count > 0 ? state.Operands.Peek() : null;
                var firstSegment = currentOperator.SourceMap;
                if (operand is not null && operand.SourceMap.IsBetween(firstSegment, previousSeparator))
                    bracketOperands.Push(state.Operands.Pop());
                else if (hasSeparators && (operand is null || !operand.SourceMap.IsBetween(firstSegment, previousSeparator)))
                    // if we have separators then we should have something between the last separator and the open bracket.
                    throw new OperandExpectedException(Substring.Between(firstSegment, previousSeparator));

                // pass our all bracket operands to the open bracket method, he will know
                // what we should do.
                var closeBracketOperator = new Operator(this, token.SourceMap, () => { });
                ((BracketOpenDefinition)currentOperator.Definition).ApplyBracketOperands(
                    currentOperator,
                    bracketOperands,
                    closeBracketOperator,
                    state);
                return;
            }
            if (ListDelimiterDefinition is not null && currentOperator.Definition == ListDelimiterDefinition)
            {
                hasSeparators = true;
                var operand = state.Operands.Pop();

                // if our operator is not between two delimiters, then we are missing an operator
                var firstSegment = currentOperator.SourceMap;
                var secondSegment = previousSeparator;
                if (!operand.SourceMap.IsBetween(firstSegment, secondSegment))
                    throw new OperandExpectedException(Substring.Between(firstSegment, secondSegment));

                bracketOperands.Push(operand);
                previousSeparator = currentOperator.SourceMap;
            }
            else
                // regular operator, execute it
                currentOperator.Execute();

        }

        // We have popped through all the operators and not found an open bracket
        throw new BracketUnmatchedException(token.SourceMap);
    }
}
