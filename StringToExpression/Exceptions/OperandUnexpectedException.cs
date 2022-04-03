using JetBrains.Annotations;

namespace StringToExpression;

/// <summary>
/// Exception when an operand was found but not expected.
/// </summary>
[PublicAPI]
public class OperandUnexpectedException : ParseException
{
    /// <summary>
    /// String segment that contains the operator.
    /// </summary>
    public readonly Substring OperatorSubstring;

    /// <summary>
    /// String segment where the unexpected operators were found.
    /// </summary>
    public readonly Substring UnexpectedOperandSubstring;

    /// <summary>
    /// Initializes a new instance of the <see cref="OperandUnexpectedException"/> class.
    /// </summary>
    /// <param name="unexpectedOperandSubstring">The location that caused the exception.</param>
    public OperandUnexpectedException(Substring unexpectedOperandSubstring)
        : base(unexpectedOperandSubstring, $"Unexpected operands '{unexpectedOperandSubstring}' found. Perhaps an operator is missing")
    {
        UnexpectedOperandSubstring = unexpectedOperandSubstring;
        OperatorSubstring = default;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OperandUnexpectedException"/> class.
    /// </summary>
    /// <param name="operatorSubstring">The operator that was executing when the opreand was encountered.</param>
    /// <param name="unexpectedOperandSubstring">The location of the operand that was unexpected.</param>
    public OperandUnexpectedException(Substring operatorSubstring, Substring unexpectedOperandSubstring)
        : base(unexpectedOperandSubstring,
            $"Unexpected operands '{unexpectedOperandSubstring}' found while processing '{operatorSubstring}'. Perhaps an operator is missing")
    {
        OperatorSubstring = operatorSubstring;
        UnexpectedOperandSubstring = unexpectedOperandSubstring;
    }
}
