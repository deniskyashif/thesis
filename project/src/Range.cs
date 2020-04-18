using System;
using System.Diagnostics.CodeAnalysis;

public class Range : IEquatable<Range>
{
    public static readonly Range All = new Range(char.MinValue, char.MaxValue);

    public Range(char ch) : this(ch, ch) { }

    public Range(char min, char max)
    {
        if (min > max)
            throw new ArgumentOutOfRangeException("Invalid char range");

        this.Min = min;
        this.Max = max;
    }

    public char Min { get; }
    public char Max { get; }

    public bool Includes(char ch) =>
        this.Min <= ch && ch <= this.Max;
    
    public Range Intersect(Range other)
    {
        var first = this;
        var second = other;

        if (this.Min > other.Min)
        {
            first = other;
            second = this;
        }

        if (first.Min <= second.Min) {
            var intMin = second.Min;
            var intMax = (char)Math.Min(first.Max, second.Max);

            if (intMin > intMax)
                return null;

            return new Range(intMin, intMax);
        }

        return null;
    }

    public bool Equals(Range other)
    {
        if (other == null)
            return false;

        return this.Min == other.Min && this.Max == other.Max;
    }

    public override int GetHashCode() => 
        this.Min.GetHashCode() ^ this.Max.GetHashCode();
    
    public override string ToString() =>
        this.Min == this.Max ? $"'{this.Min}'" : $"'{this.Min}'-'{this.Max}'";
}