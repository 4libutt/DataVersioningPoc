using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using DataVersioningPoc.Analyzer.Models;
using Microsoft.CodeAnalysis;

namespace DataVersioningPoc.Analyzer.Baseline;

public sealed class BaselineReader
{
    public Dictionary<string, ModelSnapshot> Read(ImmutableArray<AdditionalText> additionalFiles)
    {
        AdditionalText? baselineFile = additionalFiles.FirstOrDefault(file => Path.GetFileName(file.Path) == "model-baseline.json");

        if (baselineFile is null)
        {
            return new Dictionary<string, ModelSnapshot>();
        }

        string? text = baselineFile.GetText()?.ToString();

        if (string.IsNullOrWhiteSpace(text))
        {
            return new Dictionary<string, ModelSnapshot>();
        }

        JsonSerializerOptions options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        Dictionary<string, BaselineModel>? rawModels = JsonSerializer.Deserialize<Dictionary<string, BaselineModel>>(text, options);

        if (rawModels is null)
        {
            return new Dictionary<string, ModelSnapshot>();
        }

        return rawModels.ToDictionary(
            pair => pair.Key,
            pair => new ModelSnapshot(
                pair.Key,
                pair.Value.Version,
                pair.Value.Properties
                    .Select(property => new PropertySnapshot(
                        property.Name,
                        property.Type))
                    .ToArray()));
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