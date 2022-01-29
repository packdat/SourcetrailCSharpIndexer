using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SourcetrailCSharpIndexer.Analyzing
{
    class FieldDeclarationVisitor : SyntaxVisitorBase
    {
        public override void VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            var model = Globals.Compilation.GetSemanticModel(node.SyntaxTree);

            int typeId = 0;
            var typeSymbol = model.GetTypeInfo(node.Declaration.Type);
            var baseType = GetBaseType(typeSymbol.Type);
            if (baseType != null && baseType.Kind != SymbolKind.ErrorType)
            {
                var targetKind = GetTypeKind(baseType);
                var fieldTypeName = NameHelper.GetFullSymbolName(typeSymbol.Type, out _, out _);
                if (!string.IsNullOrWhiteSpace(fieldTypeName))
                {
                    typeId = Globals.DataCollector.CollectSymbol(fieldTypeName, targetKind);
                    Globals.DataCollector.CollectQualifierLocation(typeId, node.Declaration.Type.GetLocation());
                }
            }
            foreach (var variable in node.Declaration.Variables)
            {
                var varSymbol = model.GetDeclaredSymbol(variable);
                var varName = NameHelper.GetFullSymbolName(varSymbol, out var prefix, out var postfix);
                var varId = Globals.DataCollector.CollectSymbol(varName,
                    CoatiSoftware.SourcetrailDB.SymbolKind.SYMBOL_FIELD, prefix, postfix);
                Globals.DataCollector.CollectSymbolLocation(varId, variable.Identifier.GetLocation());

                Globals.CurrentMethodId = varId;

                if (typeId > 0)
                {
                    var refId = Globals.DataCollector.CollectReference(varId, typeId,
                        CoatiSoftware.SourcetrailDB.ReferenceKind.REFERENCE_TYPE_USAGE);
                    Globals.DataCollector.CollectReferenceLocation(refId, node.Declaration.Type.GetLocation());
                }
            }

            if (typeId > 0)
                CollectReferences(node.AttributeLists, model, typeId);
        }
    }
}
