﻿using JetBrains.Annotations;
using Nemesis.TextParsers.Runtime;
using Nemesis.TextParsers.Settings;

namespace Nemesis.TextParsers.Parsers;

[UsedImplicitly]
public sealed class TextFactoryTransformerCreator : FactoryMethodTransformerCreator
{
    public TextFactoryTransformerCreator([NotNull] FactoryMethodSettings settings)
        : base(settings) { }

    protected override Type GetFactoryMethodContainer(Type type)
    {
        Type factoryType = type.GetCustomAttribute<TextFactoryAttribute>()?.FactoryType;
        if (factoryType == null) return null;
        if (factoryType.IsGenericTypeDefinition)
        {
            if (type.IsGenericTypeDefinition)
                throw new NotSupportedException($"Text transformation for GenericTypeDefinition is not supported: {type.GetFriendlyName()}");

            factoryType = type.IsGenericType ?
                factoryType.MakeGenericType(type.GenericTypeArguments) :
                factoryType.MakeGenericType(type);
        }
        return factoryType;
    }

    protected override MethodInfo PrepareParseMethod(MethodInfo method, Type elementType)
    {
        if (method.IsGenericMethodDefinition)
        {
            method = elementType.IsGenericType ?
                method.MakeGenericMethod(elementType.GenericTypeArguments) :
                method.MakeGenericMethod(elementType);
        }

        return method;
    }

    public override sbyte Priority => 21;

    public override string ToString() =>
        $"Create transformer using {nameof(TextFactoryAttribute)}.{nameof(TextFactoryAttribute.FactoryType)}.{FactoryMethodName}(ReadOnlySpan<char> or string)";
}

// ReSharper disable RedundantAttributeUsageProperty
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, Inherited = true, AllowMultiple = false)]
// ReSharper restore RedundantAttributeUsageProperty
public sealed class TextFactoryAttribute : Attribute
{
    public Type FactoryType { get; }

    public TextFactoryAttribute([NotNull] Type factoryType) => FactoryType = factoryType ?? throw new ArgumentNullException(nameof(factoryType));
}
