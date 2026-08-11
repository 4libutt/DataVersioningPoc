using System;
using System.Collections.Generic;
using System.Text;

namespace DataVersioningPoc.Analyzer.Models;

public sealed class PropertySnapshot
{
    public PropertySnapshot(string name, string type)
    {
        Name = name;
        Type = type;
    }

    public string Name { get; }

    public string Type { get; }
}
