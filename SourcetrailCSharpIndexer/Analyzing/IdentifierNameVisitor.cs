using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Diagnostics;

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

        public void VisitIdentifierName_naa(IdentifierNameSyntax node)
        {
            // ignore, if we're outside of code
            if (Globals.CurrentMethodId == 0)
                return;
            // invocations and member-access are handled by different visitors
            if (node.FindParent(SyntaxKind.InvocationExpression) is InvocationExpressionSyntax
                ||  node.FindParent(SyntaxKind.SimpleMemberAccessExpression) is MemberAccessExpressionSyntax)
                return;

            var model = Globals.Compilation.GetSemanticModel(node.SyntaxTree);
            var symbolInfo = model.GetSymbolInfo(node);
            var symbol = symbolInfo.Symbol;
            // if symbol is part of an anonymous type, skip it
            if (symbol != null && symbol.ContainingType != null && symbol.ContainingType.IsAnonymousType)
                return;

            var symQueue = new Queue<ISymbol>();
            var isAmbiguous = false;
            if (symbol != null)
                symQueue.Enqueue(symbol);
            else if (symbolInfo.CandidateSymbols.Length > 0)
            {
                // this is most probably a method-overload
                if (node.FindParent(SyntaxKind.InvocationExpression) is InvocationExpressionSyntax)
                {
                    foreach (var sym in symbolInfo.CandidateSymbols)
                        symQueue.Enqueue(sym);
                    isAmbiguous = true;
                }
                else
                    // this may happen, when the wrong framework-assemblies are loaded (at least in my case)
                    // e.g. when indexing a .NET 4.x project and not specifying the framework-path with the -fp parameter 
                    Debug.Assert(false, "Checkme");
            }
            while (symQueue.Count > 0)
            {
                symbol = symQueue.Dequeue();
                if (symbol != null)
                {
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
                            var refKind = CoatiSoftware.SourcetrailDB.ReferenceKind.REFERENCE_USAGE;
                            if (symbol.Kind == SymbolKind.Method && node.IsChildOf(SyntaxKind.InvocationExpression))
                            {
                                refKind = CoatiSoftware.SourcetrailDB.ReferenceKind.REFERENCE_CALL;
                                var method = (IMethodSymbol)symbol;
                                if (method.IsExtensionMethod)
                                {
                                    // for extension methods, use the original definition, not what the compiler makes out of it
                                    if (method.MethodKind == MethodKind.ReducedExtension)
                                        symbol = method.ReducedFrom;
                                }
                            }
                            else if (symbol.Kind == SymbolKind.NamedType)
                                refKind = CoatiSoftware.SourcetrailDB.ReferenceKind.REFERENCE_TYPE_USAGE;
                            var symbolName = NameHelper.GetFullSymbolName(symbol, out prefix, out postfix);
                            if (string.IsNullOrWhiteSpace(symbolName))
                                continue;
                            var targetId = Globals.DataCollector.CollectSymbol(symbolName, targetKind, prefix, postfix);

                            refId = Globals.DataCollector.CollectReference(Globals.CurrentMethodId, targetId, refKind);
                            Globals.DataCollector.CollectReferenceLocation(refId, node.GetLocation());
                            if (isAmbiguous)
                                Globals.DataCollector.MarkReferenceAsAmbiguous(refId);
                            Globals.DataCollector.CollectQualifierLocation(targetId, node.GetLocation());
                            // if we're accessing an interface-method, add a reference to the implemented methods as well
                            if (symbol is IMethodSymbol ms && ms.ContainingType.TypeKind == TypeKind.Interface)
                            {
                                var implementors = SymbolCache.GetInterfaceImplementors(ms.ContainingType);
                                foreach (var impl in implementors)
                                {
                                    var implMethod = impl.FindImplementationForInterfaceMember(symbol);
                                    if (implMethod != null)
                                    {
                                        var methodName = NameHelper.GetFullSymbolName(implMethod, out prefix, out postfix);
                                        if (string.IsNullOrWhiteSpace(methodName))
                                            continue;
                                        var implId = Globals.DataCollector.CollectSymbol(methodName,
                                            CoatiSoftware.SourcetrailDB.SymbolKind.SYMBOL_METHOD, prefix, postfix);
                                        refId = Globals.DataCollector.CollectReference(Globals.CurrentMethodId, implId, refKind);
                                        Globals.DataCollector.CollectReferenceLocation(refId, node.GetLocation());
                                    }
                                }
                            }
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
    }
}
