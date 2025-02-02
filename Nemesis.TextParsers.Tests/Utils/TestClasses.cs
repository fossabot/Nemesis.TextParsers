﻿using System.ComponentModel;
using System.Diagnostics;
using JetBrains.Annotations;
using Nemesis.TextParsers.Parsers;
using Nemesis.TextParsers.Utils;

namespace Nemesis.TextParsers.Tests.Utils;

#pragma warning disable IDE0250 // Make struct 'readonly'
[Flags]
public enum DaysOfWeek : byte
{
    None = 0,
    Monday
        = 0b0000_0001,
    Tuesday
        = 0b0000_0010,
    Wednesday
        = 0b0000_0100,
    Thursday
        = 0b0000_1000,
    Friday
        = 0b0001_0000,
    Saturday
        = 0b0010_0000,
    Sunday
        = 0b0100_0000,

    Weekdays = Monday | Tuesday | Wednesday | Thursday | Friday,
    Weekends = Saturday | Sunday,
    All = Weekdays | Weekends
}

[TypeConverter(typeof(PointConverter))]
internal readonly struct Point : IEquatable<Point>
{
    public int X { get; }
    public int Y { get; }

    public Point(int x, int y) => (X, Y) = (x, y);

    public bool Equals(Point other) => X == other.X && Y == other.Y;

    public override bool Equals(object obj) => obj is not null && obj is Point other && Equals(other);

    public override int GetHashCode() => unchecked(X * 397 ^ Y);

    public override string ToString() => $"{X};{Y}";

    public static Point FromText(ReadOnlySpan<char> text)
    {
        var enumerator = text.Split(';').GetEnumerator();

        if (!enumerator.MoveNext()) return default;
        int x = Int32Transformer.Instance.Parse(enumerator.Current);

        if (!enumerator.MoveNext()) return default;
        int y = Int32Transformer.Instance.Parse(enumerator.Current);

        return new(x, y);
    }
}

internal sealed class PointConverter : BaseTextConverter<Point>
{
    public override Point ParseString(string text) => Point.FromText(text.AsSpan());

    public override string FormatToString(Point value) => value.ToString();
}

internal readonly struct Rect : IEquatable<Rect>
{
    public int X { get; }
    public int Y { get; }
    public int Width { get; }
    public int Height { get; }

    public Rect(int x, int y, int width, int height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public bool Equals(Rect other) => X == other.X && Y == other.Y && Height == other.Height && Width == other.Width;

    public override bool Equals(object obj) => obj is not null && obj is Rect other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = X;
            hashCode = hashCode * 397 ^ Y;
            hashCode = hashCode * 397 ^ Height;
            hashCode = hashCode * 397 ^ Width;
            return hashCode;
        }
    }

    public static bool operator ==(Rect left, Rect right) => left.Equals(right);

    public static bool operator !=(Rect left, Rect right) => !left.Equals(right);

    public override string ToString() => $"{X};{Y};{Width};{Height}";

    [UsedImplicitly]
    public static Rect FromText(ReadOnlySpan<char> text)
    {
        var enumerator = text.Split(';').GetEnumerator();

        if (!enumerator.MoveNext()) return default;
        int x = Int32Transformer.Instance.Parse(enumerator.Current);

        if (!enumerator.MoveNext()) return default;
        int y = Int32Transformer.Instance.Parse(enumerator.Current);

        if (!enumerator.MoveNext()) return default;
        int width = Int32Transformer.Instance.Parse(enumerator.Current);

        if (!enumerator.MoveNext()) return default;
        int height = Int32Transformer.Instance.Parse(enumerator.Current);


        return new Rect(x, y, width, height);
    }
}

internal readonly struct ThreeLetters(char c1, char c2, char c3) : IEquatable<ThreeLetters>
{
    public char C1 { get; } = c1;
    public char C2 { get; } = c2;
    public char C3 { get; } = c3;

    public bool Equals(ThreeLetters other) => C1 == other.C1 && C2 == other.C2 && C3 == other.C3;

    public override bool Equals(object obj) => obj is not null && obj is ThreeLetters other && Equals(other);

    public override int GetHashCode() => (C1, C2, C3).GetHashCode();


    public static bool operator ==(ThreeLetters left, ThreeLetters right) => left.Equals(right);

    public static bool operator !=(ThreeLetters left, ThreeLetters right) => !left.Equals(right);

    public override string ToString() => $"{C1}{C2}{C3}";

    [UsedImplicitly]
    public static ThreeLetters FromText(ReadOnlySpan<char> text) =>
        text.Length == 3 ?
            new ThreeLetters(text[0], text[1], text[2]) : default;
}

internal readonly struct ThreeElements<TElement> : IEquatable<ThreeElements<TElement>>
    where TElement : IEquatable<TElement>
{
    public TElement E1 { get; }
    public TElement E2 { get; }
    public TElement E3 { get; }

    public ThreeElements(TElement e1, TElement e2, TElement e3)
    {
        E1 = e1;
        E2 = e2;
        E3 = e3;
    }

    public bool Equals(ThreeElements<TElement> other) =>
        E1.Equals(other.E1) && E2.Equals(other.E2) && E3.Equals(other.E3);

    public override bool Equals(object obj) =>
        obj is not null && obj is ThreeElements<TElement> other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = EqualityComparer<TElement>.Default.GetHashCode(E1);
            hashCode = hashCode * 397 ^ EqualityComparer<TElement>.Default.GetHashCode(E2);
            hashCode = hashCode * 397 ^ EqualityComparer<TElement>.Default.GetHashCode(E3);
            return hashCode;
        }
    }

    public static bool operator ==(ThreeElements<TElement> left, ThreeElements<TElement> right) => left.Equals(right);

    public static bool operator !=(ThreeElements<TElement> left, ThreeElements<TElement> right) => !left.Equals(right);

    public override string ToString() => FormattableString.Invariant($"{E1},{E2},{E3}");

    [UsedImplicitly]
    public static ThreeElements<TElement> FromText(ReadOnlySpan<char> text)
    {
        var trans = Sut.GetTransformer<TElement>();

        var tokens = text.Tokenize(',', '\\', true);
        var parsed = tokens.PreParse('\\', '∅', ',');

        var enumerator = parsed.GetEnumerator();
        {
            if (!enumerator.MoveNext()) throw GetException(0);
            var first = enumerator.Current.ParseWith(trans);

            if (!enumerator.MoveNext()) throw GetException(1);
            var second = enumerator.Current.ParseWith(trans);

            if (!enumerator.MoveNext()) throw GetException(2);
            var third = enumerator.Current.ParseWith(trans);

            //end of sequence
            if (enumerator.MoveNext()) throw GetException(4);

            return new ThreeElements<TElement>(first, second, third);
        }

        static Exception GetException(int numberOfElements) => new ArgumentException(
            $@"Sequence should contain 3 elements, but contained {(numberOfElements > 3 ? "more than 3" : numberOfElements.ToString())} elements");
    }
}

internal readonly struct Range<TElement>
{
    private const char SEPARATOR = '‥';
    private const char ESCAPING_SEQUENCE_START = '\\';
    private const char NULL_ELEMENT_MARKER = '∅';

    private static readonly IFormatter<TElement> _formatter = Sut.GetTransformer<TElement>();

    public TElement From { get; }
    public TElement To { get; }

    public Range(TElement from, TElement to)
    {
        From = from;
        To = to;
    }

    public override string ToString()
    {
        Span<char> initialBuffer = stackalloc char[32];
        var accumulator = new ValueSequenceBuilder<char>(initialBuffer);

        FormatElement(From, ref accumulator);
        accumulator.Append(SEPARATOR);
        FormatElement(To, ref accumulator);

        var text = accumulator.AsSpan().ToString();
        accumulator.Dispose();
        return text;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void FormatElement(TElement element, ref ValueSequenceBuilder<char> accumulator)
    {
        string elementText = _formatter.Format(element);
        if (elementText == null)
            accumulator.Append(NULL_ELEMENT_MARKER);
        else
        {
            foreach (char c in elementText)
            {
                if (c == ESCAPING_SEQUENCE_START || c == NULL_ELEMENT_MARKER || c == SEPARATOR)
                    accumulator.Append(ESCAPING_SEQUENCE_START);
                accumulator.Append(c);
            }
        }
    }

    [UsedImplicitly]
    public static Range<TElement> FromText(ReadOnlySpan<char> text)
    {
        var trans = Sut.GetTransformer<TElement>();

        var tokens = text.Tokenize(SEPARATOR, ESCAPING_SEQUENCE_START, true);
        var parsed = tokens.PreParse(ESCAPING_SEQUENCE_START, NULL_ELEMENT_MARKER, SEPARATOR);

        var enumerator = parsed.GetEnumerator();
        {
            if (!enumerator.MoveNext()) throw GetException(0);
            var from = enumerator.Current;

            if (!enumerator.MoveNext()) throw GetException(1);
            var to = enumerator.Current;

            if (enumerator.MoveNext()) throw GetException(3);//end of sequence

            return new Range<TElement>(from.ParseWith(trans), to.ParseWith(trans));
        }

        static Exception GetException(int numberOfElements) => new ArgumentException(
            $@"Sequence should contain 2 elements, but contained {(numberOfElements > 2 ? "more than 2" : numberOfElements.ToString())} elements");
    }
}

[TextFactory(typeof(PairTextFactory<>))]
internal readonly struct PairWithFactory<TElement> : IEquatable<PairWithFactory<TElement>>
    where TElement : IEquatable<TElement>
{
    public TElement Left { get; }
    public TElement Right { get; }

    public PairWithFactory(TElement left, TElement right)
    {
        Left = left;
        Right = right;
    }

    public bool Equals(PairWithFactory<TElement> other) => Left.Equals(other.Left) && Right.Equals(other.Right);

    public override bool Equals(object obj) =>
        obj is not null && obj is PairWithFactory<TElement> other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = EqualityComparer<TElement>.Default.GetHashCode(Left);
            hashCode = hashCode * 397 ^ EqualityComparer<TElement>.Default.GetHashCode(Right);
            return hashCode;
        }
    }

    public override string ToString() => FormattableString.Invariant($"{Left},{Right}");
}

internal static class PairTextFactory<TElement> where TElement : IEquatable<TElement>
{
    [UsedImplicitly]
    public static PairWithFactory<TElement> FromText(ReadOnlySpan<char> text)
    {
        var trans = Sut.GetTransformer<TElement>();

        var tokens = text.Tokenize(',', '\\', true);
        var parsed = tokens.PreParse('\\', '∅', ',');

        var enumerator = parsed.GetEnumerator();
        {
            if (!enumerator.MoveNext()) throw GetException(0);
            var left = enumerator.Current;

            if (!enumerator.MoveNext()) throw GetException(1);
            var right = enumerator.Current;

            //end of sequence
            if (enumerator.MoveNext()) throw GetException(3);

            return new PairWithFactory<TElement>(left.ParseWith(trans), right.ParseWith(trans));
        }

        static Exception GetException(int numberOfElements) => new ArgumentException(
            $@"Sequence should contain either 2, but contained {(numberOfElements > 2 ? "more than 2" : numberOfElements.ToString())} elements");
    }
}

internal enum Color { Red = 1, Blue = 2, Green = 3 }

[Flags]
internal enum Colors { None = 0, Red = 1, Blue = 2, Green = 4, RedAndBlue = Red | Blue }


[TypeConverter(typeof(OptionConverter))]
internal readonly struct Option
{
    public OptionEnum Value { get; }

    public Option(OptionEnum value) => Value = value;

    public override string ToString() => Value.ToString();
}

internal enum OptionEnum : byte { None, Option1, Option2, Option3 }

internal sealed class OptionConverter : BaseTextConverter<Option>, ITransformer<Option>
{
    public override Option ParseString(string text) =>
        text.ToLowerInvariant() switch
        {
            "option1" => new(OptionEnum.Option1),
            "o1" => new(OptionEnum.Option1),
            "option2" => new(OptionEnum.Option2),
            "o2" => new(OptionEnum.Option2),
            "option3" => new(OptionEnum.Option3),
            "o3" => new(OptionEnum.Option3),
            // "none"
            _ => new(OptionEnum.None)
        };

    public override string FormatToString(Option value) => value.ToString();




    public Option Parse(in ReadOnlySpan<char> text)
    {
        static bool TryParseInt(ReadOnlySpan<char> input, out int result) =>
            int.TryParse(
#if NETFRAMEWORK
                input.ToString()
#else
                input
#endif
                , out result);


        var input = text.Trim();

        return input.Length switch
        {
            2 when (input[0] == 'o' || input[0] == 'O') && char.IsDigit(input[1]) &&
                   int.Parse(input[1].ToString()) is { } i1 && i1 >= 1 && i1 <= 3 => new Option((OptionEnum)i1),

            7 when (input[0] == 'o' || input[0] == 'O') && (input[1] == 'p' || input[1] == 'P') &&
                   (input[2] == 't' || input[2] == 'T') && (input[3] == 'i' || input[3] == 'I') &&
                   (input[4] == 'o' || input[4] == 'O') && (input[5] == 'n' || input[5] == 'N') &&
                   TryParseInt(input.Slice(6, 1), out int i2) && i2 >= 1 && i2 <= 3 => new Option((OptionEnum)i2),

            _ => TryParseInt(input, out int i3) ? new Option((OptionEnum)i3) : new Option(OptionEnum.None)
        };
    }

    public bool TryParse(in ReadOnlySpan<char> input, out Option result)
    {
        try
        {
            result = Parse(input);
            return true;
        }
        catch (Exception)
        {
            result = default;
            return false;
        }
    }

    public Option Parse(string text) => Parse(text.AsSpan());

    public object ParseObject(string text) => Parse(text.AsSpan());

    public object ParseObject(in ReadOnlySpan<char> input) => Parse(input);
    public bool TryParseObject(in ReadOnlySpan<char> input, out object result)
    {
        try
        {
            result = ParseObject(input);
            return true;
        }
        catch (Exception)
        {
            result = default;
            return false;
        }
    }


    public string Format(Option element) => element.ToString();
    public string FormatObject(object element) => Format((Option)element);




    public Option GetEmpty() => new(OptionEnum.None);
    public object GetEmptyObject() => GetEmpty();


    public Option GetNull() => new(OptionEnum.None);
    public object GetNullObject() => GetNull();
}

[TypeConverter(typeof(PointConverter2))]
[DebuggerDisplay("X = {" + nameof(X) + "}, Y = {" + nameof(Y) + "}")]
readonly struct PointWithConverter : IEquatable<PointWithConverter>
{
    public int X { get; }
    public int Y { get; }

    public PointWithConverter(int x, int y) => (X, Y) = (x, y);

    public bool Equals(PointWithConverter other) => X == other.X && Y == other.Y;

    public override bool Equals(object obj) => obj is PointWithConverter other && Equals(other);

    public override int GetHashCode() => unchecked(X * 397 ^ Y);
}

sealed class PointConverter2 : BaseTextConverter<PointWithConverter>
{
    private static PointWithConverter FromText(ReadOnlySpan<char> text)
    {
        var enumerator = text.Split(';').GetEnumerator();

        if (!enumerator.MoveNext()) return default;
        int x = Int32Transformer.Instance.Parse(enumerator.Current);

        if (!enumerator.MoveNext()) return default;
        int y = Int32Transformer.Instance.Parse(enumerator.Current);

        return new PointWithConverter(x, y);
    }

    public override PointWithConverter ParseString(string text) => FromText(text.AsSpan());

    public override string FormatToString(PointWithConverter pwc) => $"{pwc.X};{pwc.Y}";
}

[TypeConverter(typeof(BadPointConverter))]
struct PointWithBadConverter
{
    public int X { get; }
    public int Y { get; }

    public PointWithBadConverter(int x, int y) => (X, Y) = (x, y);
}

sealed class BadPointConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) =>
        sourceType != typeof(string);

    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) =>
        destinationType != typeof(string);
}

struct PointWithoutConverter
{
    public int X { get; }
    public int Y { get; }

    public PointWithoutConverter(int x, int y) => (X, Y) = (x, y);
}
#pragma warning restore IDE0250 // Make struct 'readonly'