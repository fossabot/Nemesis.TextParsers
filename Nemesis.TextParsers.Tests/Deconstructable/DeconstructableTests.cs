﻿using System;
using System.Collections.Generic;
using Nemesis.Essentials.Runtime;
using NUnit.Framework;
using TCD = NUnit.Framework.TestCaseData;
using Sett = Nemesis.TextParsers.Parsers.DeconstructionTransformerSettings;
using static Nemesis.TextParsers.Tests.TestHelper;

namespace Nemesis.TextParsers.Tests.Deconstructable
{
    /*TODO
    
recursive tests (,,, (,)) + test for no borders 
       
        
    with (', null) sequence. 
    with '' empty tuple. 
    with empty text with no border. 
    with no border and trailing/leading spaces

        get rid of Console.WriteLine + Is.EquivalentOf


        
        test all thrown exceptions in DecoTrans
        test TupleHelper +test all thrown exceptions
update to https://www.nuget.org/packages/Microsoft.SourceLink.GitHub/
    */
    [TestFixture]
    internal class DeconstructableTests
    {
        private static readonly ITransformerStore _transformerStore = TextTransformer.Default;

        internal static IEnumerable<TCD> CorrectData() => new[]
        {
            new TCD(typeof(CarrotAndOnionFactors),
                    new CarrotAndOnionFactors(123.456789M, new[] { 1, 2, 3, (float)Math.Round(Math.PI, 2) }, TimeSpan.Parse("12:34:56")),
                    @"(123.456789;1|2|3|3.14;12:34:56)"),
            new TCD(typeof(Address), new Address("Wrocław", 52200), @"(Wrocław;52200)"),
            new TCD(typeof(Address), new Address("Wrocław)", 52200), @"(Wrocław);52200)"),
            new TCD(typeof(Address), new Address("(Wrocław)", 52200), @"((Wrocław);52200)"),
            new TCD(typeof(Address), new Address("A", 1), @"(A;1)"),
            new TCD(typeof(Person), new Person("Mike", 36, new Address("Wrocław", 52200)), @"(Mike;36;(Wrocław\;52200))"),

            new TCD(typeof(LargeStruct), LargeStruct.Sample, @"(3.14159265;2.718282;-123456789;123456789;-1234;1234;127;-127;-4611686018427387904;9223372036854775807;23.14069263;123456789012345678901234567890;(3.14159265\; 2.71828183))"),

        };

        [TestCaseSource(nameof(CorrectData))]
        public void FormatAndParse(Type type, object instance, string text)
        {
            var tester = Method.OfExpression<Action<object, string, Func<Sett, Sett>>>(
                (i, t, m) => FormatAndParseHelper(i, t, m)
            ).GetGenericMethodDefinition();

            tester = tester.MakeGenericMethod(type);

            tester.Invoke(null, new[] { instance, text, null });
        }

        private static void FormatAndParseHelper<TDeconstructable>(TDeconstructable instance, string text, Func<Sett, Sett> settingsMutator = null)
        {
            var settings = Sett.Default;
            settings = settingsMutator?.Invoke(settings) ?? settings;

            var sut = settings.ToTransformer<TDeconstructable>(_transformerStore);


            var actualFormatted = sut.Format(instance);
            Assert.That(actualFormatted, Is.EqualTo(text));


            var actualParsed1 = sut.Parse(text);
            var actualParsed2 = sut.Parse(actualFormatted);
            IsMutuallyEquivalent(actualParsed1, instance);
            IsMutuallyEquivalent(actualParsed2, instance);
            IsMutuallyEquivalent(actualParsed1, actualParsed2);
        }

        private static void ParseHelper<TDeconstructable>(TDeconstructable expected, string input, Func<Sett, Sett> settingsMutator = null)
        {
            var settings = Sett.Default;
            settings = settingsMutator?.Invoke(settings) ?? settings;

            var sut = settings.ToTransformer<TDeconstructable>(_transformerStore);


            var actualParsed1 = sut.Parse(input);
            var formatted1 = sut.Format(actualParsed1);
            var actualParsed2 = sut.Parse(formatted1);


            IsMutuallyEquivalent(actualParsed1, expected);
            IsMutuallyEquivalent(actualParsed2, expected);
            IsMutuallyEquivalent(actualParsed1, actualParsed2);
        }

        [Test]
        public void ParseAndFormat_CustomDeconstruct() => FormatAndParseHelper(
                new DataWithNoDeconstruct("Mike", "Wrocław", 36, 3.14), @"{Mike;Wrocław;36;3.14}",
                s => s.WithBorders('{', '}').WithCustomDeconstruction(DataWithNoDeconstructExt.DeconstructMethod,
                    DataWithNoDeconstructExt.Constructor));

        [Test]
        public void ParseAndFormat_DeconstructContravariance() =>
            FormatAndParseHelper(
                new ExternallyDeconstructable(10.1f, 555.55M), @"{10.1;555.55}",
                s => s.WithBorders('{', '}').WithCustomDeconstruction(ExternallyDeconstructableExt.DeconstructMethod,
                    ExternallyDeconstructableExt.Constructor));


        [Test]
        public void ParseAndFormat_NoBorders()
        {
            FormatAndParseHelper(
                new ThreeStrings("A", "B", "C"), @"A;B;C",
                s => s.WithoutBorders());

            FormatAndParseHelper(
                new ThreeStrings(" A", "B", "C "), @" A;B;C ",
                s => s.WithoutBorders());
        }

        [Test]
        public void ParseAndFormat_SameBorders()
        {
            FormatAndParseHelper(
                new ThreeStrings("A", "B", "C"), @"/A;B;C/",
                s => s.WithBorders('/', '/'));

            ParseHelper(
                new ThreeStrings("A", "B", "C"), @"    /A;B;C/    ",
                s => s.WithBorders('/', '/'));


            ParseHelper(
                new ThreeStrings("A", "B", "C"), @"    AA;B;CA    ",
                s => s.WithBorders('A', 'A'));
        }

        [Test]
        public void ParseAndFormat_MixedBorders_WithOptionalWhitespace()
        {
            FormatAndParseHelper(
                new ThreeStrings("A", "B", "C"), @"/A;B;C",
                s => s.WithoutBorders().WithStart('/')
                );

            FormatAndParseHelper(
                new ThreeStrings("A", "B", "C"), @"A;B;C/",
                s => s.WithoutBorders().WithEnd('/')
                );

            FormatAndParseHelper(
                new ThreeStrings(" A", "B", "C"), @" A;B;C/",
                s => s.WithoutBorders().WithEnd('/')
            );

            FormatAndParseHelper(
                new ThreeStrings(" A", "B", "C "), @"/ A;B;C ",
                s => s.WithoutBorders().WithStart('/')
            );
        }
        
        [Test]
        public void ParseAndFormat_NonStandardControlCharacters()
        {
            //(\∅;∅;\\t123\\ABC\\\∅DEF\∅GHI\;)
            var data = new ThreeStrings("∅␀", null, @"\t123\ABC\∅DEF∅GHI;/:");
            
            FormatAndParseHelper(data, @"(\∅␀;∅;\\t123\\ABC\\\∅DEF\∅GHI\;/:)");

            FormatAndParseHelper(data, @"(∅\␀;␀;\\t123\\ABC\\∅DEF∅GHI\;/:)",
                s=>s.WithNullElementMarker('␀')
                );
            
            FormatAndParseHelper(data, @"(/∅␀;∅;\t123\ABC\/∅DEF/∅GHI/;//:)",
                s=>s.WithEscapingSequenceStart('/')
                );

            FormatAndParseHelper(data, @"(\∅␀:∅:\\t123\\ABC\\\∅DEF\∅GHI;/\:)",
                s => s.WithDelimiter(':')
            );
            
            FormatAndParseHelper(data, @"(∅/␀:␀:\t123\ABC\∅DEF∅GHI;///:)",
                s => s.WithDelimiter(':')
                      .WithEscapingSequenceStart('/')
                      .WithNullElementMarker('␀')
            );
        }


        internal static IEnumerable<TCD> CustomDeconstructable_Data() => new[]
        {
            new TCD(new DataWithCustomDeconstructableTransformer(3.14f, false, new decimal[]{10,20,30}), @"{3.14_False_10|20|30}"),
            new TCD(new DataWithCustomDeconstructableTransformer(666, true, new decimal[]{6,7,8,9 }), null), //overriden by custom transformer 
            new TCD(new DataWithCustomDeconstructableTransformer(0.0f, false, new decimal[0]), ""), //overriden by deconstructable aspect convention
            
            new TCD(new DataWithCustomDeconstructableTransformer(3.14f, false, null), @"{3.14_False_␀}"),
            new TCD(new DataWithCustomDeconstructableTransformer(0.0f, false, null), @"{␀_False_␀}"),
            new TCD(new DataWithCustomDeconstructableTransformer(0.0f, false, null), @"{␀_␀_␀}"),
            new TCD(new DataWithCustomDeconstructableTransformer(0.0f, false, new decimal[0]), @"{__}"),
            
        };

        [TestCaseSource(nameof(CustomDeconstructable_Data))]
        public void Custom_ConventionTransformer_BasedOnDeconstructable(DataWithCustomDeconstructableTransformer instance, string text)
        {
            var sut = _transformerStore.GetTransformer<DataWithCustomDeconstructableTransformer>();

            
            var actualParsed1 = sut.Parse(text);
            
            var formattedInstance = sut.Format(instance);
            var formattedActualParsed1 = sut.Format(actualParsed1);
            
            Assert.That(formattedActualParsed1, Is.EqualTo(formattedInstance));

            var actualParsed2 = sut.Parse(formattedInstance);

            IsMutuallyEquivalent(actualParsed1, instance);
            IsMutuallyEquivalent(actualParsed2, instance);
            IsMutuallyEquivalent(actualParsed1, actualParsed2);
        }

        [TestCase(@"(Mike;36;(Wrocław;52200))", typeof(ArgumentException), "These requirements were not met in:'(Wrocław'")]
        [TestCase(@"(Mike;36;(Wrocław);52200))", typeof(ArgumentException), "2nd element was not found after 'Wrocław'")]
        [TestCase(@"(Mike;36;(Wrocław\;52200);123))", typeof(ArgumentException), "cannot have more than 3 elements: '123)'")]
        public void Parse_NegativeTest(string wrongInput, Type expectedException, string expectedMessagePart)
        {
            var sut = Sett.Default
                .ToTransformer<Person>(_transformerStore);

            bool passed = false;
            Person parsed = default;
            try
            {
                parsed = sut.Parse(wrongInput);
                passed = true;
            }
            catch (Exception actual)
            {
                AssertException(actual, expectedException, expectedMessagePart);
            }
            if (passed)
                Assert.Fail($"'{wrongInput}' should not be parseable to:{Environment.NewLine}\t{parsed}");
        }
    }
}
