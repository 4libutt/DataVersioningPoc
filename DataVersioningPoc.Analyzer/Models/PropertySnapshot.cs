using System;
using System.Collections.Generic;
using System.Text;

namespace DataVersioningPoc.Analyzer.Models;

public sealed class PropertySnapshot
{
    //Wat er bewaard wordt van iedere property
    public PropertySnapshot(string name, string type)
    {
        Name = name;
        Type = type;
    }

    public string Name { get; }

    public string Type { get; }
}
