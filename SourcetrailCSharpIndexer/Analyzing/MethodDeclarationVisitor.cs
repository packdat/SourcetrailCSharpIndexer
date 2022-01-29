using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SourcetrailCSharpIndexer.Analyzing
{
    class MethodDeclarationVisitor : SyntaxVisitorBase
    {
        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            int tid, rid;
            var model = Globals.Compilation.GetSemanticModel(node.SyntaxTree);
            var methodSymbol = model.GetDeclaredSymbol(node);
            var methodName = NameHelper.GetFullSymbolName(methodSymbol, out string prefix, out string postfix);
            Globals.CurrentMethodId =
                Globals.DataCollector.CollectSymbol(methodName, CoatiSoftware.SourcetrailDB.SymbolKind.SYMBOL_METHOD,
                    prefix, postfix);
            Globals.DataCollector.CollectSymbolLocation(Globals.CurrentMethodId, node.Identifier.GetLocation());

            var returnTypeInfo = model.GetTypeInfo(node.ReturnType);
            var returnType = GetBaseType(returnTypeInfo.Type);
            var returnTypeName = returnType.Kind == SymbolKind.ErrorType
                ? returnTypeInfo.Type.OriginalDefinition.Name
                : NameHelper.GetFullSymbolName(returnType, out _, out _);
            if (!string.IsNullOrWhiteSpace(returnTypeName) &&
                returnType.Kind != SymbolKind.ErrorType /*&& returnType.Kind != SymbolKind.TypeParameter*/)
            {
                var typeKind = GetTypeKind(returnType);
                tid = Globals.DataCollector.CollectSymbol(returnTypeName, typeKind);
                Globals.DataCollector.CollectQualifierLocation(tid, node.ReturnType.GetLocation());
                rid = Globals.DataCollector.CollectReference(Globals.CurrentMethodId, tid,
                    CoatiSoftware.SourcetrailDB.ReferenceKind.REFERENCE_TYPE_USAGE);
                Globals.DataCollector.CollectReferenceLocation(rid, node.ReturnType.GetLocation());
            }

            CollectReferences(node.ParameterList, model, Globals.CurrentMethodId);
            CollectReferences(node.TypeParameterList, model, Globals.CurrentMethodId);
            CollectReferences(node.AttributeLists, model, Globals.CurrentMethodId);
        }
    }
}
