using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

using m.Deploy;

namespace m.Config
{
    public sealed class ConfigMap : IEnumerable<ConfigMap.Entry>
    {
        public sealed class Entry
        {
            public readonly PropertyInfo Property;
            public readonly EnvironmentVariableAttribute EnvironmentVariable;
            public readonly TypeConverter Converter;

            public Entry(PropertyInfo property, EnvironmentVariableAttribute environmentVariable, TypeConverter converter)
            {
                Property = property;
                EnvironmentVariable = environmentVariable;
                Converter = converter;
            }
        }

        public readonly Type ConfigurableType;
        public readonly DeployConfigLabelAttribute DeployConfigLabel;
        readonly IReadOnlyList<Entry> entries;

        public IEnumerator<Entry> GetEnumerator()
        {
            return entries.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return entries.GetEnumerator();
        }

        public ConfigMap(Type configurableType, DeployConfigLabelAttribute deployConfigLabel, IReadOnlyList<Entry> entries)
        {
            ConfigurableType = configurableType;
            DeployConfigLabel = deployConfigLabel;
            this.entries = entries;
        }
    }
}
