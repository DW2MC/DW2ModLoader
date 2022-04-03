using JetBrains.Annotations;

namespace StringToExpression;

/// <summary>
/// Exception when an operand was expected but not found.
/// </summary>
[PublicAPI]
public class OperandExpectedException : ParseException
{
    /// <summary>
    /// String segment that contains the operator.
    /// </summary>
    public readonly Substring OperatorSubstring;

    /// <summary>
    /// String segment where the operators were expected.
    /// </summary>
    public readonly Substring ExpectedOperandSubstring;

    /// <summary>
    /// Initializes a new instance of the <see cref="OperandExpectedException"/> class.
    /// </summary>
    /// <param name="expectedOperandSubstring">The location where the operand was expected to be.</param>
    public OperandExpectedException(Substring expectedOperandSubstring)
        : base(expectedOperandSubstring, $"Expected operands to be found")
    {
        OperatorSubstring = default;
        ExpectedOperandSubstring = expectedOperandSubstring;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OperandExpectedException"/> class.
    /// </summary>
    /// <param name="operatorSubstring">The operator that was expecting the operand.</param>
    /// <param name="expectedOperandSubstring">The location where the operand was expected to be.</param>
    public OperandExpectedException(Substring operatorSubstring, Substring expectedOperandSubstring)
        : base(expectedOperandSubstring, $"Expected operands to be found for '{operatorSubstring}'")
    {
        OperatorSubstring = operatorSubstring;
        ExpectedOperandSubstring = expectedOperandSubstring;
    }
}
