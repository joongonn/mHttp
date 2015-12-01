using System;

namespace m.Deploy
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple=false)]
    public class DeployConfigLabelAttribute : Attribute
    {
        public readonly string Label;

        public DeployConfigLabelAttribute(string label)
        {
            this.Label = label;
        }
    }
}
