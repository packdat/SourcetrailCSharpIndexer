using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SourcetrailCSharpIndexer.Analyzing
{
    class IdentifierNameVisitor : SyntaxVisitorBase
    {
        public override void VisitIdentifierName(IdentifierNameSyntax node)
        {
            // ignore, if we're outside of code
            if (Globals.CurrentMethodId == 0)
                return;

            var model = Globals.Compilation.GetSemanticModel(node.SyntaxTree);
            var symbolInfo = model.GetSymbolInfo(node);
            var symbol = symbolInfo.Symbol;
            // if symbol is part of an anonymous type, skip it
            if (symbol == null || (symbol.ContainingType != null && symbol.ContainingType.IsAnonymousType))
                return;

            int refId;
            string prefix, postfix;
            switch (symbol.Kind)
            {
                case SymbolKind.Method:
                case SymbolKind.Property:
                case SymbolKind.Field:
                case SymbolKind.Event:
                case SymbolKind.NamedType:
                case SymbolKind.TypeParameter:
                    var targetKind = symbol.Kind switch
                    {
                        SymbolKind.Method => CoatiSoftware.SourcetrailDB.SymbolKind.SYMBOL_METHOD,
                        SymbolKind.Property => CoatiSoftware.SourcetrailDB.SymbolKind.SYMBOL_FIELD,
                        SymbolKind.Field => CoatiSoftware.SourcetrailDB.SymbolKind.SYMBOL_FIELD,
                        SymbolKind.Event => CoatiSoftware.SourcetrailDB.SymbolKind.SYMBOL_FIELD,
                        _ => CoatiSoftware.SourcetrailDB.SymbolKind.SYMBOL_CLASS
                    };
                    if (symbol is ITypeSymbol ts)
                    {
                        if (ts.IsAnonymousType || ts.TypeKind == TypeKind.TypeParameter)
                            break;
                        targetKind = GetTypeKind(ts);
                    }

                    var symbolName = NameHelper.GetFullSymbolName(symbol, out prefix, out postfix);
                    if (string.IsNullOrWhiteSpace(symbolName))
                        break;
                    var targetId = Globals.DataCollector.CollectSymbol(symbolName, targetKind, prefix, postfix);

                    var refKind = CoatiSoftware.SourcetrailDB.ReferenceKind.REFERENCE_USAGE;
                    if (symbol.Kind == SymbolKind.NamedType)
                    {
                        refKind = CoatiSoftware.SourcetrailDB.ReferenceKind.REFERENCE_TYPE_USAGE;
                        Globals.DataCollector.CollectQualifierLocation(targetId, node.GetLocation());
                    }

                    refId = Globals.DataCollector.CollectReference(Globals.CurrentMethodId, targetId, refKind);
                    Globals.DataCollector.CollectReferenceLocation(refId, node.GetLocation());
                    break;
                case SymbolKind.Parameter:
                    var ps = (IParameterSymbol)symbol;
                    var type = GetBaseType(ps.Type);
                    if (type.TypeKind != TypeKind.Error)
                    {
                        var name = NameHelper.GetFullSymbolName(type, out prefix, out postfix);
                        if (string.IsNullOrWhiteSpace(name))
                            break;
                        var tkind = type.TypeKind == TypeKind.Enum
                            ? CoatiSoftware.SourcetrailDB.SymbolKind.SYMBOL_ENUM
                            : CoatiSoftware.SourcetrailDB.SymbolKind.SYMBOL_CLASS;
                        var rkind = CoatiSoftware.SourcetrailDB.ReferenceKind.REFERENCE_TYPE_USAGE;
                        var tid = Globals.DataCollector.CollectSymbol(name, tkind, prefix, postfix);
                        refId = Globals.DataCollector.CollectReference(Globals.CurrentMethodId, tid, rkind);
                        Globals.DataCollector.CollectReferenceLocation(refId, node.GetLocation());
                        Globals.DataCollector.CollectQualifierLocation(tid, node.GetLocation());
                    }
                    break;
            }
        }
    }
}
