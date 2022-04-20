using FastExpressionCompiler.LightExpression;

namespace StringToExpression;

/// <summary>
/// Parsers a stream of tokens into an expression.
/// </summary>
public class Parser
{
    /// <summary>
    /// Parses the specified tokens.
    /// </summary>
    /// <param name="tokens">The stream of tokens to parse.</param>
    /// <param name="parameters">Any parameters that should be accessible by the operands and operators.</param>
    /// <returns>An Expression that is the compiled state of all the tokens in the stream.</returns>
    public Expression Parse(IEnumerable<Token> tokens, IEnumerable<ParameterExpression>? parameters = null)
    {
        parameters = parameters ?? Enumerable.Empty<ParameterExpression>();

        var compileState = new ParseState();
        compileState.Parameters.AddRange(parameters);
        foreach (var token in tokens)
            token.Definition.Apply(token, compileState);

        var outputExpression = FoldOperators(compileState);
        return outputExpression;
    }

    private Expression FoldOperators(ParseState state)
    {
        while (state.Operators.Count > 0)
        {
            var op = state.Operators.Pop();
            op.Execute();
        }

        return state.Operands.Count switch
        {
            //if we dont have a single operand, then we probably had too many operands
            //and not enough operators in our input string
            0 => throw new OperandExpectedException(new("", 0, 0)),
            > 1 => throw new OperandUnexpectedException(state.Operands.Peek().SourceMap),
            _ => state.Operands.Peek().Expression
        };

    }
}
