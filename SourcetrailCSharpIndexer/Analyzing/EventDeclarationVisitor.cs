using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SourcetrailCSharpIndexer.Analyzing
{
    class EventDeclarationVisitor : SyntaxVisitorBase
    {
        public override void VisitEventDeclaration(EventDeclarationSyntax node)
        {
            var model = Globals.Compilation.GetSemanticModel(node.SyntaxTree);

            var eventSymbol = model.GetDeclaredSymbol(node);
            var eventName = NameHelper.GetFullSymbolName(eventSymbol, out var prefix, out var postfix);
            var eventId = Globals.DataCollector.CollectSymbol(eventName,
                CoatiSoftware.SourcetrailDB.SymbolKind.SYMBOL_FIELD, prefix, postfix);
            Globals.DataCollector.CollectSymbolLocation(eventId, node.Identifier.GetLocation());

            Globals.CurrentMethodId = eventId;
            CollectReferences(node.AttributeLists, model, eventId);

            var typeSymbol = model.GetTypeInfo(node.Type);
            if (typeSymbol.Type != null && typeSymbol.Type.Kind != SymbolKind.ErrorType)
            {
                var targetKind = GetTypeKind(typeSymbol.Type);
                var eventTypeName = NameHelper.GetFullSymbolName(typeSymbol.Type, out _, out _);
                if (string.IsNullOrWhiteSpace(eventTypeName))
                    return;
                var typeId = Globals.DataCollector.CollectSymbol(eventTypeName, targetKind);
                var refId = Globals.DataCollector.CollectReference(eventId, typeId,
                    CoatiSoftware.SourcetrailDB.ReferenceKind.REFERENCE_TYPE_USAGE);
                Globals.DataCollector.CollectReferenceLocation(refId, node.Type.GetLocation());
            }

            CollectReferences(node.AttributeLists, model, eventId);            
        }
    }
}
