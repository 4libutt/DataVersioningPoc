using System.Reflection;
using DataVersioningPoc.Shared;

namespace DataVersioningPoc.Generator.Snapshot;

public sealed class ModelSnapshotBuilder
{
    private const string DataStructureVersionAttributeName = "DataVersioningPoc.Models.DataStructureVersionAttribute";

    public IReadOnlyList<ModelSnapshot> BuildSnapshots(string assemblyPath)
    {
        if (!File.Exists(assemblyPath))
        {
            throw new FileNotFoundException("The models assembly could not be found.", assemblyPath);
        }

        Assembly assembly = Assembly.LoadFrom(assemblyPath);
        List<ModelSnapshot> snapshots = [];
        IEnumerable<Type> modelTypes = assembly.GetTypes()
            .Where(type => type.IsClass || type.IsValueType)
            .Where(type => type is { IsAbstract: false, IsInterface: false });

        foreach (Type modelType in modelTypes)
        {
            CustomAttributeData? versionAttribute = modelType
                .CustomAttributes
                .FirstOrDefault(attribute => attribute.AttributeType.FullName == DataStructureVersionAttributeName);

            if (versionAttribute is null)
            {
                continue;
            }

            if (versionAttribute.ConstructorArguments.Count != 1)
            {
                throw new InvalidOperationException($"Invalid DataStructureVersionAttribute on {modelType.FullName}.");
            }

            int version = (int)versionAttribute.ConstructorArguments[0].Value!;

            NullabilityInfoContext nullabilityContext = new();

            PropertySnapshot[] properties = modelType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(property => property.GetMethod is not null)
                .Where(property => property.GetIndexParameters().Length == 0)
                .Select(property => new PropertySnapshot(property.Name, GetPropertyTypeName(nullabilityContext.Create(property))))
                .OrderBy(property => property.Name, StringComparer.Ordinal)
                .ToArray();

            ModelSnapshot snapshot = new(modelType.FullName ?? modelType.Name, version, properties);
            snapshots.Add(snapshot);
        }

        return snapshots.OrderBy(snapshot => snapshot.FullyQualifiedName, StringComparer.Ordinal).ToArray();
    }

    private static string GetPropertyTypeName(NullabilityInfo nullabilityInfo)
    {
        Type propertyType = nullabilityInfo.Type;
        // Nullable value types, bijvoorbeeld int? en DateTime?
        Type? nullableType = Nullable.GetUnderlyingType(propertyType);
        if (nullableType is not null)
        {
            return $"{GetTypeName(nullableType)}?";
        }

        string typeName;

        // Arrays
        if (propertyType.IsArray)
        {
            NullabilityInfo elementInfo = nullabilityInfo.ElementType!;
            typeName = $"{GetPropertyTypeName(elementInfo)}[]";
        }
        // Generieke types
        else if (propertyType.IsGenericType)
        {
            Type genericType = propertyType.GetGenericTypeDefinition();
            string genericTypeName = genericType.FullName!.Split('`')[0];
            string genericArguments = string.Join(", ", nullabilityInfo.GenericTypeArguments.Select(GetPropertyTypeName));
            typeName = $"{genericTypeName}<{genericArguments}>";
        }
        else
        {
            typeName = propertyType.FullName ?? propertyType.Name;
        }

        // Nullable reference types
        if (nullabilityInfo.ReadState == NullabilityState.Nullable)
        {
            typeName += "?";
        }

        return typeName;
    }

    private static string GetTypeName(Type type)
    {
        Type? nullableType = Nullable.GetUnderlyingType(type);
        if (nullableType is not null)
        {
            return $"{GetTypeName(nullableType)}?";
        }

        if (type.IsArray)
        {
            return $"{GetTypeName(type.GetElementType()!)}[]";
        }

        if (type.IsGenericType)
        {
            Type genericType = type.GetGenericTypeDefinition();
            string genericTypeName = genericType.FullName!.Split('`')[0];
            string genericArguments = string.Join(", ", type.GetGenericArguments().Select(GetTypeName));
            return $"{genericTypeName}<{genericArguments}>";
        }

        return type.FullName ?? type.Name;
    }
}
