using JetBrains.Annotations;

namespace StringToExpression;

/// <summary>
/// Exception thrown when there is a generic issue processing the Expressions. Usually caused by grammar definition configurations.
/// </summary>
[PublicAPI]
public class OperationInvalidException : ParseException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OperationInvalidException"/> class.
    /// </summary>
    /// <param name="errorSegment">The location that caused the exception.</param>
    /// <param name="innerException">The inner exception.</param>
    public OperationInvalidException(Substring errorSegment, Exception innerException)
        : base(errorSegment, $"Unable to perform operation '{errorSegment}'", innerException) { }
}
