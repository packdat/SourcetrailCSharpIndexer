using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SourcetrailCSharpIndexer.Analyzing
{
    class DelegateDeclarationVisitor : SyntaxVisitorBase
    {
        public override void VisitDelegateDeclaration(DelegateDeclarationSyntax node)
        {
            var model = Globals.Compilation.GetSemanticModel(node.SyntaxTree);
            var symbol = model.GetDeclaredSymbol(node);
            var delegateName = NameHelper.GetFullSymbolName(symbol, out _, out _);

            var id = Globals.DataCollector.CollectSymbol(delegateName, CoatiSoftware.SourcetrailDB.SymbolKind.SYMBOL_FUNCTION);
            Globals.DataCollector.CollectSymbolLocation(id, node.Identifier.GetLocation());
        }
    }
}
