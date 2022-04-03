using JetBrains.Annotations;

namespace StringToExpression;

/// <summary>
/// Exception when a function argument is not the expected type.
/// </summary>
[PublicAPI]
public class FunctionArgumentTypeException : ParseException
{
    /// <summary>
    /// String segment that contains the argument of incorrect type.
    /// </summary>
    public readonly Substring ArgumentSubstring;

    /// <summary>
    /// Argument type expected.
    /// </summary>
    public readonly Type ExpectedType;

    /// <summary>
    /// Argument type.
    /// </summary>
    public readonly Type ActualType;

    /// <summary>
    /// Initializes a new instance of the <see cref="FunctionArgumentTypeException"/> class.
    /// </summary>
    /// <param name="argumentSubstring">The location of the argument.</param>
    /// <param name="expectedType">The expected type of the argument.</param>
    /// <param name="actualType">The actual type of the argument.</param>
    public FunctionArgumentTypeException(Substring argumentSubstring, Type expectedType, Type actualType)
        : base(argumentSubstring, $"Argument '{argumentSubstring}' type expected {expectedType} but was {actualType}")
    {
        ArgumentSubstring = argumentSubstring;
        ExpectedType = expectedType;
        ActualType = actualType;
    }
}
