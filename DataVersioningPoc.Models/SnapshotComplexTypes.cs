using System;
using System.Collections.Generic;

namespace DataVersioningPoc.Models;

[DataStructureVersion(2)]
public class SnapshotComplexTypes
{
    public int Id { get; set; }

    public DateTime? BirthDate { get; set; }

    public int[] Scores { get; set; } = [];

    public List<string> Names { get; set; } = [];

    public Dictionary<string, int> Properties { get; set; } = [];

    public List<DateTime?> Dates { get; set; } = [];
    public string? MiddleName { get; set; }

    public List<string?> NullableNames { get; set; } = [];
}