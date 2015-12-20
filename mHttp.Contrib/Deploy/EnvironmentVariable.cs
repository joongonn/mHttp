namespace m.Deploy
{
    public sealed class EnvironmentVariable
    {
        public string ConfigLabel { get; private set; }
        public string VariableName { get; private set; }
        public string VariableType { get; private set; }

        public EnvironmentVariable() { }

        public EnvironmentVariable(string label, string name, string type)
        {
            ConfigLabel = label;
            VariableName = name;
            VariableType = type;
        }
    }
}
