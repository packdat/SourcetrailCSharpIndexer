using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SourcetrailCSharpIndexer.Analyzing
{
    class PropertyDeclarationVisitor : SyntaxVisitorBase
    {
        public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            var model = Globals.Compilation.GetSemanticModel(node.SyntaxTree);

            var propSymbol = model.GetDeclaredSymbol(node);
            var propName = NameHelper.GetFullSymbolName(propSymbol, out var prefix, out var postfix);
            var propId = Globals.DataCollector.CollectSymbol(propName,
                CoatiSoftware.SourcetrailDB.SymbolKind.SYMBOL_FIELD, prefix, postfix);
            Globals.DataCollector.CollectSymbolLocation(propId, node.Identifier.GetLocation());

            Globals.CurrentMethodId = propId;

            var typeInfo = model.GetTypeInfo(node.Type);
            var baseType = GetBaseType(typeInfo.Type);
            if (baseType != null && baseType.Kind != SymbolKind.ErrorType)
            {
                var targetKind = GetTypeKind(baseType);
                var propTypeName = NameHelper.GetFullSymbolName(typeInfo.Type, out _, out _);
                if (string.IsNullOrWhiteSpace(propTypeName))
                    return;
                var typeId = Globals.DataCollector.CollectSymbol(propTypeName, targetKind);
                Globals.DataCollector.CollectQualifierLocation(typeId, node.Type.GetLocation());
                var refId = Globals.DataCollector.CollectReference(propId, typeId,
                    CoatiSoftware.SourcetrailDB.ReferenceKind.REFERENCE_TYPE_USAGE);
                Globals.DataCollector.CollectReferenceLocation(refId, node.Type.GetLocation());
            }

            CollectReferences(node.AttributeLists, model, propId);
        }
    }
}
