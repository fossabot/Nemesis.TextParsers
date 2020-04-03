﻿namespace Nemesis.TextParsers.Settings
{
    //TODO
    public sealed class FactoryMethodSettings : ISettings
    {
        public string FactoryMethodName { get; }
        public string EmptyPropertyName { get; }
        public string NullPropertyName { get; }

        public FactoryMethodSettings(string factoryMethodName, string emptyPropertyName, string nullPropertyName)
        {
            FactoryMethodName = factoryMethodName;
            EmptyPropertyName = emptyPropertyName;
            NullPropertyName = nullPropertyName;
        }

        public static FactoryMethodSettings Default { get; } =
            new FactoryMethodSettings("FromText", "Empty", "Null");

        public override string ToString() =>
            $"Parsed by {FactoryMethodName} Empty: {EmptyPropertyName} Null: {NullPropertyName}";
    }
}