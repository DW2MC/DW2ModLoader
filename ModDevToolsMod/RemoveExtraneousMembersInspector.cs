using System.Collections.Immutable;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.TypeInspectors;

namespace ModDevToolsMod;

public class RemoveExtraneousMembersInspector : TypeInspectorSkeleton {

  private static readonly ImmutableHashSet<Type> SkippedTypes = ImmutableHashSet.CreateRange(new[] {
    typeof(Xenko.Graphics.Texture)
  });

  private static readonly ImmutableHashSet<Type> SkippedGtds = ImmutableHashSet.CreateRange(new[] {
    typeof(Xenko.Graphics.GeometricMeshData<>)
  });

  private readonly ITypeInspector _innerTypeDescriptor;

  public RemoveExtraneousMembersInspector(ITypeInspector innerTypeDescriptor)
    => _innerTypeDescriptor = innerTypeDescriptor;

  public override IEnumerable<IPropertyDescriptor> GetProperties(Type type, object? container) {
    foreach (var prop in _innerTypeDescriptor.GetProperties(type, container)) {
      var propType = prop.TypeOverride ?? prop.Type;

      if (propType.IsConstructedGenericType)
        if (SkippedGtds.Contains(propType.GetGenericTypeDefinition()))
          continue;

      if (SkippedTypes.Contains(propType))
        continue;

      yield return prop;
    }
  }

}