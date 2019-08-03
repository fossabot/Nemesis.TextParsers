﻿using System;
using System.Globalization;
using Nemesis.TextParsers.Runtime;

namespace Nemesis.TextParsers
{
    public interface ITransformer
    {
        object ParseObject(string text);
        object ParseObject(ReadOnlySpan<char> input);

        string FormatObject(object element);
    }

    public interface ISpanParser<out TElement>
    {
        TElement Parse(ReadOnlySpan<char> input);
    }
    public interface IFormatter<in TElement> { string Format(TElement element); }
    
    public interface ITransformer<TElement> : ISpanParser<TElement>, IFormatter<TElement>, ITransformer
    {
        TElement ParseFromText(string text);//for tests - ReadOnlySpan<char> cannot be boxed
    }

    public abstract class TransformerBase<TElement> : ITransformer<TElement>
    {
        public abstract TElement Parse(ReadOnlySpan<char> input);

        public abstract string Format(TElement element);


        TElement ITransformer<TElement>.ParseFromText(string text) => Parse(text.AsSpan());

        public object ParseObject(string text) => Parse(text.AsSpan());
        
        public object ParseObject(ReadOnlySpan<char> input) => Parse(input);

        public string FormatObject(object element) => Format((TElement) element);
    }

    public interface ICanTransformType
    {
        //bool CanHandle(Type type);
        Type Type { get; }
    }
    public interface ICanCreateTransformer
    {
        bool CanHandle(Type type);
        sbyte Priority { get; }
        ITransformer<TElement> CreateTransformer<TElement>();
    }

    public sealed class FormattableFormatter<TElement> : IFormatter<TElement> where TElement : IFormattable
    {
        public string Format(TElement element) =>
            element?.ToString(null, CultureInfo.InvariantCulture);

        public override string ToString() => $"Format {typeof(TElement).GetFriendlyName()} based on ToString(null, CultureInfo.InvariantCulture)";
    }

    public sealed class NormalFormatter<TElement> : IFormatter<TElement>
    {
        public string Format(TElement element) => element?.ToString();

        public override string ToString() => $"Format {typeof(TElement).GetFriendlyName()} based on ToString()";
    }

    public sealed class CompositionTransformer<TElement> : TransformerBase<TElement>
    {
        private readonly ISpanParser<TElement> _parser;
        private readonly IFormatter<TElement> _formatter;

        public CompositionTransformer(ISpanParser<TElement> parser, IFormatter<TElement> formatter)
        {
            _parser = parser;
            _formatter = formatter;
        }

        public override TElement Parse(ReadOnlySpan<char> input) => _parser.Parse(input);

        public override string Format(TElement element) => _formatter.Format(element);

        public override string ToString() => $"{_parser?.ToString() ?? ""};{_formatter?.ToString() ?? ""}";
    }
}
