using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace DataVersioningPoc.Shared;

public sealed class ModelSnapshot
{
    // om de modelstructuur vast te leggen voor de models
    public ModelSnapshot()
    {
        FullyQualifiedName = string.Empty;
        Properties = [];
    }

    public ModelSnapshot(string fullyQualifiedName, int version, IReadOnlyList<PropertySnapshot> properties)
    {
        FullyQualifiedName = fullyQualifiedName;
        Version = version;
        Properties = properties;
    }

    [JsonIgnore]
    public string FullyQualifiedName { get; set; }

    public int Version { get; set; }

    public IReadOnlyList<PropertySnapshot> Properties { get; set; }
}
