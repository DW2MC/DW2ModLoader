using System.Reflection;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.TypeInspectors;

namespace ModDevToolsMod;

public class DefRefInspector : TypeInspectorSkeleton {

  private readonly ITypeInspector _innerTypeDescriptor;

  private readonly Type _rootDefType;

  public DefRefInspector(ITypeInspector innerTypeDescriptor, Type rootDefType) {
    _innerTypeDescriptor = innerTypeDescriptor;
    _rootDefType = rootDefType;
  }

  public override IEnumerable<IPropertyDescriptor> GetProperties(Type type, object? container) {
    foreach (var prop in _innerTypeDescriptor.GetProperties(type, container)) {
      var propType = prop.TypeOverride ?? prop.Type;

      if (Mod.DefTypes.Contains(propType) && propType != _rootDefType) {
        if (!Mod.DefIdFields.TryGetValue(propType.Name, out var fieldName))
          throw new NotImplementedException();

        var idProp = propType.GetMember(fieldName).First();
        prop.TypeOverride
          = idProp switch {
            PropertyInfo pi => pi.PropertyType,
            FieldInfo fi => fi.FieldType,
            _ => throw new NotImplementedException()
          };
      }

      yield return prop;
    }
  }

}