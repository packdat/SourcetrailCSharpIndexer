using CoatiSoftware.SourcetrailDB;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SourcetrailCSharpIndexer.Analyzing
{
    class NamespaceDeclarationVisitor : CSharpSyntaxVisitor
    {
        public override void VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
        {
            var name = node.Name.ToString();
            var location = node.GetLocation();
            Globals.DataCollector.CollectFile(location.SourceTree.FilePath, Globals.FileLanguage);
            Globals.DataCollector.CollectSymbol(name, SymbolKind.SYMBOL_NAMESPACE);
        }
    }
}
