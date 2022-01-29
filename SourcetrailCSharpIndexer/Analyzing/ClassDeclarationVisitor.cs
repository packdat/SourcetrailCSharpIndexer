using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SourcetrailCSharpIndexer.Analyzing
{
    class ClassDeclarationVisitor : SyntaxVisitorBase
    {
        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            var model = Globals.Compilation.GetSemanticModel(node.SyntaxTree);
            var mSymbol = model.GetDeclaredSymbol(node);
            var className = NameHelper.GetFullSymbolName(mSymbol, out _, out _);

            Globals.CurrentClassId =
                Globals.DataCollector.CollectSymbol(className, CoatiSoftware.SourcetrailDB.SymbolKind.SYMBOL_CLASS);
            Globals.DataCollector.CollectSymbolLocation(Globals.CurrentClassId, node.Identifier.GetLocation());
            // cache interfaces implemented by this type
            SymbolCache.CollectSymbol(mSymbol);

            CollectReferences(node.BaseList, model, Globals.CurrentClassId);
            CollectReferences(node.TypeParameterList, model, Globals.CurrentClassId);
            CollectReferences(node.AttributeLists, model, Globals.CurrentClassId);
        }
    }
}
