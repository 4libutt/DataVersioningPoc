using DataVersioningPoc.Generator.Snapshot;

if (args.Length != 2)
{
    Console.Error.WriteLine(
        "Usage: DataVersioningPoc.Generator <assembly-path> <baseline-path>");

    return 1;
}

string assemblyPath = Path.GetFullPath(args[0]);
string baselinePath = Path.GetFullPath(args[1]);

try
{
    ModelSnapshotBuilder builder = new();
    IReadOnlyList<DataVersioningPoc.Shared.ModelSnapshot> snapshots = builder.BuildSnapshots(assemblyPath);
    SnapshotWriter writer = new();
    writer.WriteIfChanged(baselinePath, snapshots);
    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine(exception);
    return 1;
}
