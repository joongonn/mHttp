using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace m.Http.Routing
{
    public sealed class Route : IComparable<Route>
    {
        static readonly IReadOnlyDictionary<string, string> EmptyPathVariables = new Dictionary<string, string>();
        static readonly Regex TemplateRegex = new Regex(@"^(/\{[a-zA-Z_]+\}|/[a-zA-Z0-9]+)*(/\*|/)?$");

        public readonly string PathTemplate;
        readonly ITemplatePart[] templateParts;

        readonly string toString;

        internal Route(string pathTemplate)
        {
            templateParts = BuildTemplate(pathTemplate);
            PathTemplate = pathTemplate;

            toString = string.Format("Route({0})", PathTemplate);
        }

        static ITemplatePart[] BuildTemplate(string pathTemplate)
        {
            if (String.IsNullOrEmpty(pathTemplate))
            {   
                throw new ArgumentException("Must not be null or empty", nameof(pathTemplate));
            }

            if (pathTemplate[0] != '/')
            {
                throw new ArgumentException("Must begin with '/'", nameof(pathTemplate));
            }

            var match = TemplateRegex.Match(pathTemplate);
            if (match.Success)
            {
                var templateParts = new List<ITemplatePart>();
                templateParts.Add(new Literal("/"));

                var stringParts = pathTemplate.Split('/');

                for (int i=1; i<stringParts.Length; i++)
                {
                    if (stringParts[i] == "*")
                    {
                        templateParts.Add(Wildcard.Instance);
                    }
                    else if (stringParts[i].StartsWith("{"))
                    {
                        templateParts.Add(new Variable(stringParts[i].Substring(1, stringParts[i].Length - 2)));
                    }
                    else if (stringParts[i] == string.Empty) // due to trailing slash
                    {
                        continue;
                    }
                    else // literal
                    {
                        if (i == stringParts.Length - 1)
                        {
                            templateParts.Add(new Literal(stringParts[i]));
                        }
                        else
                        {
                            templateParts.Add(new Literal(stringParts[i] + "/"));
                        }
                    }
                }

                return templateParts.ToArray();
            }
            else
            {
                throw new ArgumentException(string.Format("Must match regex pattern {0}", TemplateRegex), "pathTemplate");
            }
        }

        public bool TryMatch(Uri url, out IReadOnlyDictionary<string, string> pathVariables)
        {
            var segments = url.Segments;
            var variablesToCapture = 0;
            var isMatched = true;
            var i = 0;

            while (true)
            {
                var currentSegment = segments[i];
                var currentTemplatePart = templateParts[i];

                if (currentTemplatePart == Wildcard.Instance)
                {
                    break;
                }
                else if (currentTemplatePart is Variable)
                {
                    variablesToCapture++;
                    i++;
                }
                else
                {
                    var literalPart = currentTemplatePart as Literal;
                    if (string.Equals(literalPart.Value, currentSegment))
                    {
                        i++;
                    }
                    else
                    {
                        isMatched = false;
                        break;
                    }
                }

                if (i == segments.Length || i == templateParts.Length)
                {
                    // if (segments.Length < templateParts.Length)
                    // {
                    //    isMatched = templateParts[i] is Wildcard; // eg if we want "/html" to be captured by "/html/*"
                    //    break;
                    // }
                    // else
                    {
                        isMatched = segments.Length == templateParts.Length;
                        break;
                    }
                }
            }

            if (isMatched)
            {
                if (variablesToCapture > 0)
                {
                    var variables = new Dictionary<string, string>(variablesToCapture);

                    var j = 1;
                    while (j < segments.Length && j < templateParts.Length)
                    {
                        var variablePart = templateParts[j] as Variable;
                        if (variablePart != null)
                        {
                            var segment = segments[j];
                            var segmentLength = segment.Length;

                            variables[variablePart.Name] = (segment[segmentLength - 1] == '/') ? segment.Substring(0, segmentLength - 1) : segment;
                        }

                        j++;
                    }

                    pathVariables = variables;
                }
                else
                {
                    pathVariables = EmptyPathVariables;
                }
            }
            else
            {
                pathVariables = null;
            }

            return isMatched;
        }

        public int CompareTo(Route that)
        {
            if (templateParts.Length > that.templateParts.Length)
            {
                return -1;
            }
            else if (templateParts.Length < that.templateParts.Length)
            {
                return 1;
            }
            else
            {
                for (int i=0; i<templateParts.Length; i++)
                {
                    var thisPart = templateParts[i];
                    var thatPart = that.templateParts[i];

                    var v = thisPart.CompareWeight.CompareTo(thatPart.CompareWeight);
                    if (v != 0)
                    {
                        return v;
                    }
                }
            }

            return 0;
        }

        public override string ToString()
        {
            return toString;
        }
    }
}
