namespace DataVersioningPoc.Models;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class DataStructureVersionAttribute : Attribute
{
    public DataStructureVersionAttribute(int version)
    {
        Version = version;
    }

    public int Version { get; }
}
