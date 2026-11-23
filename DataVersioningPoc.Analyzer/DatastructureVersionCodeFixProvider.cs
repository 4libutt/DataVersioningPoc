using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using System.Composition;
using Microsoft.CodeAnalysis.CSharp;

namespace DataVersioningPoc.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DataStructureVersionCodeFixProvider))]
public sealed class DataStructureVersionCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ["DSV001"];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        Diagnostic diagnostic = context.Diagnostics.First();
        SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
        if (root is null)
        {
            return;
        }

        SyntaxNode node = root.FindNode(diagnostic.Location.SourceSpan);
        ClassDeclarationSyntax? classDeclaration = node.FirstAncestorOrSelf<ClassDeclarationSyntax>();
        if (classDeclaration is null)
        {
            return;
        }

        if (!diagnostic.Properties.TryGetValue("BaselineVersion", out var baselineVersionText) || !int.TryParse(baselineVersionText, out var baselineVersion))
        {
            return;
        }

        context.RegisterCodeFix(CodeAction.Create(title:$"Increase DataStructureVersion to {baselineVersion + 1}",

                createChangedDocument: (Func<CancellationToken, Task<Document>>)(
                        cancellationToken => IncreaseVersionAsync(context.Document, classDeclaration, baselineVersion, cancellationToken)),

                equivalenceKey: "IncreaseDataStructureVersion"), diagnostic);
    }

    private static async Task<Document> IncreaseVersionAsync(Document document, ClassDeclarationSyntax classDeclaration, int baselineVersion, CancellationToken cancellationToken)
    {
        AttributeSyntax? attribute = FindDataStructureVersionAttribute(classDeclaration);
        if (attribute is null)
        {
            return document;
        }

        if (attribute.ArgumentList is null || attribute.ArgumentList.Arguments.Count == 0)
        {
            return document;
        }

        int newVersion = baselineVersion + 1;

        AttributeArgumentSyntax newArgument = SyntaxFactory.AttributeArgument(SyntaxFactory.
            LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(newVersion)));

        AttributeArgumentListSyntax newArgumentList = SyntaxFactory.AttributeArgumentList(SyntaxFactory.SingletonSeparatedList(newArgument));

        AttributeSyntax newAttribute = attribute.WithArgumentList(newArgumentList);

        SyntaxNode? root = await document.GetSyntaxRootAsync(cancellationToken);

        if (root is null)
        {
            return document;
        }

        SyntaxNode newRoot = root.ReplaceNode(attribute, newAttribute);

        return document.WithSyntaxRoot(newRoot);
    }

    private static AttributeSyntax? FindDataStructureVersionAttribute(ClassDeclarationSyntax classDeclaration)
    {
        return classDeclaration.AttributeLists.SelectMany(list => list.Attributes).FirstOrDefault(attribute => attribute.Name.ToString().Contains("DataStructureVersion"));
    }
}
