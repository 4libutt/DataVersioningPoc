using DataVersioningPoc.Analyzer.Baseline;
using DataVersioningPoc.Analyzer.Comparison;
using DataVersioningPoc.Analyzer.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace DataVersioningPoc.Analyzer;

#pragma warning disable RS1038 // Compiler extensions should be implemented in assemblies with compiler-provided references
[DiagnosticAnalyzer(LanguageNames.CSharp)]
#pragma warning restore RS1038 // Compiler extensions should be implemented in assemblies with compiler-provided references
public sealed class DataStructureVersionAnalyzer : DiagnosticAnalyzer
{
    // regelsets
    private static readonly DiagnosticDescriptor Rule = new(
    id: "DSV001",
    title: "DataStructureVersion must be increased",
    messageFormat: "Persisted model '{0}' changed: {1}. " + "DataStructureVersion is {2}, but must be greater than {3}.",
    category: "DataVersioning",
    defaultSeverity: DiagnosticSeverity.Error,
    isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor MigrationWarning = new(
    id: "DSV002",
    title: "Data migration may be required",
    messageFormat:
        "DataStructureVersion for persisted model '{0}' was increased " + "from {1} to {2}. Existing persisted data may require a migration.",
    category: "DataVersioning",
    defaultSeverity: DiagnosticSeverity.Warning,
    isEnabledByDefault: true);

    // voor Roslyn zodat het weet wel diagnostics er mogelijk zijn
    public override ImmutableArray<DiagnosticDescriptor>
    SupportedDiagnostics =>
    [
        Rule,
        MigrationWarning
    ];

    // Dit is waar de codeanalysis daadwerkelijk wordt uitgevoerd
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(
            compilationContext =>
            {
                BaselineReader baselineReader = new BaselineReader();

                Dictionary<string, ModelSnapshot> baseline = baselineReader.Read(compilationContext.Options.AdditionalFiles);

                compilationContext.RegisterSymbolAction(
                    symbolContext =>
                    {
                        AnalyzeNamedType(
                            symbolContext,
                            baseline);
                    },
                    SymbolKind.NamedType);
            });
    }

    private static void AnalyzeNamedType(SymbolAnalysisContext context, Dictionary<string, ModelSnapshot> baseline)
    {
        INamedTypeSymbol type = (INamedTypeSymbol)context.Symbol;

        AttributeData? attribute = type.GetAttributes()
            .FirstOrDefault(attribute => attribute.AttributeClass?.ToDisplayString() == "DataVersioningPoc.Models.DataStructureVersionAttribute");

        if (attribute is null)
        {
            return;
        }

        int version = (int)attribute.ConstructorArguments[0].Value!;

        //Huidige model uitlezen
        PropertySnapshot[] properties = type.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(property => !property.IsImplicitlyDeclared)
            .Select(property => new PropertySnapshot(
                property.Name,
                property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)))
            .OrderBy(property => property.Name)
            .ToArray();

        // Huidige model omzetten naar een snapshot
        ModelSnapshot snapshot = new ModelSnapshot(type.ToDisplayString(), version, properties);

        if (!baseline.TryGetValue(snapshot.FullyQualifiedName, out var baselineSnapshot))
        {
            return;
        }

        ModelComparer comparer = new ModelComparer();

        //Baseline en current vergelijken
        IReadOnlyList<ModelChange> changes = comparer.Compare(snapshot, baselineSnapshot);

        // controleren of er veranderingen zijn en de regels toepassen
        if (changes.Any())
        {
            if (snapshot.Version == baselineSnapshot.Version)
            {
                foreach (ModelChange change in changes)
                {
                    ImmutableDictionary<string, string?> diagnosticProperties = 
                        ImmutableDictionary<string, string?>.Empty.Add("BaselineVersion", baselineSnapshot.Version.ToString());

                    Diagnostic diagnostic = Diagnostic.Create(
                        Rule,
                        type.Locations[0],
                        diagnosticProperties,
                        snapshot.FullyQualifiedName,
                        DescribeChange(change),
                        snapshot.Version,
                        baselineSnapshot.Version);

                    context.ReportDiagnostic(diagnostic);
                }
            }
            else if (snapshot.Version > baselineSnapshot.Version)
            {
                Diagnostic diagnostic = Diagnostic.Create(
                    MigrationWarning,
                    type.Locations[0],
                    snapshot.FullyQualifiedName,
                    baselineSnapshot.Version,
                    snapshot.Version);

                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    // Beschrijving van de aanpassing
    private static string DescribeChange(
    ModelChange change)
    {
        return change.Type switch
        {
            ModelChangeType.Added =>
                $"property '{change.PropertyName}' was added",

            ModelChangeType.Removed =>
                $"property '{change.PropertyName}' was removed",

            ModelChangeType.TypeChanged =>
                $"property '{change.PropertyName}' changed type " +
                $"from '{change.OldType}' to '{change.NewType}'",

            _ => "unknown structural change"
        };
    }
}