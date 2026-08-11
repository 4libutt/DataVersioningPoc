using System;
using System.Collections.Generic;
using System.Text;

namespace DataVersioningPoc.Analyzer.Models;

public sealed class ModelSnapshot
{
    public ModelSnapshot(
        string fullyQualifiedName,
        int version,
        IReadOnlyList<PropertySnapshot> properties)
    {
        FullyQualifiedName = fullyQualifiedName;
        Version = version;
        Properties = properties;
    }

    public string FullyQualifiedName { get; }

    public int Version { get; }

    public IReadOnlyList<PropertySnapshot> Properties { get; }
}
