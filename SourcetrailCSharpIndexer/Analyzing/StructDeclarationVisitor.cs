using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SourcetrailCSharpIndexer.Analyzing
{
    class StructDeclarationVisitor : SyntaxVisitorBase
    {
        public override void VisitStructDeclaration(StructDeclarationSyntax node)
        {
            var model = Globals.Compilation.GetSemanticModel(node.SyntaxTree);
            var symbol = model.GetDeclaredSymbol(node);
            var structName = NameHelper.GetFullSymbolName(symbol, out _, out _);

            var sid = Globals.DataCollector.CollectSymbol(structName, CoatiSoftware.SourcetrailDB.SymbolKind.SYMBOL_STRUCT);
            Globals.DataCollector.CollectSymbolLocation(sid, node.Identifier.GetLocation());
            Globals.CurrentClassId = sid;

            CollectReferences(node.BaseList, model, sid);
            CollectReferences(node.TypeParameterList, model, sid);
            CollectReferences(node.AttributeLists, model, sid);
        }
    }
}
