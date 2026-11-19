using System;
using System.Collections.Generic;
using System.Text;

namespace DataVersioningPoc.Shared;

public sealed class PropertySnapshot
{
    //Wat er bewaard wordt van iedere property
    public PropertySnapshot()
    {
        Name = string.Empty;
        Type = string.Empty;
    }

    public PropertySnapshot(string name, string type)
    {
        Name = name;
        Type = type;
    }

    public string Name { get; set; }

    public string Type { get; set; }
}
