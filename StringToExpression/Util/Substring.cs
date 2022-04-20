using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace StringToExpression;

/// <summary>
/// Represent a part of a string.
/// </summary>
[PublicAPI]
public readonly struct Substring : IComparable<string>, IEquatable<string>, IComparable<Substring>, IEquatable<Substring>
{
    /// <summary>
    /// The source string.
    /// </summary>
    public readonly string Source;

    /// <summary>
    /// The start position of this segment within the source string.
    /// </summary>
    public readonly int Start;

    /// <summary>
    /// The length of this segment.
    /// </summary>
    public readonly int Length;

    /// <summary>
    /// The end position of this segment within the source string.
    /// </summary>
    public int End => Start + Length;

    /// <summary>
    /// Initializes a new instance of the <see cref="Substring"/> class.
    /// </summary>
    /// <param name="sourceString">The source string.</param>
    /// <param name="start">The start position of this segment within the source string.</param>
    /// <param name="length">The length of this segment.</param>
    /// <exception cref="System.ArgumentNullException">sourceString</exception>
    public Substring(string sourceString, int start, int length)
    {
        Source = sourceString ?? throw new ArgumentNullException(nameof(sourceString));
        Start = start;
        Length = length;
    }
    /// <summary>
    /// Initializes a new instance of the <see cref="Substring"/> class.
    /// </summary>
    /// <param name="sourceString">The source string.</param>
    /// <param name="start">The start position of this segment within the source string.</param>
    /// <param name="length">The length of this segment.</param>
    /// <exception cref="System.ArgumentNullException">sourceString</exception>
    public Substring(string sourceString, int start)
        : this(sourceString, start, sourceString.Length - start) { }

    public bool IsEmpty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => IsNull || Length == 0;
    }

    public bool IsNull
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Source is null;
    }

    /// <summary>
    /// Determines if given segment is to the right of this segment.
    /// </summary>
    /// <param name="segment">segment to check.</param>
    /// <returns>
    ///   <c>true</c> if passed segment is to the right of this segment; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="System.ArgumentNullException">segment</exception>
    /// <exception cref="System.ArgumentException">segment - when this segment and passed segment have different source strings</exception>
    public bool IsRightOf(Substring segment)
    {
        if (Source != segment.Source)
            throw new ArgumentException($"{nameof(segment)} must have the same source string", nameof(segment));
        return segment.End <= Start;
    }

    /// <summary>
    /// Determines if index is to the right of this segment.
    /// </summary>
    /// <param name="index">index to check.</param>
    /// <returns>
    ///   <c>true</c> if index is to the right of this segment; otherwise, <c>false</c>.
    /// </returns>
    public bool IsRightOf(int index)
        => Start >= index;

    /// <summary>
    /// Determines if segment is to the left of this segment.
    /// </summary>
    /// <param name="segment">segment to check</param>
    /// <returns>
    ///   <c>true</c> if passed segment is to the left of this segment; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="System.ArgumentNullException">segment</exception>
    /// <exception cref="System.ArgumentException">segment -  when this segment and passed segment have different source strings</exception>
    public bool IsLeftOf(Substring segment)
    {
        if (Source != segment.Source)
            throw new ArgumentException($"{nameof(segment)} must have the same source string", nameof(segment));
        return segment.Start >= End;
    }

    /// <summary>
    /// Determines if index is to the left of this segment.
    /// </summary>
    /// <param name="index">index to check.</param>
    /// <returns>
    ///   <c>true</c> if index is to the left of this segment; otherwise, <c>false</c>.
    /// </returns>
    public bool IsLeftOf(int index)
        => End <= index;


    /// <summary>
    /// Create a segment that encompasses all the passed segments
    /// </summary>
    /// <param name="segments">segments to encompass</param>
    /// <returns>segment that enompasses all the passed segments</returns>
    /// <exception cref="System.ArgumentException">
    /// segments - when does not contain at least one item
    /// or
    /// segments - when all segments do not have the same source strings
    /// </exception>
    public static Substring Encompass(IEnumerable<Substring> segments)
    {

        using var enumerator = segments.GetEnumerator();
        if (!enumerator.MoveNext())
            throw new ArgumentException($"{nameof(segments)} must have at least one item", nameof(segments));

        var sourceString = enumerator.Current.Source;
        var minStart = enumerator.Current.Start;
        var maxEnd = enumerator.Current.End;
        while (enumerator.MoveNext())
        {
            if (enumerator.Current.Source != sourceString)
                throw new ArgumentException($"{nameof(segments)} must all have the same source string", nameof(segments));
            minStart = Math.Min(enumerator.Current.Start, minStart);
            maxEnd = Math.Max(enumerator.Current.End, maxEnd);
        }

        return new(sourceString, minStart, maxEnd - minStart);
    }

    /// <summary>
    /// Create a segment that encompasses all the passed segments
    /// </summary>
    /// <param name="segments">segments to encompass</param>
    /// <returns>segment that enompasses all the passed segments</returns>
    /// <exception cref="System.ArgumentException">
    /// segments - when does not contain at least one item
    /// or
    /// segments - when all segments do not have the same source strings
    /// </exception>
    public static Substring Encompass(params Substring[] segments)
        => Encompass((IEnumerable<Substring>)segments);

    /// <summary>
    /// Determines if this segment is between (and not within) the two passed segments.
    /// </summary>
    /// <param name="segment1">The first segment.</param>
    /// <param name="segment2">The second segment.</param>
    /// <returns>
    ///   <c>true</c> if this segment is between the two passed segments; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="System.ArgumentException">
    /// segment1 - when segment does not have the same source string
    /// or
    /// segment2 - when segment does not have the same source string
    /// </exception>
    public bool IsBetween(Substring segment1, Substring segment2)
    {
        if (Source != segment1.Source)
            throw new ArgumentException($"{nameof(segment1)} must have the same source string", nameof(segment1));
        if (Source != segment2.Source)
            throw new ArgumentException($"{nameof(segment2)} must have the same source string", nameof(segment2));
        return segment1.End <= Start && segment2.Start >= End;
    }

    /// <summary>
    /// Creates a segment which fits between (and not within) the two passed segments.
    /// </summary>
    /// <param name="segment1">The first segment.</param>
    /// <param name="segment2">The second segment.</param>
    /// <returns>A Substring which is between the two passed segments</returns>
    /// <exception cref="System.ArgumentException">when the two segments do not have the same soruce string</exception>
    public static Substring Between(Substring segment1, Substring segment2)
    {
        if (segment1.Source != segment2.Source)
            throw new ArgumentException($"{nameof(segment1)} and {nameof(segment2)} must the same source string");
        return new(
            segment1.Source,
            segment1.End,
            segment2.Start - segment1.End);
    }

    /// <inheritdoc cref="IComparable{T}.CompareTo"/>
    public int CompareTo(string? other)
    {
        if (IsNull)
            if (other is null)
                return 0;
        if (other is null)
            return 1;
        if (other == "")
            return IsEmpty ? 0 : 1;
        return string.Compare(
            Source, Start,
            other, 0,
            Math.Max(Length, other.Length));
    }

    /// <inheritdoc cref="IEquatable{T}.Equals(T?)"/>
    public bool Equals(string? other)
        => CompareTo(other) == 0;

    public int CompareTo(Substring other)
    {
        if (IsNull)
        {
            if (other.IsNull)
                return 0;
            return -1;
        }
        if (other.IsNull)
            return 1;
        if (other.IsEmpty)
            return IsEmpty ? 0 : 1;
        return string.Compare(
            Source, Start,
            other.Source, other.Start,
            Math.Max(Length, other.Length));
    }

    public bool Equals(Substring other)
        => CompareTo(other) == 0;

    /// <summary>
    /// Returns a <see cref="System.String" /> that represents this instance.
    /// </summary>
    /// <returns>
    /// A <see cref="System.String" /> that represents this instance.
    /// </returns>
    public override string ToString()
        => Source.Substring(Start, Length);

    public static implicit operator string(Substring substring)
        => substring.ToString();

    public static implicit operator Substring(string str)
        => new(str, 0, str.Length);
}
