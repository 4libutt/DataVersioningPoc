using DataVersioningPoc.Shared;

namespace DataVersioningPoc.Analyzer.Comparison;

public sealed class ModelComparer
{
    public IReadOnlyList<ModelChange> Compare(ModelSnapshot current, ModelSnapshot baseline)
    {
        var changes = new List<ModelChange>();
        var baselineProperties = baseline.Properties.ToDictionary(property => property.Name);
        var currentProperties = current.Properties.ToDictionary(property => property.Name);

        // Added / changed
        foreach (var currentProperty in current.Properties)
        {
            if (!baselineProperties.TryGetValue(currentProperty.Name, out var baselineProperty))
            {
                changes.Add(new ModelChange(ModelChangeType.Added, currentProperty.Name, newType: currentProperty.Type));
                continue;
            }

            if (baselineProperty.Type != currentProperty.Type)
            {
                changes.Add(new ModelChange(ModelChangeType.TypeChanged, currentProperty.Name, oldType: baselineProperty.Type, newType: currentProperty.Type));
            }
        }

        // Removed
        foreach (var baselineProperty in baseline.Properties)
        {
            if (!currentProperties.ContainsKey(baselineProperty.Name))
            {
                changes.Add(new ModelChange(ModelChangeType.Removed, baselineProperty.Name, oldType: baselineProperty.Type));
            }
        }

        return changes;
    }
}