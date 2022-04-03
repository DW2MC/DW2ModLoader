namespace StringToExpression;

public static class SubstringExtensions
{
    public static Substring Substring(this string s, int index)
        => new(s, index);

    public static Substring Substring(this string s, int index, int length)
        => new(s, index, length);
}
