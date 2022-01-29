using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SourcetrailCSharpIndexer.Analyzing
{
    class ObjectCreationExpressionVisitor : SyntaxVisitorBase
    {
        public override void VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
        {
            if (Globals.CurrentMethodId == 0)
                return;

            var model = Globals.Compilation.GetSemanticModel(node.SyntaxTree);
            var symbol = model.GetSymbolInfo(node.Type);
            var constructor = model.GetSymbolInfo(node);
            if (symbol.Symbol == null || symbol.Symbol is not INamedTypeSymbol)
                return;

            if (constructor.Symbol != null)
            {
                var ctorName = NameHelper.GetFullSymbolName(constructor.Symbol, out var prefix, out var postfix);
                if (string.IsNullOrWhiteSpace(ctorName))
                    return;
                var ctorId = Globals.DataCollector.CollectSymbol(ctorName, CoatiSoftware.SourcetrailDB.SymbolKind.SYMBOL_METHOD,
                    prefix, postfix);
                var refId = Globals.DataCollector.CollectReference(Globals.CurrentMethodId, ctorId,
                    CoatiSoftware.SourcetrailDB.ReferenceKind.REFERENCE_CALL);
                Globals.DataCollector.CollectReferenceLocation(refId, node.GetLocation());
            }
            else if (constructor.CandidateSymbols.Length > 0)
            {
                foreach (var ctor in constructor.CandidateSymbols)
                {
                    var ctorName = NameHelper.GetFullSymbolName(ctor, out var prefix, out var postfix);
                    if (string.IsNullOrWhiteSpace(ctorName))
                        continue;
                    var ctorId = Globals.DataCollector.CollectSymbol(ctorName, CoatiSoftware.SourcetrailDB.SymbolKind.SYMBOL_METHOD,
                        prefix, postfix);
                    var refId = Globals.DataCollector.CollectReference(Globals.CurrentMethodId, ctorId,
                        CoatiSoftware.SourcetrailDB.ReferenceKind.REFERENCE_CALL);
                    Globals.DataCollector.CollectReferenceLocation(refId, node.GetLocation());
                    Globals.DataCollector.MarkReferenceAsAmbiguous(refId);
                }
            }
        }
    }
}
