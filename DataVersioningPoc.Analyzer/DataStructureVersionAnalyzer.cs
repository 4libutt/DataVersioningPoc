using DataVersioningPoc.Analyzer.Baseline;
using DataVersioningPoc.Analyzer.Comparison;
using DataVersioningPoc.Shared;
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
    private static readonly SymbolDisplayFormat SnapshotTypeFormat =
    SymbolDisplayFormat.FullyQualifiedFormat
        .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted)
        .WithMiscellaneousOptions(
            (SymbolDisplayFormat.FullyQualifiedFormat.MiscellaneousOptions &
             ~SymbolDisplayMiscellaneousOptions.UseSpecialTypes) |
            SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);
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

    private static readonly DiagnosticDescriptor VersionDecreased = new(
    id: "DSV003",
    title: "DataStructureVersion cannot be decreased",
    messageFormat: "DataStructureVersion for persisted model '{0}' was decreased from {1} to {2}",
    category: "DataVersioning",
    defaultSeverity: DiagnosticSeverity.Error,
    isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor NewPersistedModel = new(
    id: "DSV004",
    title: "New persisted model detected",
    messageFormat:
        "New persisted model '{0}' detected with DataStructureVersion {1}. Its snapshot will be added to the baseline.",
    category: "DataVersioning",
    defaultSeverity: DiagnosticSeverity.Warning,
    isEnabledByDefault: true);
    private static readonly DiagnosticDescriptor PersistedModelRemoved = new(
        id: "DSV005",
        title: "Persisted model was removed",
        messageFormat: "Persisted model '{0}' was removed",
        category: "DataVersioning",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: new[]
        {
            WellKnownDiagnosticTags.CompilationEnd
        });
    private static readonly DiagnosticDescriptor InvalidVersion = new(
    id: "DSV006",
    title: "DataStructureVersion must be positive",
    messageFormat: "DataStructureVersion for persisted model '{0}' must be greater than 0, but is {1}",
    category: "DataVersioning",
    defaultSeverity: DiagnosticSeverity.Error,
    isEnabledByDefault: true);

    // voor Roslyn zodat het weet wel diagnostics er mogelijk zijn
    public override ImmutableArray<DiagnosticDescriptor>
    SupportedDiagnostics =>
    [
        Rule,
        MigrationWarning,
        VersionDecreased,
        NewPersistedModel,
        PersistedModelRemoved,
        InvalidVersion
    ];

    // Dit is waar de codeanalysis daadwerkelijk wordt uitgevoerd
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(
        compilationContext =>
        {
            BaselineReader baselineReader = new();

            Dictionary<string, ModelSnapshot> baseline = baselineReader.Read(compilationContext.Options.AdditionalFiles);

            compilationContext.RegisterSymbolAction(
                symbolContext =>
                {
                    AnalyzeNamedType(symbolContext, baseline);
                },
                SymbolKind.NamedType);

            compilationContext.RegisterCompilationEndAction(
                compilationAnalysisContext =>
                {
                    AnalyzeRemovedModels(compilationAnalysisContext, baseline);
                });
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
        if (version <= 0)
        {
            Diagnostic diagnostic = Diagnostic.Create(InvalidVersion, type.Locations[0], type.ToDisplayString(), version);
            context.ReportDiagnostic(diagnostic);
            return;
        }

        //Huidige model uitlezen
        PropertySnapshot[] properties = type.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(property => !property.IsImplicitlyDeclared)
            .Where(property => property.DeclaredAccessibility == Accessibility.Public)
            .Where(property => !property.IsStatic)
            .Where(property => !property.IsIndexer)
            .Where(property => property.GetMethod is not null)
            .Where(property => property.GetMethod is
            {
                DeclaredAccessibility: Accessibility.Public
            })
            .Select(property => new PropertySnapshot(property.Name, property.Type.ToDisplayString(SnapshotTypeFormat)))
            .OrderBy(property => property.Name, StringComparer.Ordinal)
            .ToArray();

        // Huidige model omzetten naar een snapshot
        ModelSnapshot snapshot = new ModelSnapshot(type.ToDisplayString(), version, properties);

        if (!baseline.TryGetValue(snapshot.FullyQualifiedName, out ModelSnapshot? baselineSnapshot))
        {
            Diagnostic diagnostic = Diagnostic.Create(NewPersistedModel, type.Locations[0], snapshot.FullyQualifiedName, snapshot.Version);
            context.ReportDiagnostic(diagnostic);
            return;
        }

        ModelComparer comparer = new ModelComparer();

        //Baseline en current vergelijken
        IReadOnlyList<ModelChange> changes = comparer.Compare(snapshot, baselineSnapshot);

        // controleren of er veranderingen zijn en de regels toepassen
        if (snapshot.Version < baselineSnapshot.Version)
        {
            Diagnostic diagnostic = Diagnostic.Create(
                VersionDecreased,
                type.Locations[0],
                snapshot.FullyQualifiedName,
                baselineSnapshot.Version,
                snapshot.Version);

            context.ReportDiagnostic(diagnostic);
        }
        else if (changes.Any())
        {
            if (snapshot.Version == baselineSnapshot.Version)
            {
                foreach (ModelChange change in changes)
                {
                    ImmutableDictionary<string, string?> diagnosticProperties =
                        ImmutableDictionary<string, string?>.Empty.Add(
                            "BaselineVersion",
                            baselineSnapshot.Version.ToString());

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
            else
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
                $"property '{change.PropertyName}' changed type " + $"from '{change.OldType}' to '{change.NewType}'",

            _ => "unknown structural change"
        };
    }

    private static void AnalyzeRemovedModels(CompilationAnalysisContext context, Dictionary<string, ModelSnapshot> baseline)
    {
        HashSet<string> currentPersistedModels = new(StringComparer.Ordinal);

        CollectPersistedModels(context.Compilation.Assembly.GlobalNamespace, currentPersistedModels);

        foreach (KeyValuePair<string, ModelSnapshot> baselineEntry in baseline)
        {
            if (currentPersistedModels.Contains(baselineEntry.Key))
            {
                continue;
            }

            Diagnostic diagnostic = Diagnostic.Create(PersistedModelRemoved, Location.None, baselineEntry.Key);
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static void CollectPersistedModels(INamespaceSymbol namespaceSymbol, HashSet<string> persistedModels)
    {
        foreach (ISymbol member in namespaceSymbol.GetMembers())
        {
            if (member is INamespaceSymbol nestedNamespace)
            {
                CollectPersistedModels(nestedNamespace, persistedModels);
                continue;
            }

            if (member is INamedTypeSymbol type)
            {
                CollectPersistedModels(type, persistedModels);
            }
        }
    }

    private static void CollectPersistedModels(INamedTypeSymbol type, HashSet<string> persistedModels)
    {
        bool hasDataStructureVersionAttribute = type.GetAttributes()
            .Any(attribute => attribute.AttributeClass?.ToDisplayString() == "DataVersioningPoc.Models.DataStructureVersionAttribute");

        if (hasDataStructureVersionAttribute)
        {
            persistedModels.Add(type.ToDisplayString());
        }

        foreach (INamedTypeSymbol nestedType in type.GetTypeMembers())
        {
            CollectPersistedModels(nestedType, persistedModels);
        }
    }
}