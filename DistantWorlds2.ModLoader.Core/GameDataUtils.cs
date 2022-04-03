using System.Reflection;
using DistantWorlds.Types;
using YamlDotNet.RepresentationModel;

namespace DistantWorlds2.ModLoader;

public static class GameDataUtils
{
    public static YamlStream LoadYaml(Stream data)
    {
        using var sr = new StreamReader(data);
        var ys = new YamlStream();
        ys.Load(sr);
        return ys;
    }

    public static MemberInfo GetInstancePropertyOrField(Type type, string name)
        => type
            .GetMember(name, MemberTypes.Field | MemberTypes.Property,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
            .Single(m => m is FieldInfo { IsStatic: false } or PropertyInfo { GetMethod.IsStatic: false });

    public static Type GetType(MemberInfo m)
        => m switch
        {
            FieldInfo fi => fi.FieldType,
            PropertyInfo pi => pi.PropertyType,
            _ => throw new NotImplementedException()
        };

    public static object? GetValue(object obj, MemberInfo m)
        => m switch
        {
            FieldInfo fi => fi.GetValue(obj),
            PropertyInfo pi => pi.GetValue(obj),
            _ => throw new NotImplementedException()
        };
    public static void SetValue(object obj, MemberInfo m, object? value)
    {
        switch (m)
        {
            case FieldInfo fi:
                fi.SetValue(obj, value);
                break;
            case PropertyInfo pi:
                pi.SetValue(obj, value);
                break;
            default: throw new NotImplementedException();
        }
    }

    public static object GetRealNextId<T>(List<T> defs)
        => defs.Count;

    public static object GetRealNextId<T>(IndexedList<T> defs)
    {
        var nextId = defs.Count;
        while (defs.ContainsId(nextId))
            nextId += 1;
        //defs.SetNextId(nextId + 1);
        return nextId;
    }
    public static Type GetFieldOrPropertyType(MemberInfo idField)
        => idField is FieldInfo idFi ? idFi.FieldType : ((PropertyInfo)idField).PropertyType;
}
