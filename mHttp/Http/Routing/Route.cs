using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace m.Http.Routing
{
    public sealed class Route
    {
        static readonly IReadOnlyDictionary<string, string> Empty = new Dictionary<string, string>();
        static readonly Regex TemplateRegex = new Regex(@"^(/\{[a-zA-Z_]+\}|/[a-zA-Z0-9]+)*(/\*|/)?$");

        public readonly string PathTemplate;
        readonly ITemplatePart[] templateParts;

        public Route(string pathTemplate)
        {
            templateParts = BuildTemplate(pathTemplate);
            PathTemplate = pathTemplate;
        }

        static ITemplatePart[] BuildTemplate(string pathTemplate)
        {
            if (String.IsNullOrEmpty(pathTemplate))
            {   
                throw new ArgumentException("Must not be null or empty", "pathTemplate");
            }

            if (pathTemplate[0] != '/')
            {
                throw new ArgumentException("Must begin with '/'", "pathTemplate");
            }

            var match = TemplateRegex.Match(pathTemplate);
            if (match.Success)
            {
                var parts = new List<ITemplatePart>();

                foreach (Capture capture in match.Groups[1].Captures)
                {
                    var value = capture.Value;
                    if (capture.Value[1] == '{')
                    {
                        parts.Add(new Variable(value.Substring(2, value.Length - 3)));
                    }
                    else
                    {
                        parts.Add(new Literal(value.Substring(1)));
                    }
                }

                if (match.Groups[2].Captures.Count > 0 && match.Groups[2].Captures[0].Value == "/*")
                {
                    parts.Add(Wildcard.Instance);
                }

                return parts.ToArray();
            }
            else
            {
                throw new ArgumentException(string.Format("Must match regex pattern {0}", TemplateRegex), "pathTemplate");
            }
        }

        public bool TryMatch(Uri url, out IReadOnlyDictionary<string, string> urlVariables)
        {
            var urlSegments = url.Segments;

            if (urlSegments.Length == 1) // request for root "/"
            {
                if (templateParts.Length == 0)
                {
                    urlVariables = Empty;
                    return true;
                }
                else
                {
                    urlVariables = null;
                    return false;
                }
            }
            else
            {
                if (templateParts.Length == 0)
                {
                    urlVariables = null;
                    return false;
                }
            }

            int i = 0; // templateParts
            int j = 1; // System.Uri.Segments (always starts with "/")
            int variablesToCapture = 0;

            bool isMatched = true;
            while (i < templateParts.Length && j < urlSegments.Length)
            {
                var currentTemplatePart = templateParts[i];
                var currentUrlSegment = urlSegments[j];

                if (currentTemplatePart == Wildcard.Instance) // "/*"
                {
                    break;
                }

                var literal = currentTemplatePart as Literal;
                if (literal != null)
                {
                    var urlSegmentLength = currentUrlSegment.Length;
                    if (currentUrlSegment[urlSegmentLength - 1] == '/') { urlSegmentLength--; }

                    if (urlSegmentLength != literal.Value.Length)
                    {
                        isMatched = false;
                        break;
                    }

                    for (int k = 0; k < urlSegmentLength; k++)
                    {
                        if (currentUrlSegment[k] != literal.Value[k])
                        {
                            isMatched = false;
                            break;
                        }
                    }
                }
                else // MUST be true (currentTemplatePart is Variable)
                {
                    variablesToCapture++;
                }

                i++;
                j++;

                if (i < templateParts.Length && j == urlSegments.Length) // out of "segments" to match
                {
                    isMatched = false;
                    break;
                }

                if (i == templateParts.Length && j < urlSegments.Length) // out of "templateParts" to match
                {
                    isMatched = false;
                    break;
                }
            }

            if (isMatched)
            {
                if (variablesToCapture > 0)
                {
                    i = 0;
                    j = 1;

                    var variables = new Dictionary<string, string>(variablesToCapture);

                    while (i < templateParts.Length && j < urlSegments.Length)
                    {
                        var currentTemplatePart = templateParts[i];
                        var currentSegment = urlSegments[j];

                        var variable = currentTemplatePart as Variable;
                        if (variable != null)
                        {
                            var segmentLength = currentSegment.Length;
                            var captureSegment = (currentSegment[segmentLength - 1] == '/') ? currentSegment.Substring(0, segmentLength - 1) : currentSegment;

                            variables.Add(variable.Name, captureSegment);
                        }

                        i++;
                        j++;
                    }

                    urlVariables = variables;
                }
                else
                {
                    urlVariables = Empty;
                }
            }
            else
            {
                urlVariables = null;
            }

            return isMatched;
        }
    }
}
