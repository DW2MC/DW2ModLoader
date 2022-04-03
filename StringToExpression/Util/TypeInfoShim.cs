using System.Reflection;

namespace StringToExpression;

internal static class TypeShim
{
    public static PropertyInfo GetProperty(Type type, string property)
        => type.GetTypeInfo().GetProperty(property, BindingFlags.Instance | BindingFlags.IgnoreCase | BindingFlags.Public)
            ?? throw new MissingMemberException(type.FullName, property);
}
