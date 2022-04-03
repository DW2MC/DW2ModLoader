using System.Linq.Expressions;
using JetBrains.Annotations;

namespace StringToExpression.GrammarDefinitions;

/// <summary>
/// Represents an operator that has two parameters, one to the left and one to the right.
/// </summary>
/// <seealso cref="StringToExpression.GrammarDefinitions.OperatorDefinition" />
public class BinaryOperatorDefinition : OperatorDefinition
{
    private static readonly RelativePosition[] LeftRight = { RelativePosition.Left, RelativePosition.Right };

    /// <summary>
    /// Initializes a new instance of the <see cref="BinaryOperatorDefinition"/> class.
    /// </summary>
    /// <param name="name">The name of the definition.</param>
    /// <param name="regex">The regex to match tokens.</param>
    /// <param name="orderOfPrecedence">The relative order this operator should be applied. Lower orders are applied first.</param>
    /// <param name="expressionBuilder">The function given the single operand expressions, outputs a new operand.</param>
    public BinaryOperatorDefinition(string name,
        [RegexPattern] string regex,
        int orderOfPrecedence,
        Func<Expression, Expression, Expression> expressionBuilder)
        : base(
            name,
            regex,
            orderOfPrecedence,
            LeftRight,
            param => {
                var left = param[0];
                var right = param[1];
                ExpressionConversions.TryImplicitlyConvert(ref left, ref right);
                return expressionBuilder(left, right);
            }) { }
}
