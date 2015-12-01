using System;

namespace m.Config
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple=false)]
    public sealed class EnvironmentVariableAttribute : Attribute
    {
        public readonly string Name;

        public EnvironmentVariableAttribute(string name)
        {
            Name = name;
        }

        public string GetEnvironmentValue()
        {
            return Environment.GetEnvironmentVariable(Name);
        }
    }
}
