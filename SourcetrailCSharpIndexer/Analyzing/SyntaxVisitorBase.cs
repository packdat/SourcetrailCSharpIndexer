using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SourcetrailCSharpIndexer.Analyzing
{
    abstract class SyntaxVisitorBase : CSharpSyntaxVisitor
    {
        public static void CollectReferences(BaseListSyntax baseList,
                                             SemanticModel model,
                                             int referencingSymbolId)
        {
            if (baseList != null)
            {
                foreach (var baseType in baseList.Types)
                {
                    var ts = model.GetTypeInfo(baseType.Type);
                    if (ts.Type != null && ts.Type.Kind != SymbolKind.ErrorType)
                    {
                        var type = ts.Type;
                        var typeName = NameHelper.GetFullSymbolName(type, out _, out _);
                        if (string.IsNullOrWhiteSpace(typeName))
                            continue;
                        var typeKind = GetTypeKind(type);
                        var tid = Globals.DataCollector.CollectSymbol(typeName, typeKind);
                        var rid = Globals.DataCollector.CollectReference(referencingSymbolId, tid,
                            CoatiSoftware.SourcetrailDB.ReferenceKind.REFERENCE_INHERITANCE);
                        Globals.DataCollector.CollectQualifierLocation(rid, baseType.GetLocation());
                    }
                }
            }
        }

        public static void CollectReferences(SyntaxList<AttributeListSyntax> attributeLists,
                                             SemanticModel model,
                                             int referencingSymbolId)
        {
            if (attributeLists.Any())
            {
                foreach (var list in attributeLists)
                {
                    foreach (var attrib in list.Attributes)
                    {
                        var aSymb = model.GetTypeInfo(attrib);
                        var baseType = GetBaseType(aSymb.Type);
                        if (baseType != null && baseType.Kind != SymbolKind.ErrorType)
                        {
                            var typeName = NameHelper.GetFullSymbolName(baseType, out _, out _);
                            if (string.IsNullOrWhiteSpace(typeName))
                                continue;
                            var typeKind = GetTypeKind(baseType);
                            var tid = Globals.DataCollector.CollectSymbol(typeName, typeKind);
                            Globals.DataCollector.CollectQualifierLocation(tid, attrib.GetLocation());
                            var rid = Globals.DataCollector.CollectReference(referencingSymbolId, tid,
                                CoatiSoftware.SourcetrailDB.ReferenceKind.REFERENCE_ANNOTATION_USAGE);
                            Globals.DataCollector.CollectReferenceLocation(rid, attrib.GetLocation());
                        }
                    }
                }
            }
        }

        public static void CollectReferences(TypeParameterListSyntax parameters,
                                             SemanticModel model,
                                             int referencingSymbolId)
        {
            if (parameters != null)
            {
                foreach (var type in parameters.Parameters)
                {
                    var ts = model.GetTypeInfo(type);
                    var baseType = GetBaseType(ts.Type);
                    if (baseType != null && baseType.Kind != SymbolKind.ErrorType)
                    {
                        var typeName = NameHelper.GetFullSymbolName(baseType, out _, out _);
                        if (string.IsNullOrWhiteSpace(typeName))
                            continue;
                        var typeKind = GetTypeKind(baseType);
                        var tid = Globals.DataCollector.CollectSymbol(typeName, typeKind);
                        Globals.DataCollector.CollectQualifierLocation(tid, type.GetLocation());
                        var rid = Globals.DataCollector.CollectReference(referencingSymbolId, tid,
                            CoatiSoftware.SourcetrailDB.ReferenceKind.REFERENCE_TYPE_ARGUMENT);
                        Globals.DataCollector.CollectReferenceLocation(rid, type.GetLocation());
                    }
                }
            }
        }

        public static void CollectReferences(ParameterListSyntax parameters,
                                             SemanticModel model,
                                             int referencingSymbolId)
        {
            if (parameters != null)
            {
                foreach (var parameter in parameters.Parameters)
                {
                    var ts = model.GetTypeInfo(parameter.Type);
                    var baseType = GetBaseType(ts.Type);
                    if (baseType != null && baseType.Kind != SymbolKind.ErrorType)
                    {
                        var typeName = NameHelper.GetFullSymbolName(baseType, out _, out _);
                        if (string.IsNullOrWhiteSpace(typeName))
                            continue;
                        var typeKind = GetTypeKind(baseType);
                        var tid = Globals.DataCollector.CollectSymbol(typeName, typeKind);
                        Globals.DataCollector.CollectQualifierLocation(tid, parameter.GetLocation());
                        var rid = Globals.DataCollector.CollectReference(referencingSymbolId, tid,
                            CoatiSoftware.SourcetrailDB.ReferenceKind.REFERENCE_TYPE_USAGE);
                        Globals.DataCollector.CollectReferenceLocation(rid, parameter.GetLocation());
                    }
                }
            }
        }

        /// <summary>
        /// For array-type symbols, returns the element type of the array.<br/>
        /// For all other types, returns <paramref name="symbol"/>
        /// </summary>
        /// <param name="symbol"></param>
        /// <returns></returns>
        public static ITypeSymbol GetBaseType(ITypeSymbol symbol)
        {
            if (symbol == null)
                return symbol;
            while (symbol is IArrayTypeSymbol array)
            {
                symbol = array.ElementType;
            }
            return symbol;
        }

        public static CoatiSoftware.SourcetrailDB.SymbolKind GetTypeKind(ITypeSymbol symbol)
        {
            // TODO: attributes may need special handling...
            return symbol.TypeKind switch
            {
                TypeKind.Struct => CoatiSoftware.SourcetrailDB.SymbolKind.SYMBOL_STRUCT,
                TypeKind.Enum => CoatiSoftware.SourcetrailDB.SymbolKind.SYMBOL_ENUM,
                TypeKind.Interface => CoatiSoftware.SourcetrailDB.SymbolKind.SYMBOL_INTERFACE,
                TypeKind.TypeParameter => CoatiSoftware.SourcetrailDB.SymbolKind.SYMBOL_TYPE_PARAMETER,
                _ => CoatiSoftware.SourcetrailDB.SymbolKind.SYMBOL_CLASS
            };
        }
    }
}
