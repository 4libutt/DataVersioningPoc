namespace DataVersioningPoc.Models;

[DataStructureVersion(1)]
public class SnapshotTest
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public static string StaticName { get; set; } = string.Empty;

    private string InternalName { get; set; } = string.Empty;

    public string this[int index]
    {
        get => Name;
        set => Name = value;
    }
}
