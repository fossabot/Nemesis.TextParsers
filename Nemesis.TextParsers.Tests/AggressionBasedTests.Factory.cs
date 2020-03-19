﻿using NUnit.Framework;
using System;
using System.Collections.Generic;
using FacInt = Nemesis.TextParsers.Tests.AggressionBasedFactory<int>;
using FacIntCheck = Nemesis.TextParsers.Tests.AggressionBasedFactoryChecked<int>;
using System.Collections;
using Nemesis.TextParsers.Utils;
using Dss = System.Collections.Generic.Dictionary<string, string>;

namespace Nemesis.TextParsers.Tests
{
    [TestFixture(TestOf = typeof(IAggressionBased<>))]
    internal partial class AggressionBasedTests
    {
        private static IEnumerable<(int number, Type elemenType, string input, IEnumerable expectedOutput)> ValidValuesFor_FromText_Complex() => new[]
        {
            (01, typeof(int), @"1#2#3#4#5#6#7#8#9", (IEnumerable)new[]{1,2,3,4,5,6,7,8,9}),

            (02, typeof(int), @"1#1#1#4#4#4#7#7#7", new[]{1,1,1,4,4,4,7,7,7} ),
            (03, typeof(int), @"1#4#7", new[]{1,4,7} ),

            (04, typeof(int), @"1#1#1#1#1#1#1#1#1", new[]{1,1,1,1,1,1,1,1,1}),
            (05, typeof(int), @"1", new[]{1}),

            (06, typeof(List<int>), @"1|2|3#4|5|6#7|8|9", new[]{new List<int>{1,2,3},new List<int>{4,5,6},new List<int>{7,8,9}}),
            (07, typeof(List<int>), @"1|2|3#1|2|3#1|2|3", new[]{new List<int>{1,2,3},new List<int>{1,2,3},new List<int>{1,2,3}}),

            (08, typeof(IAggressionBased<int>), @"1\#2\#4#40\#50\#70#7\#8\#9", new[]
            {
                FacInt.FromPassiveNormalAggressive(1,2,4),
                FacInt.FromPassiveNormalAggressive(40,50,70),
                FacInt.FromPassiveNormalAggressive(7,8,9),
            }),
            (09, typeof(IAggressionBased<int>), @"1\#2\#40#1\#2\#40#1\#2\#40",new[]
            {
                FacInt.FromPassiveNormalAggressive(1,2,40),
                FacInt.FromPassiveNormalAggressive(1,2,40),
                FacInt.FromPassiveNormalAggressive(1,2,40)
            }),

            (10, typeof(Dss), @"Key=Text", new[] { new Dss { { "Key", @"Text" } } }),
        };

        [TestCaseSource(nameof(ValidValuesFor_FromText_Complex))]
        public void AggressionBasedFactory_FromText_ShouldParseComplexCases((int number, Type elementType, string input, IEnumerable expectedOutput) data)
        {
            var tester = (
                            GetType().GetMethod(nameof(AggressionBasedFactory_FromText_ShouldParseComplexCasesHelper), ALL_FLAGS) ??
                            throw new MissingMethodException(GetType().FullName, nameof(AggressionBasedFactory_FromText_ShouldParseComplexCasesHelper))
                        ).MakeGenericMethod(data.elementType);

            tester.Invoke(null, new object[] { data.input, data.expectedOutput });
        }

        private static void AggressionBasedFactory_FromText_ShouldParseComplexCasesHelper<TElement>(string input, IEnumerable expectedOutput)
        {
            var actual = AggressionBasedFactoryChecked<TElement>.FromText(input);

            Assert.That(actual, Is.Not.Null);
            Assert.That(actual, Is.AssignableTo<IAggressionValuesProvider<TElement>>());

            var values = ((IAggressionValuesProvider<TElement>)actual).Values;
            Assert.That(values, Is.EquivalentTo(expectedOutput));
        }

        private static IEnumerable<(string inputText, IEnumerable<int> inputValues, string expectedOutput, IEnumerable<int> expectedValuesCompacted, IEnumerable<int> expectedValues)> ValidValuesForFactory()
            => new (string, IEnumerable<int>, string, IEnumerable<int>, IEnumerable<int>)[]
            {
                (null,          null, "0", new []{0}, new []{0}),
                ("",            new int [0], @"0",new []{0},new []{0}),
                ("123",         new []{123}, @"123",new []{123},new []{123}),
                ("123#456#789", new []{123,456,789}, @"123#456#789",new []{123,456,789},new []{123,456,789}),
                ("123#123#123", new []{123,123,123}, @"123",new []{123},new []{123, 123, 123}),

                ("1#1#1#1#1#1#1#1#1", new []{1,1,1,1,1,1,1,1,1}, @"1",new []{1},new []{1,1,1,1,1,1,1,1,1}),
                ("1#1#1#4#4#4#7#7#7", new []{1,1,1,4,4,4,7,7,7}, @"1#4#7",new []{1,4,7},new []{1,1,1,4,4,4,7,7,7}),
                ("1#2#3#4#5#6#7#8#9", new []{1,2,3,4,5,6,7,8,9}, @"1#2#3#4#5#6#7#8#9", new []{1,2,3,4,5,6,7,8,9}, new []{1,2,3,4,5,6,7,8,9}),
            };

        [TestCaseSource(nameof(ValidValuesForFactory))]
        public void AggressionBasedFactory_FromValues_ShouldCreateAndCompactValues((string _, IEnumerable<int> inputValues, string expectedOutput, IEnumerable<int> expectedValuesCompacted, IEnumerable<int> expectedValues) data)
        {
            var actual = FacInt.FromValuesCompact(data.inputValues);

            Assert.That(actual, Is.Not.Null);

            Assert.That(actual.ToString(), Is.EqualTo(data.expectedOutput));

            Assert.That(((IAggressionValuesProvider<int>)actual).Values, Is.EquivalentTo(data.expectedValuesCompacted));
        }

        [TestCaseSource(nameof(ValidValuesForFactory))]
        public void AggressionBasedFactory_FromText_ShouldParse((string inputText, IEnumerable<int> _, string _s, IEnumerable<int> expectedValuesCompacted, IEnumerable<int> expectedValues) data)
        {
            var actual = FacIntCheck.FromText(data.inputText);

            Assert.That(actual, Is.Not.Null);

            Assert.That(((IAggressionValuesProvider<int>)actual).Values, Is.EquivalentTo(data.expectedValues));
        }

        private static IEnumerable<IEnumerable<int>> FromValues_Invalid() => new IEnumerable<int>[]
        {
            new []{1,2},
            new []{1,2,3,4},
            new []{1,2,3,4,5},
            new []{1,2,3,4,5,6},
            new []{1,2,3,4,5,6,7},
            new []{1,2,3,4,5,6,7,8},

            new []{1,2,3,4,5,6,7,8,9,10},
            new []{1,2,3,4,5,6,7,8,9,10,11},
        };

        [TestCaseSource(nameof(FromValues_Invalid))]
        public void AggressionBasedFactory_FromValues_NegativeTests(IEnumerable<int> values) =>
            Assert.Throws<ArgumentException>(() => FacInt.FromValuesCompact(values));
        
        [TestCaseSource(nameof(FromValues_Invalid))]
        public void AggressionBasedFactoryChecked_FromValues_NegativeTests(IEnumerable<int> values) =>
            Assert.Throws<ArgumentException>(() => FacIntCheck.FromValues(values));

        private const string AGG_BASED_STRING_SYNTAX =
            @"Hash ('#') delimited list with 1 or 3 (passive, normal, aggressive) elements i.e. 1#2#3
escape '#' with ""\#""and '\' with double backslash ""\\""

Elements syntax:
UTF-16 character string";

        private const string AGG_BASED_NULLABLE_INT_ARRAY_SYNTAX = @"Hash ('#') delimited list with 1 or 3 (passive, normal, aggressive) elements i.e. 1#2#3
escape '#' with ""\#""and '\' with double backslash ""\\""

Elements syntax:
Elements separated with pipe ('|') i.e.
1|2|3
(escape '|' with ""\|""and '\' with double backslash ""\\"")
Element syntax:
Whole number from -2147483648 to 2147483647 or null";

        private static IEnumerable<TestCaseData> GetSyntaxData() => new[]
        {
            new TestCaseData(typeof(IAggressionBased<string>), AGG_BASED_STRING_SYNTAX),
            new TestCaseData(typeof(AggressionBased3<string>), AGG_BASED_STRING_SYNTAX),
            new TestCaseData(typeof(AggressionBased3<int?[]>), AGG_BASED_NULLABLE_INT_ARRAY_SYNTAX),
        };

        private static string NormalizeNewLines(string s) => s?
            .Replace(Environment.NewLine, " ")
            .Replace("\n", " ")
            .Replace("\r", " ");

        [TestCaseSource(nameof(GetSyntaxData))]
        public void AggressionBased_GetSyntax(Type type, string expectedSyntax) =>
            Assert.That(
                NormalizeNewLines(TextConverterSyntaxAttribute.GetConverterSyntax(type)),
                Is.EqualTo(
                    NormalizeNewLines(expectedSyntax)
                    )
                );
    }
}
