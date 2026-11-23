using System;
using System.Collections.Generic;
using System.Text;

namespace DataVersioningPoc.Analyzer.Comparison
{
    public sealed class ModelChange
    {
        public ModelChange(ModelChangeType type, string propertyName, string? oldType = null, string? newType = null)
        {
            Type = type;
            PropertyName = propertyName;
            OldType = oldType;
            NewType = newType;
        }

        public ModelChangeType Type { get; }

        public string PropertyName { get; }

        public string? OldType { get; }

        public string? NewType { get; }
    }
}
