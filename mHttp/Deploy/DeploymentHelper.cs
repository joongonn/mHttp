using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using m.Config;

namespace m.Deploy
{
    public static class DeploymentHelper
    {
        static IEnumerable<EnvironmentVariable> ToEnvironmentVariables(this ConfigMap configMap)
        {
            var configLabel = (configMap.DeployConfigLabel == null) ? configMap.ConfigurableType.Name : configMap.DeployConfigLabel.Label;

            return configMap.Select(entry => new EnvironmentVariable(configLabel,
                                                                     entry.EnvironmentVariable.Name,
                                                                     entry.Property.PropertyType.Name));
        }

        public static Assembly[] FindAssemblies(string path)
        {
            var directory = new DirectoryInfo(path);
            var assemblies = new List<Assembly>();

            foreach (var file in directory.GetFiles())
            {
                try
                {
                    assemblies.Add(Assembly.LoadFile(file.FullName));
                }
                catch (BadImageFormatException)
                {
                    continue;
                }
            }

            return assemblies.ToArray();
        }
        
        public static EnvironmentVariable[] FindEnvironmentVariables(Assembly assembly)
        {
            IEnumerable<Type> configurableTypes = assembly.GetTypes()
                                                          .Where(type => type.GetInterfaces().Contains(typeof(IConfigurable)));

            IEnumerable<ConfigMap> configMaps = configurableTypes.Select(ConfigManager.GetConfigMap);

            IEnumerable<EnvironmentVariable> environmentVariables = configMaps.SelectMany(ToEnvironmentVariables);

            return environmentVariables.ToArray();
        }

        public static EnvironmentVariable[] ExportEnvironmentVariables()
        {
            var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            return FindAssemblies(path).SelectMany(DeploymentHelper.FindEnvironmentVariables).ToArray();
        }
    }
}

