using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SourcetrailCSharpIndexer.Analyzing
{
    class InterfaceDeclarationVisitor : SyntaxVisitorBase
    {
        public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
        {
            var model = Globals.Compilation.GetSemanticModel(node.SyntaxTree);
            var mSymbol = model.GetDeclaredSymbol(node);
            var interfaceName = NameHelper.GetFullSymbolName(mSymbol, out _, out _);

            var iid = Globals.DataCollector.CollectSymbol(interfaceName, CoatiSoftware.SourcetrailDB.SymbolKind.SYMBOL_INTERFACE);
            Globals.DataCollector.CollectSymbolLocation(iid, node.Identifier.GetLocation());

            CollectReferences(node.BaseList, model, iid);
            CollectReferences(node.TypeParameterList, model, iid);
            CollectReferences(node.AttributeLists, model, iid);
        }
    }
}
