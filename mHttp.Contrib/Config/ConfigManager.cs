using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using m.Deploy;

namespace m.Config
{
    public static class ConfigManager
    {
        static ConfigMap GetConfigMap<TConf>() where TConf : IConfigurable, new()
        {
            return GetConfigMap(typeof(TConf));
        }

        internal static ConfigMap GetConfigMap(Type confType)
        {
            var deployConfigLabel = (DeployConfigLabelAttribute)confType.GetCustomAttributes(typeof(DeployConfigLabelAttribute), false)
                                                                        .SingleOrDefault();
            var properties = confType.GetProperties();

            var configMapEntries = properties.Select(p => new {
                                                  Property = p,
                                                  EnvironmentVariable = p.GetCustomAttributes(typeof(EnvironmentVariableAttribute), false)
                                                                         .SingleOrDefault() as EnvironmentVariableAttribute
                                              })
                                             .Where(p => p.EnvironmentVariable != null)
                                             .Select(p => new ConfigMap.Entry(p.Property,
                                                                              p.EnvironmentVariable,
                                                                              TypeDescriptor.GetConverter(p.Property.PropertyType)))
                                             .ToList();
            
            return new ConfigMap(confType, deployConfigLabel, configMapEntries);
        }
        
        public static void Apply<TConf>(TConf conf) where TConf : IConfigurable, new()
        {
            var configMapEntries = GetConfigMap<TConf>();
            var entriesToApply = configMapEntries.Select(e => new {
                                                      Property = e.Property,
                                                      Converter = e.Converter,
                                                      EnvironmentValueString = e.EnvironmentVariable.GetEnvironmentValue()
                                                  })
                                                 .Where(e => !string.IsNullOrEmpty(e.EnvironmentValueString));

            foreach (var entry in entriesToApply)
            {
                try
                {
                    object propertyValue = entry.Converter.ConvertFromString(entry.EnvironmentValueString);
                    entry.Property.SetValue(conf, propertyValue);
                }
                catch (Exception e)
                {
                    throw new ArgumentException(string.Format("Error setting property:[{0}] with environment value:[{1}] for conf object of type:[{2}]",
                                                              entry.Property.Name, entry.EnvironmentValueString, typeof(TConf)),
                                                e);
                }
            }
        }

        public static TConf Load<TConf>() where TConf : IConfigurable, new()
        {
            var config = new TConf();
            Apply(config);

            return config;
        }
    }
}
