using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace SourcetrailCSharpIndexer.Analyzing
{
    static class SyntaxNodeExtensions
    {
        public static bool IsChildOf(this SyntaxNode node, SyntaxKind kind)
        {
            var parent = node.Parent;
            while (parent != null && parent.Kind() != kind)
                parent = parent.Parent;
            return parent != null;
        }

        public static SyntaxNode FindParent(this SyntaxNode node, SyntaxKind kind)
        {
            var parent = node.Parent;
            while (parent != null && parent.Kind() != kind)
                parent = parent.Parent;
            return parent;
        }
    }
}
