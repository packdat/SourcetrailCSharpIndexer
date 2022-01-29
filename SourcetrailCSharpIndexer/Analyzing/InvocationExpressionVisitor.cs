using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;

namespace SourcetrailCSharpIndexer.Analyzing
{
    class InvocationExpressionVisitor : SyntaxVisitorBase
    {
        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            // ignore, if we're outside of code
            if (Globals.CurrentMethodId == 0)
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
                foreach (var sym in symbolInfo.CandidateSymbols)
                    symQueue.Enqueue(sym);
                isAmbiguous = true;
            }
            while (symQueue.Count > 0)
            {
                symbol = symQueue.Dequeue();
                var method = (IMethodSymbol)symbol;
                if (method.IsExtensionMethod)
                {
                    // for extension methods, use the original definition, not what the compiler makes out of it
                    if (method.MethodKind == MethodKind.ReducedExtension)
                        symbol = method.ReducedFrom;
                }
                var symbolName = NameHelper.GetFullSymbolName(symbol, out var prefix, out var postfix);
                if (string.IsNullOrWhiteSpace(symbolName))
                    continue;
                var targetKind = CoatiSoftware.SourcetrailDB.SymbolKind.SYMBOL_METHOD;
                var refKind = CoatiSoftware.SourcetrailDB.ReferenceKind.REFERENCE_CALL;
                var targetId = Globals.DataCollector.CollectSymbol(symbolName, targetKind, prefix, postfix);

                var refId = Globals.DataCollector.CollectReference(Globals.CurrentMethodId, targetId, refKind);
                Globals.DataCollector.CollectReferenceLocation(refId, node.GetLocation());
                if (isAmbiguous)
                    Globals.DataCollector.MarkReferenceAsAmbiguous(refId);
                // if we're accessing an interface-method, add a reference to the implemented methods as well
                if (symbol is IMethodSymbol ms && ms.ContainingType.TypeKind == TypeKind.Interface)
                {
                    var implementors = SymbolCache.GetInterfaceImplementors(ms.ContainingType);
                    foreach (var impl in implementors)
                    {
                        try
                        {
                            // got an exception here, because "the object is in an invalid state"
                            // TODO: research, what might cause this and how to prevent it
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
                        catch (Exception ex)
                        {
                            Console.WriteLine($"{ex.GetType().Name} trying to access implementation of " +
                                $"{ms.ContainingType.Name}.{ms.Name} on {impl.Name}");
                            Console.WriteLine("Message: " + ex.Message);
                        }
                    }
                }

            }
        }
    }
}
