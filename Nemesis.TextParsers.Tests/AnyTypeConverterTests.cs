﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using NUnit.Framework;

namespace Nemesis.TextParsers.Tests
{
    [TestFixture]
    class AnyTypeConverterTests
    {
        [Test]
        public void CorrectUsage_SingleInstance()
        {
            var data = Enumerable.Range(1, 10).Select(i => new PointWithConverter(i * 10, i * -100)).ToList();
            var sut = TextTransformer.Default.GetTransformer<PointWithConverter>();


            var actualTexts = data.Select(pwc => sut.Format(pwc)).ToList();
            var actual = actualTexts.Select(text => sut.ParseFromText(text)).ToList();


            Assert.That(actual, Is.EquivalentTo(data));
        }

        [Test]
        public void CorrectUsage_Dict()
        {
            IDictionary<PointWithConverter, int> data = Enumerable.Range(1, 9)
                .Select(i => new PointWithConverter(i * 11, i * 100))
                .ToDictionary(p => p, p => p.X + p.Y);
            var sut = TextTransformer.Default.GetTransformer<IDictionary<PointWithConverter, int>>();


            var actualText = sut.Format(data);
            var actual = sut.ParseFromText(actualText);


            Assert.That(actual, Is.EquivalentTo(data));
        }

        [Test]
        public void BadConverter_NegativeTest()
        {
            var conv = TypeDescriptor.GetConverter(typeof(PointWithBadConverter));
            Assert.That(conv, Is.TypeOf<BadPointConverter>());

            Assert.That(
                () => TextTransformer.Default.GetTransformer<PointWithBadConverter>(),
                Throws.TypeOf<NotSupportedException>()
                    .With.Message.EqualTo("PointWithBadConverter is not supported for text transformation. Create appropriate chain of responsibility pattern element or provide a TypeConverter that can parse from/to string")
                );
        }
        
        [Test]
        public void NoConverter_NegativeTest()
        {
            var conv = TypeDescriptor.GetConverter(typeof(PointWithoutConverter));
            Assert.That(conv, Is.TypeOf<TypeConverter>());

            Assert.That(
                () => TextTransformer.Default.GetTransformer<PointWithoutConverter>(),
                Throws.TypeOf<NotSupportedException>()
                    .With.Message.EqualTo("PointWithoutConverter is not supported for text transformation. Type converter should be a subclass of TypeConverter but must not be TypeConverter itself")
                );
        }


        [TypeConverter(typeof(PointConverter))]
        [DebuggerDisplay("X = {" + nameof(X) + "}, Y = {" + nameof(Y) + "}")]
        struct PointWithConverter : IEquatable<PointWithConverter>
        {
            public int X { get; }
            public int Y { get; }

            public PointWithConverter(int x, int y) => (X, Y) = (x, y);

            public bool Equals(PointWithConverter other) => X == other.X && Y == other.Y;

            public override bool Equals(object obj) => obj is PointWithConverter other && Equals(other);

            public override int GetHashCode() => unchecked((X * 397) ^ Y);
        }

        sealed class PointConverter : TypeConverter
        {
            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) =>
                sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

            public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) =>
                destinationType == typeof(string) || base.CanConvertTo(context, destinationType);

            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) =>
                value is string text ? FromText(text) : default;

            public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) =>
                destinationType == typeof(string) && value is PointWithConverter pwc ?
                    $"{pwc.X};{pwc.Y}" :
                    base.ConvertTo(context, culture, value, destinationType);

            private static PointWithConverter FromText(string text) =>
                text.Split(';') is { } arr && arr.Length == 2
                    ? new PointWithConverter(int.Parse(arr[0]), int.Parse(arr[1]))
                    : default;
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
    }
}
