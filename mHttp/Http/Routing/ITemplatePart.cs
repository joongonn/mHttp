﻿namespace m.Http.Routing
{
    interface ITemplatePart { }

    class Literal : ITemplatePart
    {
        public readonly string Value;

        public Literal(string value) { Value = value; }

        public override string ToString() { return string.Format("Literal({0})", Value); }
    }

    class Variable : ITemplatePart
    {
        public readonly string Name;

        public Variable(string name) { Name = name; }

        public override string ToString() { return string.Format("Variable({0})", Name); }
    }

    class Wildcard : ITemplatePart
    {
        public static readonly Wildcard Instance = new Wildcard();

        Wildcard() { }

        public override string ToString() { return string.Format("Wildcard"); }
    }
}
