﻿using System;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Nemesis.TextParsers.Runtime;
using Nemesis.TextParsers.Utils;

namespace Nemesis.TextParsers.Parsers
{
    [UsedImplicitly]
    public sealed class ValueTupleTransformerCreator : ICanCreateTransformer
    {
        private static readonly TupleHelper _helper = new TupleHelper(',', '∅', '\\', '(', ')');

        private readonly ITransformerStore _transformerStore;
        public ValueTupleTransformerCreator(ITransformerStore transformerStore) => _transformerStore = transformerStore;

        public ITransformer<TTuple> CreateTransformer<TTuple>()
        {
            if (!IsSupported(typeof(TTuple), out var elementTypes))
                throw new NotSupportedException($"Type {typeof(TTuple).GetFriendlyName()} is not supported by {GetType().Name}");

            int arity = elementTypes.Length;

            var transformerType = arity switch
            {
                1 => typeof(ValueTuple1Transformer<>),
                2 => typeof(ValueTuple2Transformer<,>),
                3 => typeof(ValueTuple3Transformer<,,>),
                4 => typeof(ValueTuple4Transformer<,,,>),
                5 => typeof(ValueTuple5Transformer<,,,,>),
                6 => typeof(ValueTuple6Transformer<,,,,,>),
                7 => typeof(ValueTuple7Transformer<,,,,,,>),
                8 => typeof(ValueTupleRestTransformer<,,,,,,,>),
                _ => throw new NotSupportedException($"Only ValueTuple with arity 1..{MAX_ARITY} are supported"),
            };
            transformerType = transformerType.MakeGenericType(elementTypes);

            var ctor = CheckParameters(transformerType, arity, elementTypes);

            var elementTransformers = elementTypes.Select(t => _transformerStore.GetTransformer(t));
            var transformerParams = new object[] { _helper }.Concat(elementTransformers).ToArray();

            return (ITransformer<TTuple>)ctor.Invoke(transformerParams);
        }

        private ConstructorInfo CheckParameters(Type transformerType, in int arity, Type[] elementTypes)
        {
            var ctors = transformerType.GetConstructors();
            if (ctors.Length != 1)
                throw new NotSupportedException($"Only single public constructor is supported by {GetType().Name}");
            var ctor = ctors[0];

            var ctorParams = ctor.GetParameters();
            if (ctorParams.Length != arity + 1 || ctorParams[0].ParameterType != typeof(TupleHelper))
                throw new NotSupportedException($"Constructor with {arity + 1} parameters with first being {nameof(TupleHelper)} is supported by {GetType().Name}");

            var expectedParameters = elementTypes.Select(t => typeof(ITransformer<>).MakeGenericType(t));
            var actualParameters = ctorParams.Skip(1).Select(p => p.ParameterType);

            if (false == expectedParameters.SequenceEqual(actualParameters))
                throw new NotSupportedException($"Remaining parameters are expected to be transformers for types (in order): {string.Join(", ", elementTypes.Select(t => t.GetFriendlyName()))}");

            return ctor;
        }


        private const byte MAX_ARITY = 8;

        public bool CanHandle(Type type) => IsSupported(type, out _);

        private bool IsSupported(Type type, out Type[] elementTypes) =>
            TryGetTupleElements(type, out elementTypes) &&
            elementTypes != null &&
            elementTypes.Length is { } arity && arity <= MAX_ARITY && arity >= 1 &&
            elementTypes.All(t => _transformerStore.IsSupportedForTransformation(t));

        private static bool TryGetTupleElements(Type type, out Type[] elementTypes)
        {
            bool isValueTuple = type.IsValueType && type.IsGenericType && !type.IsGenericTypeDefinition &&
#if NETSTANDARD2_0 || NETFRAMEWORK
            type.Namespace == "System" &&
            type.Name.StartsWith("ValueTuple`") &&
            typeof(ValueType).IsAssignableFrom(type);
#else
            typeof(System.Runtime.CompilerServices.ITuple).IsAssignableFrom(type);
#endif

            elementTypes = isValueTuple ? type.GenericTypeArguments : null;

            return isValueTuple;
        }

        public sbyte Priority => 12;
    }
}
