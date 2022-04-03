using JetBrains.Annotations;

namespace StringToExpression;

/// <summary>
/// Exception when a string can not be parsed as an enumeration
/// </summary>
[PublicAPI]
public class EnumParseException : Exception
{
    /// <summary>
    /// The string that was attempted to be parsed.
    /// </summary>
    public readonly Substring StringValue;

    /// <summary>
    /// The enumeration that the string was attempted to be parsed as.
    /// </summary>
    public readonly Type EnumType;

    public EnumParseException(Substring stringValue, Type enumType, Exception ex)
        : base($"'{stringValue}' is not a valid value for enum type '{enumType}'", ex)
    {
        StringValue = stringValue;
        EnumType = enumType;
    }
}
