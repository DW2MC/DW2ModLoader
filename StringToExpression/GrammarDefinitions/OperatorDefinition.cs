using System.Linq.Expressions;
using JetBrains.Annotations;
using StringToExpression.Parser;
using StringToExpression.Tokenizer;

namespace StringToExpression.GrammarDefinitions;

/// <summary>
///  Represents a piece of grammar that defines an operator.
/// </summary>
/// <seealso cref="StringToExpression.GrammarDefinitions.GrammarDefinition" />
public class OperatorDefinition : GrammarDefinition
{
    /// <summary>
    /// A function given zero or more operands expressions, outputs a new operand.
    /// </summary>
    public readonly Func<Expression[], Expression> ExpressionBuilder;

    /// <summary>
    /// Positions where parameters can be found.
    /// </summary>
    public readonly IReadOnlyList<RelativePosition> ParamaterPositions;

    /// <summary>
    /// Relative order this operator should be applied. Lower orders are applied first.
    /// </summary>
    public readonly int? OrderOfPrecedence;

    /// <summary>
    /// Initializes a new instance of the <see cref="OperatorDefinition"/> class.
    /// </summary>
    /// <param name="name">The name of the definition.</param>
    /// <param name="regex">The regex to match tokens.</param>
    /// <param name="parameterPositions">The relative positions where parameters can be found.</param>
    /// <param name="expressionBuilder">The function given zero or more operands expressions, outputs a new operand.</param>
    /// <exception cref="System.ArgumentNullException">
    /// parameterPositions
    /// or
    /// expressionBuilder
    /// </exception>
    public OperatorDefinition(string name,
        [RegexPattern] string regex,
        IEnumerable<RelativePosition> parameterPositions,
        Func<Expression[], Expression> expressionBuilder)
        : this(name, regex, null, parameterPositions, expressionBuilder) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="OperatorDefinition"/> class.
    /// </summary>
    /// <param name="name">The name of the definition.</param>
    /// <param name="regex">The regex to match tokens.</param>
    /// <param name="orderOfPrecedence">The relative order this operator should be applied. Lower orders are applied first.</param>
    /// <param name="parameterPositions">The relative positions where parameters can be found.</param>
    /// <param name="expressionBuilder">The function given zero or more operands expressions, outputs a new operand.</param>
    /// <exception cref="System.ArgumentNullException">
    /// parameterPositions
    /// or
    /// expressionBuilder
    /// </exception>
    public OperatorDefinition(string name,
        [RegexPattern] string regex,
        int? orderOfPrecedence,
        IEnumerable<RelativePosition> parameterPositions,
        Func<Expression[], Expression> expressionBuilder)
        : base(name, regex)
    {
        if (parameterPositions == null)
            throw new ArgumentNullException(nameof(parameterPositions));

        ParamaterPositions = parameterPositions.ToList();
        ExpressionBuilder = expressionBuilder ?? throw new ArgumentNullException(nameof(expressionBuilder));
        OrderOfPrecedence = orderOfPrecedence;
    }

    /// <summary>
    /// Applies the token to the parsing state. Adds an operator to the state, when executed the operator will
    /// check it has enough operands and they are in the correct position. It will then execute the expressionBuilder
    /// placing the result in the state.
    /// </summary>
    /// <param name="token">The token to apply.</param>
    /// <param name="state">The state to apply the token to.</param>
    public override void Apply(Token token, ParseState state)
    {
        //Apply previous operators if they have a high precedence and they share an operand
        var anyLeftOperators = ParamaterPositions.Any(x => x == RelativePosition.Left);
        while (state.Operators.Count > 0 && OrderOfPrecedence is not null && anyLeftOperators)
        {
            var prevOperator = (OperatorDefinition)state.Operators.Peek().Definition;
            var prevOperatorPrecedence = prevOperator.OrderOfPrecedence;
            if (prevOperatorPrecedence <= OrderOfPrecedence && prevOperator.ParamaterPositions.Any(x => x == RelativePosition.Right))
                state.Operators.Pop().Execute();
            else
                break;
        }

        state.Operators.Push(new(this, token.SourceMap, () => {
            //Pop all our right arguments, and check there is the correct number and they are all to the right
            var rightArgs = new Stack<Operand>(state.Operands.PopWhile(x => x.SourceMap.IsRightOf(token.SourceMap)));
            var expectedRightArgs = ParamaterPositions.Count(x => x == RelativePosition.Right);
            if (expectedRightArgs > 0 && rightArgs.Count > expectedRightArgs)
            {
                var spanWhereOperatorExpected = Substring.Encompass(rightArgs
                    .Reverse()
                    .Take(rightArgs.Count - expectedRightArgs)
                    .Select(x => x.SourceMap));
                throw new OperandUnexpectedException(token.SourceMap, spanWhereOperatorExpected);
            }

            if (rightArgs.Count < expectedRightArgs)
                throw new OperandExpectedException(token.SourceMap, new(token.SourceMap.Source, token.SourceMap.End, 0));

            //Pop all our left arguments, and check they are not to the left of the next operator
            var nextOperatorEndIndex = state.Operators.Count == 0 ? 0 : state.Operators.Peek().SourceMap.End;
            var expectedLeftArgs = ParamaterPositions.Count(x => x == RelativePosition.Left);
            var leftArgs = new Stack<Operand>(state.Operands
                .PopWhile((x, i) => i < expectedLeftArgs && x.SourceMap.IsRightOf(nextOperatorEndIndex)
                ));

            if (leftArgs.Count < expectedLeftArgs)
                throw new OperandExpectedException(token.SourceMap, new(token.SourceMap.Source, token.SourceMap.Start, 0));

            //Map the operators into the correct argument positions
            var args = new List<Operand>();
            foreach (var paramPos in ParamaterPositions)
            {
                var operand = paramPos == RelativePosition.Right
                    ? rightArgs.Pop()
                    : leftArgs.Pop();
                args.Add(operand);
            }

            //our new source map will encompass this operator and all its operands
            var sourceMapSpan = Substring.Encompass(new[] { token.SourceMap }.Concat(args.Select(x => x.SourceMap)));

            Expression expression;
            try
            {
                expression = ExpressionBuilder(args.Select(x => x.Expression).ToArray());
            }
            catch (Exception ex)
            {
                throw new OperationInvalidException(sourceMapSpan, ex);
            }

            state.Operands.Push(new(expression, sourceMapSpan));
        }));
    }
}
