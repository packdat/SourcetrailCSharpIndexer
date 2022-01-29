using Microsoft.CodeAnalysis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace SourcetrailCSharpIndexer.Analyzing
{
    class SymbolCache
    {
        private static readonly ConcurrentDictionary<INamedTypeSymbol, List<INamedTypeSymbol>> typeToInterfaces = new(SymbolEqualityComparer.Default);

        private static readonly ConcurrentDictionary<INamedTypeSymbol, HashSet<INamedTypeSymbol>> interfaceToTypes = new(SymbolEqualityComparer.Default);
        
        public static void CollectSymbol(INamedTypeSymbol symbol)
        {
            if (typeToInterfaces.ContainsKey(symbol) || symbol.AllInterfaces.Length == 0)
                return;
            if (symbol.TypeKind == TypeKind.Class)
            {
                typeToInterfaces[symbol] = symbol.AllInterfaces.ToList();

                foreach (var iface in symbol.AllInterfaces)
                {
                    if (!interfaceToTypes.ContainsKey(iface))
                        interfaceToTypes.TryAdd(iface, new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default));
                    interfaceToTypes[iface].Add(symbol);
                }
            }
        }

        public static IEnumerable<INamedTypeSymbol> GetInterfaceImplementors(INamedTypeSymbol interfaceSymbol)
        {
            return interfaceToTypes.TryGetValue(interfaceSymbol, out var typeSymbols)
                ? typeSymbols
                : Array.Empty<INamedTypeSymbol>();
        }
    }
}
