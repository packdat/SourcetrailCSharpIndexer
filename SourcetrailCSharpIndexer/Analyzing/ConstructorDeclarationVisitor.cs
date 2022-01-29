using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SourcetrailCSharpIndexer.Analyzing
{
    class ConstructorDeclarationVisitor : SyntaxVisitorBase
    {
        public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            var model = Globals.Compilation.GetSemanticModel(node.SyntaxTree);
            var symbol = model.GetDeclaredSymbol(node);
            var methodName = NameHelper.GetFullSymbolName(symbol, out var prefix, out var postfix);

            Globals.CurrentMethodId =
                Globals.DataCollector.CollectSymbol(methodName, CoatiSoftware.SourcetrailDB.SymbolKind.SYMBOL_METHOD, prefix, postfix);
            Globals.DataCollector.CollectSymbolLocation(Globals.CurrentMethodId, node.Identifier.GetLocation());

            CollectReferences(node.ParameterList, model, Globals.CurrentMethodId);
            CollectReferences(node.AttributeLists, model, Globals.CurrentMethodId);
        }
    }
}
