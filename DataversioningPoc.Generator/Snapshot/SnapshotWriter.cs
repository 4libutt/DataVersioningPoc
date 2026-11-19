using DataVersioningPoc.Shared;

namespace DataVersioningPoc.Generator.Snapshot
{
    public sealed class SnapshotWriter
    {
        public void WriteIfChanged(string baselinePath, IReadOnlyList<ModelSnapshot> snapshots)
        {
            string newJson = ModelSnapshotSerializer.Serialize(snapshots);

            string currentJson = File.Exists(baselinePath) ? File.ReadAllText(baselinePath) : string.Empty;

            if (string.Equals(currentJson, newJson, StringComparison.Ordinal))
            {
                Console.WriteLine("Snapshot is already up to date.");
                return;
            }

            string? directory = Path.GetDirectoryName(baselinePath);

            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(baselinePath, newJson);

            Console.WriteLine("Snapshot baseline updated.");
        }
    }
}
