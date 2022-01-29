using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SourcetrailCSharpIndexer.Analyzing
{
    class CodeWalker : CSharpSyntaxWalker
    {
        public WalkerPhase Phase { get; set; }

        // in the first phase, we collect namespaces and types

        public override void VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
        {
            if (Phase == WalkerPhase.CollectSymbols)
                node.Accept(new NamespaceDeclarationVisitor());
            base.VisitNamespaceDeclaration(node);
        }

        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            if (Phase == WalkerPhase.CollectSymbols)
                node.Accept(new ClassDeclarationVisitor());
            base.VisitClassDeclaration(node);
            Globals.CurrentClassId = 0;
        }

        public override void VisitStructDeclaration(StructDeclarationSyntax node)
        {
            if (Phase == WalkerPhase.CollectSymbols)
                node.Accept(new StructDeclarationVisitor());
            base.VisitStructDeclaration(node);
            Globals.CurrentClassId = 0;
        }

        public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
        {
            if (Phase == WalkerPhase.CollectSymbols)
                node.Accept(new InterfaceDeclarationVisitor());
            base.VisitInterfaceDeclaration(node);
        }

        public override void VisitEnumDeclaration(EnumDeclarationSyntax node)
        {
            if (Phase == WalkerPhase.CollectSymbols)
                node.Accept(new EnumDeclarationVisitor());
            base.VisitEnumDeclaration(node);
        }

        public override void VisitDelegateDeclaration(DelegateDeclarationSyntax node)
        {
            if (Phase == WalkerPhase.CollectSymbols)
                node.Accept(new DelegateDeclarationVisitor());
            base.VisitDelegateDeclaration(node);
        }

        // in the second phase, we collect type-members and references to types/type-members from actual code
        // TODO: is this really all, that is needed ?

        public override void VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
        {
            if (Phase == WalkerPhase.CollectReferences)
                node.Accept(new ObjectCreationExpressionVisitor());
            base.VisitObjectCreationExpression(node);
        }

        public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            if (Phase == WalkerPhase.CollectReferences)
                node.Accept(new MemberAccessExpressionVisitor());
            base.VisitMemberAccessExpression(node);
        }

        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            if (Phase == WalkerPhase.CollectReferences)
                node.Accept(new InvocationExpressionVisitor());
            base.VisitInvocationExpression(node);
        }

        public override void VisitIdentifierName(IdentifierNameSyntax node)
        {
            if (Phase == WalkerPhase.CollectReferences)
                node.Accept(new IdentifierNameVisitor());
            base.VisitIdentifierName(node);
        }

        public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            //if (Phase == WalkerPhase.CollectSymbols)
                node.Accept(new ConstructorDeclarationVisitor());
            base.VisitConstructorDeclaration(node);
            Globals.CurrentMethodId = 0;
        }

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            //if (Phase == WalkerPhase.CollectSymbols)
                node.Accept(new MethodDeclarationVisitor());
            base.VisitMethodDeclaration(node);
            Globals.CurrentMethodId = 0;
        }

        public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            //if (Phase == WalkerPhase.CollectSymbols)
                node.Accept(new PropertyDeclarationVisitor());
            base.VisitPropertyDeclaration(node);
            Globals.CurrentMethodId = 0;
        }

        public override void VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            //if (Phase == WalkerPhase.CollectSymbols)
                node.Accept(new FieldDeclarationVisitor());
            base.VisitFieldDeclaration(node);
            Globals.CurrentMethodId = 0;
        }

        public override void VisitEventDeclaration(EventDeclarationSyntax node)
        {
            //if (Phase == WalkerPhase.CollectSymbols)
                node.Accept(new EventDeclarationVisitor());
            base.VisitEventDeclaration(node);
            Globals.CurrentMethodId = 0;
        }
    }
}
