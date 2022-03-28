using System.Linq.Expressions;
using System.Reflection;
using JetBrains.Annotations;

namespace DistantWorlds2.ModLoader;

[PublicAPI]
public static class ReflectionUtils
{
    public static MethodInfo Method<T>(Expression<Action<T>> a)
    {
        var body = a.Body;
        return body is MethodCallExpression mce
            ? mce.Method
            : throw new MissingMemberException("No method");
    }

    public static MethodInfo Method(Expression<Action> a)
    {
        var body = a.Body;
        return body is MethodCallExpression mce
            ? mce.Method
            : throw new MissingMemberException("No method");
    }

    public static ConstructorInfo Constructor(Expression<Action> a)
    {
        var body = a.Body;
        return body is NewExpression ne
            ? ne.Constructor
            : throw new MissingMemberException("No constructor");
    }

    public static MemberInfo Member<TResult>(Expression<Func<TResult>> a)
    {
        var body = a.Body;
        return body is MemberExpression me
            ? me.Member
            : throw new MissingMemberException("No member");
    }

    public static FieldInfo Field<TResult>(Expression<Func<TResult>> a)
        => Member(a) is FieldInfo fi
            ? fi
            : throw new MissingMemberException("Not a field");

    public static PropertyInfo Property<TResult>(Expression<Func<TResult>> a)
        => Member(a) is PropertyInfo pi
            ? pi
            : throw new MissingMemberException("Not a property");
}

[PublicAPI]
public static class ReflectionUtils<T>
{
    public static MemberInfo Member<TResult>(Expression<Func<T, TResult>> a)
    {
        var body = a.Body;
        return body is MemberExpression me
            ? me.Member
            : throw new MissingMemberException("No member");
    }

    public static FieldInfo Field<TResult>(Expression<Func<T, TResult>> a)
        => Member(a) is FieldInfo fi
            ? fi
            : throw new MissingMemberException("Not a field");

    public static PropertyInfo Property<TResult>(Expression<Func<T, TResult>> a)
        => Member(a) is PropertyInfo pi
            ? pi
            : throw new MissingMemberException("Not a property");
}
