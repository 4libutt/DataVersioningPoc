using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using DataVersioningPoc.Shared;
using Microsoft.CodeAnalysis;

namespace DataVersioningPoc.Analyzer.Baseline;

public sealed class BaselineReader
{
    // JSON lezen en omzetten naar onze interne modellen.
    public Dictionary<string, ModelSnapshot> Read(ImmutableArray<AdditionalText> additionalFiles)
    {
        AdditionalText? baselineFile = additionalFiles.FirstOrDefault(file => Path.GetFileName(file.Path).Equals("model-baseline.json", StringComparison.OrdinalIgnoreCase));

        if (baselineFile is null)
        {
            return new Dictionary<string, ModelSnapshot>();
        }

        string? text = baselineFile.GetText()?.ToString();
        if (string.IsNullOrWhiteSpace(text))
        {
            return new Dictionary<string, ModelSnapshot>();
        }

        return ModelSnapshotSerializer.Deserialize(text);
    }

    private sealed class BaselineModel
    {
        public int Version { get; set; }

        public List<BaselineProperty> Properties { get; set; } = [];
    }

    private sealed class BaselineProperty
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }
}