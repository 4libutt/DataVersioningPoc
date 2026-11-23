using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DataVersioningPoc.Shared
{
    public static class ModelSnapshotSerializer
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never
        };

        public static Dictionary<string, ModelSnapshot> Deserialize(string json)
        {
            Dictionary<string, ModelSnapshot> snapshots = JsonSerializer.Deserialize<Dictionary<string, ModelSnapshot>>(json, SerializerOptions) ?? new Dictionary<string, ModelSnapshot>();
            foreach (KeyValuePair<string, ModelSnapshot> entry in snapshots)
            {
                entry.Value.FullyQualifiedName = entry.Key;
            }

            return snapshots;
        }

        public static string Serialize(IEnumerable<ModelSnapshot> snapshots)
        {
            Dictionary<string, ModelSnapshot> orderedSnapshots = snapshots
                .OrderBy(snapshot => snapshot.FullyQualifiedName, StringComparer.Ordinal)
                .ToDictionary(snapshot => snapshot.FullyQualifiedName, snapshot => new ModelSnapshot(snapshot.FullyQualifiedName, snapshot.Version, snapshot.Properties
                .OrderBy(property => property.Name, StringComparer.Ordinal).ToArray()));

            return JsonSerializer.Serialize(orderedSnapshots, SerializerOptions) + "\n";
        }
    }
}
