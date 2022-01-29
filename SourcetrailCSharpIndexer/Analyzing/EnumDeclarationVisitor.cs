using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SourcetrailCSharpIndexer.Analyzing
{
    class EnumDeclarationVisitor : SyntaxVisitorBase
    {
        public override void VisitEnumDeclaration(EnumDeclarationSyntax node)
        {
            var model = Globals.Compilation.GetSemanticModel(node.SyntaxTree);
            var mSymbol = model.GetDeclaredSymbol(node);
            var enumName = NameHelper.GetFullSymbolName(mSymbol, out _, out _);

            var id = Globals.DataCollector.CollectSymbol(enumName, CoatiSoftware.SourcetrailDB.SymbolKind.SYMBOL_ENUM);
            Globals.DataCollector.CollectSymbolLocation(id, node.Identifier.GetLocation());

            foreach (var member in node.Members)
            {
                var entrySymb = model.GetDeclaredSymbol(member);
                var entryName = NameHelper.GetFullSymbolName(entrySymb, out _, out _);
                id = Globals.DataCollector.CollectSymbol(entryName, CoatiSoftware.SourcetrailDB.SymbolKind.SYMBOL_ENUM_CONSTANT);
                Globals.DataCollector.CollectSymbolLocation(id, member.Identifier.GetLocation());
            }

            CollectReferences(node.BaseList, model, id);
            CollectReferences(node.AttributeLists, model, id);
        }
    }
}
