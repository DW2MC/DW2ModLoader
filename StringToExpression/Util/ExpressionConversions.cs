using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace StringToExpression;

/// <summary>
/// Provides utilities for converting Expressions.
/// </summary>
public static class ExpressionConversions
{
    private static readonly Dictionary<Type, Type[]> ImplicitConversions = new()
    {
        { typeof(sbyte), new[] { typeof(sbyte), typeof(short), typeof(int), typeof(long), typeof(float), typeof(double), typeof(decimal) } },
        {
            typeof(byte),
            new[]
            {
                typeof(byte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float),
                typeof(double), typeof(decimal)
            }
        },
        { typeof(short), new[] { typeof(short), typeof(int), typeof(long), typeof(float), typeof(double), typeof(decimal) } },
        {
            typeof(ushort),
            new[] { typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal) }
        },
        { typeof(int), new[] { typeof(int), typeof(long), typeof(float), typeof(double), typeof(decimal) } },
        { typeof(uint), new[] { typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal) } },
        { typeof(long), new[] { typeof(long), typeof(float), typeof(double), typeof(decimal) } },
        {
            typeof(char),
            new[]
            {
                typeof(char), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double),
                typeof(decimal)
            }
        },
        { typeof(float), new[] { typeof(float), typeof(double), /*not safe, but convenient -->*/ typeof(decimal) } },
        { typeof(ulong), new[] { typeof(ulong), typeof(float), typeof(double), typeof(decimal) } },
        { typeof(double), new[] { typeof(double), /*not safe, but convenient -->*/ typeof(decimal) } },
        { typeof(decimal), new[] { typeof(decimal) } },
    };

    private static readonly Type[] EnumConversions = { typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong) };

    /// <summary>
    /// Tries a type that both passed types can safetly convert to.
    /// </summary>
    /// <param name="type1"></param>
    /// <param name="type2"></param>
    /// <param name="commonType">Type that both passed types can convert to.</param>
    /// <returns></returns>
    private static bool TryGetCommonType(Type type1, Type type2, [NotNullWhen(true)] out Type? commonType)
    {
        commonType = null;
        if (!ImplicitConversions.TryGetValue(type1, out var possibleType1))
            return false;
        if (!ImplicitConversions.TryGetValue(type2, out var possibleType2))
            return false;

        commonType = possibleType1.Intersect(possibleType2).FirstOrDefault();
        
        return commonType is not null;
    }

    /// <summary>
    /// Attempt to convert the expression into a boolean.
    /// </summary>
    /// <param name="exp"></param>
    /// <returns></returns>
    public static bool TryBoolean(ref Expression exp)
    {
        if (exp.Type == typeof(bool))
            return true;

        var left = exp;
        var right = (Expression)Expression.Constant(true);
        if (!TryImplicitlyConvert(ref left, ref right))
            return false;
        exp = Expression.Equal(left, right);
        return true;
    }

    /// <summary>
    /// If one expression is an enum and the other a number, will convert the number to the enum
    /// </summary>
    /// <param name="exp1">first expression.</param>
    /// <param name="exp2">second expression.</param>
    /// <returns>
    ///     <c>true</c> if expression are now of the same type; otherwise, <c>false</c>. 
    /// </returns>
    public static bool TryEnumNumberConvert(ref Expression exp1, ref Expression exp2)
    {
        //same type, nothing to do here
        if (exp1.Type == exp2.Type)
            return true;

        var type1 = Nullable.GetUnderlyingType(exp1.Type) ?? exp1.Type;
        var type2 = Nullable.GetUnderlyingType(exp2.Type) ?? exp2.Type;

        if (EnumConversions.Contains(type2) && type1.GetTypeInfo().IsEnum)
        {
            exp2 = Convert(exp2, exp1.Type);
            return true;
        }
        
        if (EnumConversions.Contains(type1) && type2.GetTypeInfo().IsEnum)
        {
            exp1 = Convert(exp1, exp2.Type);
            return true;
        }

        return false;
    }

    /// <summary>
    /// If one expression is an enum and the other a string, will convert the string to the enum
    /// </summary>
    /// <param name="exp1">first expression.</param>
    /// <param name="exp2">second expression.</param>
    /// <param name="ignoreCase">if set to <c>true</c> ignore case when parsing</param>
    /// <returns>
    ///   <c>true</c> if expression are now of the same type; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="NotSupportedException">When the string expression can not be evaluated as a constant.</exception>
    /// <exception cref="EnumParseException">When a string is unable to parse into the enumeration.</exception>
    public static bool TryEnumStringConvert(ref Expression exp1, ref Expression exp2, bool ignoreCase)
    {
        //same type, nothing to do here
        if (exp1.Type == exp2.Type)
            return true;

        var type1 = Nullable.GetUnderlyingType(exp1.Type) ?? exp1.Type;
        var type2 = Nullable.GetUnderlyingType(exp2.Type) ?? exp2.Type;

        bool ConvertStringExpression(Type enumType, Type enumUnderlyingType, ref Expression stringExpression)
        {
            var isNullable = enumType != enumUnderlyingType;

            //we will only support string constants to convert to enums. This prevents having enum
            //parse errors while evaluating the final expression
            if (!ExpressionEvaluator.TryEvaluate(stringExpression, out string? stringConstant))
                throw new NotSupportedException(
                    "Only string constants can be converted to an enum. String expressions can not be parsed as an enum.");

            //Enum.Parse will fail for null, however if we have a nullable enum a null is valid
            if (stringConstant is null && isNullable)
            {
                stringExpression = Convert(Expression.Constant(null), enumType);
                return true;
            }
            try
            {
                var parsedEnum = Enum.Parse(enumUnderlyingType, stringConstant!, ignoreCase);
                stringExpression = Convert(Expression.Constant(parsedEnum), enumType);
                return true;
            }
            catch (ArgumentException ex)
            {
                //enum parse failures give unhelpful errors, we will catch and rethrow an error with
                //some more details
                throw new EnumParseException(stringConstant!, enumType, ex);
            }

        }

        if (type2 == typeof(string) && type1.GetTypeInfo().IsEnum)
            return ConvertStringExpression(exp1.Type, type1, ref exp2);
        if (type1 == typeof(string) && type2.GetTypeInfo().IsEnum)
            return ConvertStringExpression(exp2.Type, type2, ref exp1);

        return false;
    }

    /// <summary>
    /// Attempts to perform the implicit conversion so both expressions are the same type.
    /// </summary>
    /// <param name="exp1">first expression.</param>
    /// <param name="exp2">second expression.</param>
    /// <returns>
    ///     <c>true</c> if a common type exists; otherwise, <c>false</c>.
    /// </returns>
    public static bool TryImplicitlyConvert(ref Expression exp1, ref Expression exp2)
    {
        var type1 = exp1.Type;
        var type2 = exp2.Type;

        //same type, nothing to do here
        if (type1 == type2)
            return true;

        var nullableType1 = Nullable.GetUnderlyingType(exp1.Type);
        var nullableType2 = Nullable.GetUnderlyingType(exp2.Type);
        var isType1Nullable = nullableType1 is not null || !type1.GetTypeInfo().IsValueType;
        var isType2Nullable = nullableType2 is not null || !type2.GetTypeInfo().IsValueType;

        type1 = nullableType1 ?? type1;
        type2 = nullableType2 ?? type2;

        Type? commonType;
        
        //if the underlying type is the same, the common type is
        //just the nullable version
        if ((isType1Nullable || isType2Nullable) && type1 == type2)
            commonType = typeof(Nullable<>).MakeGenericType(type1);
        
        //null constants always have type object,
        //if other type allows null convert it to the other type
        else if (isType1Nullable && IsNullConstant(exp2))
            commonType = exp1.Type;
        
        //null constants always have type object,
        //if other type allows null convert it to the other type
        else if (isType2Nullable && IsNullConstant(exp1))
            commonType = exp2.Type;
        
        else if (TryGetCommonType(type1, type2, out commonType))
        {
            //we have a common type, if we had a nullable type to begin
            //with convert our common type to a nullable as well
            if (isType1Nullable || isType2Nullable)
                commonType = typeof(Nullable<>).MakeGenericType(commonType);
        }
        else
            return false;

        exp1 = Convert(exp1, commonType);
        exp2 = Convert(exp2, commonType);
        return true;
    }

    /// <summary>
    /// Converts an expression to the given type only if it is not that type already.
    /// </summary>
    /// <param name="exp"></param>
    /// <param name="type"></param>
    /// <returns>Expression of the given type</returns>
    public static Expression Convert(Expression exp, Type type)
        => exp.Type == type
            ? exp
            : Expression.Convert(exp, type);

    /// <summary>
    /// Determines if expression is a null constant.
    /// </summary>
    /// <param name="exp">The exp.</param>
    /// <returns>
    ///   <c>true</c> if the expression is a null constant; otherwise, <c>false</c>.
    /// </returns>
    public static bool IsNullConstant(Expression exp)
    {
        if (exp is ConstantExpression constantExpression)
            return constantExpression.Value == null;
        return false;
    }
}
