using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SourcetrailCSharpIndexer
{
    /// <summary>
    /// Contains static methods which aid in building names for types and type-members
    /// </summary>
    static class NameHelper
    {
        public static string GetFullSymbolName(ISymbol symbol, out string prefix, out string postfix)
        {
            if (!SymbolEqualityComparer.Default.Equals(symbol.ContainingAssembly, Globals.Compilation.Assembly) 
                && Globals.Settings.OmitExternals)
            {
                prefix = postfix = string.Empty;
                return string.Empty;
            }

            if (symbol is IArrayTypeSymbol array)
            {
                postfix = "[]";
                symbol = array.ElementType;
            }

            var symbolName = symbol.ToString();

            // we apply prefixes only to type-members, not types
            prefix = symbol.DeclaredAccessibility != Accessibility.NotApplicable
                && symbol.Kind != SymbolKind.NamedType && symbol.Kind != SymbolKind.TypeParameter
                ? symbol.DeclaredAccessibility.ToString().ToLowerInvariant()
                : string.Empty;
            postfix = string.Empty;

            var possibleArraySpecifier = string.Empty;
            if (symbol is ITypeSymbol typeSymbol)
            {
                // avoid having a type "T" (or something similar) in generic classes (not very useful)
                if (typeSymbol.TypeKind == TypeKind.TypeParameter)
                    return string.Empty;
                if (symbol is INamedTypeSymbol nt && nt.IsGenericType)
                {
                    symbol = nt.OriginalDefinition;
                    symbolName = symbol.ToString();
                }
            }
            else if (symbol is IMethodSymbol ms)
            {
                if (ms.ContainingType.IsGenericType && ms.OriginalDefinition != null)
                {
                    symbol = ms = ms.OriginalDefinition;
                    symbolName = symbol.ToString();
                }
                if (ms.MethodKind == MethodKind.Ordinary)
                    prefix += ' ' + GetFullOrShortName(ms.ReturnType, out _, out possibleArraySpecifier);
                if (!string.IsNullOrWhiteSpace(possibleArraySpecifier))
                    prefix += possibleArraySpecifier;
                symbolName = symbolName.Substring(0, symbolName.IndexOf('('));
                postfix = '(' + string.Join(", ", ms.Parameters.Select(p =>
                {
                    var typeName = GetFullSymbolName(p.Type, out _, out var post);
                    // if the parameter type is from an external assembly, use the short name (instead of an empty one)
                    if (string.IsNullOrEmpty(typeName))
                        typeName = p.Type.Name;
                    return typeName + post + ' ' + p.Name;
                })) + ')';
            }
            else if (symbol is IPropertySymbol ps)
            {
                prefix += ' ' + GetFullOrShortName(ps.Type, out _, out possibleArraySpecifier);
                if (!string.IsNullOrWhiteSpace(possibleArraySpecifier))
                    prefix += possibleArraySpecifier;
            }
            else if (symbol is IFieldSymbol fs)
            {
                if (fs.ContainingType != null && fs.ContainingType.TypeKind != TypeKind.Enum)
                {
                    prefix += ' ' + GetFullOrShortName(fs.Type, out _, out possibleArraySpecifier);
                    if (!string.IsNullOrWhiteSpace(possibleArraySpecifier))
                        prefix += possibleArraySpecifier;
                }
                else if (fs.ContainingType != null && fs.ContainingType.TypeKind == TypeKind.Enum)
                    prefix = string.Empty;
            }
            else if (symbol is IEventSymbol es)
            {
                prefix += ' ' + GetFullOrShortName(es.Type, out _, out _);
            }

            return symbolName;
        }

        private static string GetFullOrShortName(ISymbol symbol, out string prefix, out string postfix)
        {
            var name = GetFullSymbolName(symbol, out prefix, out postfix);
            if (string.IsNullOrEmpty(name))
                name = symbol.Name;
            return name;
        }

        /// <summary>
        /// Converts a type-name into a JSON representation as expected by SourcetrailDB
        /// </summary>
        /// <param name="fullName">Name of type, should be the "pretty" name</param>
        /// <param name="prefix">optional prefix (e.g. return type of a method)</param>
        /// <param name="postfix">optional suffix (e.g. method-parameters)</param>
        /// <returns></returns>
        public static string SerializeName(string fullName, string prefix = "", string postfix = "")
        {
            /*
             * expected format:
             * {
             *   "name_delimiter" : "."
             *   "name_elements" : [
             *     {
             *       "prefix" : "",
             *       "name" : "",
             *       "postfix" : ""
             *     },
             *     ...
             *   ]
             * }
             */
            var parts = new List<string>();
            int genStartIndex, genEndIndex;
            if ((genStartIndex = fullName.IndexOf('<')) > 0
                && (genEndIndex = fullName.LastIndexOf('>')) > genStartIndex)     // generic type ?
            {
                var p1 = fullName.Substring(0, genStartIndex);
                var p2 = fullName.Substring(genStartIndex, genEndIndex - genStartIndex + 1);      // treat generics part as single string
                // use different delimiters for the generics part, sourcetrail would otherwise treat these as nested types
                p1 += p2.Replace('.', ':') + fullName.Substring(genEndIndex + 1);
                parts.AddRange(p1.Split('.'));
            }
            else
                parts.AddRange(fullName.Split('.'));
            var pre = "";
            var post = "";
            var sb = new StringBuilder("{ \"name_delimiter\" : \".\", \"name_elements\" : [ ");
            for (var i = 0; i < parts.Count; i++)
            {
                sb.AppendFormat("{{ \"prefix\" : \"{0}\", \"name\" : \"{1}\", \"postfix\" : \"{2}\" }}", pre, parts[i], post);
                if (i + 1 < parts.Count)
                {
                    sb.AppendLine(",");
                }
                if (i + 2 == parts.Count)
                {
                    // apply pre-/post-fix to last element
                    pre = prefix;
                    post = postfix;
                }
            }
            sb.Append("] }");
            return sb.ToString();
        }
    }
}
