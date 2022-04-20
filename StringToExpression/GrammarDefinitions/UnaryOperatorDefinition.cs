using FastExpressionCompiler.LightExpression;
using JetBrains.Annotations;

namespace StringToExpression.GrammarDefinitions;

/// <summary>
/// Represents an operator that takes a single operand.
/// </summary>
/// <seealso cref="StringToExpression.GrammarDefinitions.OperatorDefinition" />
public class UnaryOperatorDefinition : OperatorDefinition
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnaryOperatorDefinition"/> class.
    /// </summary>
    /// <param name="name">The name of the definition.</param>
    /// <param name="regex">The regex to match tokens.</param>
    /// <param name="orderOfPrecedence">The relative order this operator should be applied. Lower orders are applied first.</param>
    /// <param name="operandPosition">The relative positions where the single operand can be found.</param>
    /// <param name="expressionBuilder">The function given the single operand expressions, outputs a new operand.</param>
    public UnaryOperatorDefinition(string name,
        [RegexPattern] string regex,
        int orderOfPrecedence,
        RelativePosition operandPosition,
        Func<Expression, Expression> expressionBuilder)
        : base(
            name,
            regex,
            orderOfPrecedence,
            new[] { operandPosition },
            param => expressionBuilder(param[0])) { }
}
