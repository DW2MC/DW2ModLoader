using System.Collections;
using System.ComponentModel;
using System.IO.Hashing;
using System.IO.MemoryMappedFiles;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using DistantWorlds.Types;
using JetBrains.Annotations;
using MonoMod.Utils;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization.NodeDeserializers;
using YamlDotNet.Serialization.ObjectFactories;
using YamlDotNet.Serialization.TypeInspectors;

namespace DistantWorlds2.ModLoader;

public static class GameDataUtils
{
    public static readonly ISerializer Serializer = new SerializerBuilder()
        .WithNamingConvention(NullNamingConvention.Instance)
        .WithIndentedSequences()
        .Build();

    private static readonly DeserializerBuilder DeserializerBase = new DeserializerBuilder()
        .WithNamingConvention(NullNamingConvention.Instance)
        .IgnoreUnmatchedProperties();

    public static readonly IDeserializer Deserializer = DeserializerBase
        .WithNodeDeserializer(new FormulaScalarNodeDeserializer(new SimpleMathDsl()))
        .Build();

    public static YamlStream LoadYaml(Stream data)
    {
        using var sr = new StreamReader(data);
        var ys = new YamlStream();
        ys.Load(sr);
        return ys;
    }

    public static unsafe void PatchData(Type type, object defs, YamlSequenceNode mods, string? idFieldName = null)
    {
        var m = typeof(GameDataUtils).GetMethod(nameof(GenericPatchData))!.MakeGenericMethod(type);
        m.Invoke(null, new[] { defs, mods, idFieldName });
    }

    public static void GenericPatchData<T>(List<T> defs, YamlSequenceNode mods, string? idFieldName = null) where T : class
    {
        if (defs is null) throw new ArgumentNullException(nameof(defs));

        //var ex = Serializer.Serialize(defs.First()!);

        var type = typeof(T);

        var fieldInfos = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
        var fields = fieldInfos
            .ToDictionary(f => f.Name);

        var propInfos = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var props = propInfos
            .ToDictionary(p => p.Name);

        var idField =
            idFieldName is not null
                ? fields.TryGetValue(idFieldName, out var fi)
                    ? (MemberInfo?)fi
                    : props.TryGetValue(idFieldName, out var pi)
                        ? pi
                        : null
                : null;

        idField ??= (MemberInfo?)fieldInfos.FirstOrDefault(f => f.Name.EndsWith("DefinitionId"))
            ?? propInfos.FirstOrDefault(p => p.Name.EndsWith("DefinitionId"));

        if (idField is null)
        {
            Console.Error.WriteLine($"Unable to locate id field for {type.Name}");
            return;
        }

        var idFieldAsPropInfo = idField as PropertyInfo;
        var idFieldAsFieldInfo = idField as FieldInfo;
        var idFieldIsFieldInfo = idFieldAsFieldInfo is not null;

        var idFieldType = idFieldIsFieldInfo
            ? idFieldAsFieldInfo!.FieldType
            : idFieldAsPropInfo!.PropertyType;

        object GetId(object item)
            => idFieldIsFieldInfo
                ? idFieldAsFieldInfo!.GetValue(item)
                : idFieldAsPropInfo!.GetValue(item);

        var idFieldComparer = (IEqualityComparer)typeof(EqualityComparer<>).MakeGenericType(idFieldType)
            .GetProperty(nameof(EqualityComparer<object>.Default), BindingFlags.Public | BindingFlags.Static)!
            .GetValue(null)!;

        bool CheckId(object item, object id)
            => idFieldComparer!.Equals(GetId(item), id);

        foreach (var mod in mods)
        {
            var modNodeType = mod.NodeType;
            var instr = mod.Tag.ToString();
            switch (instr)
            {

                case "state" when mod is YamlMappingNode item: {

                    foreach (var kv in item)
                    {
                        var keyNode = kv.Key;
                        if (keyNode is not YamlScalarNode keyScalar)
                        {
                            Console.Error.WriteLine($"Can't parse state manipulation key @ {keyNode.Start}");
                            continue;
                        }
                        var keyStr = keyScalar.Value;
                        if (keyStr is null)
                        {
                            Console.Error.WriteLine($"Can't parse state manipulation key @ {keyScalar.Start}");
                            continue;
                        }
                        var valNode = kv.Value;
                        if (valNode is not YamlScalarNode valScalar)
                        {
                            Console.Error.WriteLine($"Can't parse state manipulation value @ {valNode.Start}");
                            continue;
                        }
                        var valStr = valScalar.Value;
                        if (valStr is null)
                        {
                            Console.Error.WriteLine($"Can't parse state manipulation value @ {valScalar.Start}");
                            continue;
                        }

                        if (valStr.Trim().Equals("delete()", StringComparison.OrdinalIgnoreCase))
                        {
                            if (ModManager.Instance.SharedVariables.TryRemove(keyStr, out var _))
                                continue;

                            Console.Error.WriteLine($"Failed to remove {keyStr} from state @ {valScalar.Start}");
                            continue;
                        }

                        ModManager.Instance.SharedVariables.AddOrUpdate(keyStr,
                            _ => VariableMathDsl.NaN.Parse(valStr).Compile(true)(),
                            (_, old) => {
                                double v = 0;
                                try
                                {
                                    v = ((IConvertible)old).ToDouble(null);
                                }
                                catch (Exception ex)
                                {
                                    ModManager.OnUnhandledException(ExceptionDispatchInfo.Capture(ex));
                                }
                                return new VariableMathDsl(v).Parse(valStr).Compile(true)();
                            });
                    }

                    break;
                }

                case "state":
                    Console.Error.WriteLine($"Can't parse state manipulation instruction @ {mod.Start}");
                    break;

                case "remove": {
                    if (mod is YamlScalarNode removeNode)
                    {
                        var idStr = removeNode.Value;

                        if (idStr is null)
                        {
                            Console.Error.WriteLine($"Can't parse remove instruction @ {removeNode.Start}, missing key expression");
                            break;
                        }

                        var idVal = ((IConvertible)idStr).ToType(idFieldType, null);

                        var found = defs.Find(x => CheckId(x!, idVal));
                        if (found is null)
                            Console.Error.WriteLine($"Failed to find {idStr} @ {removeNode.Start}");
                        else if (!defs.Remove(found))
                            Console.Error.WriteLine($"Failed to remove {idStr} @ {removeNode.Start}");
                    }
                    else
                    {
                        Console.Error.WriteLine($"Can't parse remove instruction @ {mod.Start}, unsupported expression");
                    }
                    break;
                }

                case "add" when mod is YamlMappingNode item: {
                    var parsed = Deserializer.Deserialize(item, type);
                    if (parsed is null)
                    {
                        Console.Error.WriteLine($"Failed to parse {type.Name} @ {item.Start}");
                        break;
                    }
                    defs.Add((T)parsed);
                    break;
                }

                case "add":
                    Console.Error.WriteLine($"Can't parse add instruction @ {mod.Start}");
                    break;

                case "replace" when mod is YamlMappingNode item: {

                    var idKv = item.FirstOrDefault();
                    if (idKv.Key is YamlScalarNode idKeyNode)
                    {
                        var idKeyStr = idKeyNode.Value;
                    }
                    if (idKv.Value is YamlScalarNode idValNode)
                    {
                        var idValStr = idValNode.Value;

                    }

                    var parsed = Deserializer.Deserialize(item, type);
                    if (parsed is null)
                    {
                        Console.Error.WriteLine($"Failed to parse {type.Name} @ {item.Start}");
                        break;
                    }

                    var parsedId = GetId(parsed);
                    var index = defs.FindIndex(m => m is not null && GetId(m) == parsedId);
                    if (index < 0)
                    {
                        Console.Error.WriteLine($"Failed to replace {type.Name} @ {item.Start}; couldn't find {parsedId}");
                        break;
                    }

                    defs[index] = (T)parsed;
                    break;
                }
                case "replace":
                    Console.Error.WriteLine($"Can't parse replace instruction @ {mod.Start}");
                    break;

                case "update" when mod is YamlMappingNode item: {
                    Console.Error.WriteLine($"Update instruction not yet implemented @ {mod.Start}");
                    break;
                }

                case "update":
                    Console.Error.WriteLine($"Can't parse update instruction @ {mod.Start}");
                    break;
            }
        }
    }

    public static unsafe void PatchIndexedData(Type type, object defs, YamlSequenceNode mods, string? idFieldName = null)
    {
        var m = typeof(GameDataUtils).GetMethod(nameof(GenericPatchIndexedData))!.MakeGenericMethod(type);
        m.Invoke(null, new[] { defs, mods, idFieldName });
    }
    public static void GenericPatchIndexedData<T>(IndexedList<T> defs, YamlSequenceNode mods, string? idFieldName = null) where T : class
    {
        if (defs is null) throw new ArgumentNullException(nameof(defs));

        //var ex = Serializer.Serialize(defs.First()!);

        var type = typeof(T);

        var fieldInfos = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
        var fields = fieldInfos
            .ToDictionary(f => f.Name);

        var propInfos = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var props = propInfos
            .ToDictionary(p => p.Name);

        var idField =
            idFieldName is not null
                ? fields.TryGetValue(idFieldName, out var fi)
                    ? (MemberInfo?)fi
                    : props.TryGetValue(idFieldName, out var pi)
                        ? pi
                        : null
                : null;

        idField ??= (MemberInfo?)fieldInfos.FirstOrDefault(f => f.Name.EndsWith("DefinitionId"))
            ?? propInfos.FirstOrDefault(p => p.Name.EndsWith("DefinitionId"));

        if (idField is null)
        {
            Console.Error.WriteLine($"Unable to locate id field for {type.Name}");
            return;
        }

        var idFieldAsPropInfo = idField as PropertyInfo;
        var idFieldAsFieldInfo = idField as FieldInfo;
        var idFieldIsFieldInfo = idFieldAsFieldInfo is not null;

        var idFieldType = idFieldIsFieldInfo
            ? idFieldAsFieldInfo!.FieldType
            : idFieldAsPropInfo!.PropertyType;

        object GetId(object item)
            => idFieldIsFieldInfo
                ? idFieldAsFieldInfo!.GetValue(item)
                : idFieldAsPropInfo!.GetValue(item);

        var idFieldComparer = (IEqualityComparer)typeof(EqualityComparer<>).MakeGenericType(idFieldType)
            .GetProperty(nameof(EqualityComparer<object>.Default), BindingFlags.Public | BindingFlags.Static)!
            .GetValue(null)!;

        bool CheckId(object item, object id)
            => idFieldComparer!.Equals(GetId(item), id);

        void SetId(object item, object id)
        {
            if (idFieldIsFieldInfo)
                idFieldAsFieldInfo!.SetValue(item, ((IConvertible)id).ToType(idFieldAsFieldInfo.FieldType, null) );
            else
                idFieldAsPropInfo!.SetValue(item, ((IConvertible)id).ToType(idFieldAsPropInfo.PropertyType, null));
        }

        object ConvertToIdType(object id)
            => ((IConvertible)id).ToType(idFieldType!, null);

        int ConvertToInt(object id)
            => ((IConvertible)id).ToInt32(null);

        idFieldName ??= idField.Name;

        var idFieldNamePrefixed = "$" + idFieldName;

        foreach (var instrModNode in mods)
        {
            if (instrModNode is not YamlMappingNode instrMod)
            {
                Console.Error.WriteLine($"Can't parse instruction @ {instrModNode.Start}");
                break;
            }

            KeyValuePair<YamlNode, YamlNode> oneInstrMod;
            try
            {
                oneInstrMod = instrMod.Single();
            }
            catch
            {
                Console.Error.WriteLine($"Can't parse instruction @ {instrMod.Start}");
                break;
            }

            var instrNode = oneInstrMod.Key;

            if (instrNode is not YamlScalarNode instrScalarNode)
            {
                Console.Error.WriteLine($"Can't parse instruction @ {instrNode.Start}");
                break;
            }

            var instr = instrScalarNode.Value;
            var mod = oneInstrMod.Value;
            switch (instr)
            {
                case "state" when mod is YamlMappingNode item: {
                    foreach (var kv in item)
                    {
                        var keyNode = kv.Key;
                        if (keyNode is not YamlScalarNode keyScalar)
                        {
                            Console.Error.WriteLine($"Can't parse state manipulation key @ {keyNode.Start}");
                            continue;
                        }
                        var keyStr = keyScalar.Value;
                        if (keyStr is null)
                        {
                            Console.Error.WriteLine($"Can't parse state manipulation key @ {keyScalar.Start}");
                            continue;
                        }
                        var valNode = kv.Value;
                        if (valNode is not YamlScalarNode valScalar)
                        {
                            Console.Error.WriteLine($"Can't parse state manipulation value @ {valNode.Start}");
                            continue;
                        }
                        var valStr = valScalar.Value;
                        if (valStr is null)
                        {
                            Console.Error.WriteLine($"Can't parse state manipulation value @ {valScalar.Start}");
                            continue;
                        }

                        if (valStr.Trim().Equals("delete()", StringComparison.OrdinalIgnoreCase))
                        {
                            if (ModManager.Instance.SharedVariables.TryRemove(keyStr, out var _))
                                continue;

                            Console.Error.WriteLine($"Failed to remove {keyStr} from state @ {valScalar.Start}");
                            continue;
                        }

                        ModManager.Instance.SharedVariables.AddOrUpdate(keyStr,
                            _ => VariableMathDsl.NaN.Parse(valStr).Compile(true)(),
                            (_, old) => {
                                double v = 0;
                                try
                                {
                                    v = ((IConvertible)old).ToDouble(null);
                                }
                                catch (Exception ex)
                                {
                                    ModManager.OnUnhandledException(ExceptionDispatchInfo.Capture(ex));
                                }
                                return new VariableMathDsl(v).Parse(valStr).Compile(true)();
                            });
                    }

                    break;
                }

                case "state":
                    Console.Error.WriteLine($"Can't parse state manipulation instruction @ {mod.Start}");
                    break;

                case "remove": {
                    if (mod is YamlScalarNode removeNode)
                    {
                        var value = removeNode.Value;

                        if (!int.TryParse(value, out var id))
                        {
                            // check if its a shared variable
                            if (!ModManager.Instance.SharedVariables.TryGetValue(value!, out var varValue))
                            {
                                Console.Error.WriteLine($"Can't parse remove instruction @ {removeNode.Start}, unsupported key expression");
                                break;
                            }

                            id = ConvertToInt(varValue);
                        }

                        var index = defs.GetIndex(id);

                        if (index == -1)
                        {
                            Console.Error.WriteLine($"Could not find {type.Name} {id} @ {removeNode.Start}");
                            break;
                        }

                        var removed = defs[index];
                        if (removed == null)
                        {
                            Console.Error.WriteLine(
                                $"Could not remove {type.Name} {id} @ {removeNode.Start}; appears to have already been removed");
                            break;
                        }

                        defs[index] = default!;

                        //defs.RebuildIndexes();

                        var removedAsString = removed.ToString();
                        Console.WriteLine($"Removed {type.Name} {id}: {removedAsString.Substring(0, Math.Min(removedAsString.Length, 64))}");

                    }
                    else
                    {
                        Console.Error.WriteLine($"Can't parse remove instruction @ {mod.Start}, unsupported expression");
                        break;
                    }
                    break;
                }

                case "add" when mod is YamlMappingNode item: {
                    var idLookupReq = item.FirstOrDefault(kv => kv.Key is YamlScalarNode sk && sk.Value == idFieldNamePrefixed);
                    // don't issue error on explicit id set
                    if (idLookupReq.Key == default && !item.Any(kv => kv.Key is YamlScalarNode sk && sk.Value == idFieldName))
                    {
                        Console.Error.WriteLine($"Failed to parse {type.Name} @ {item.Start}");
                        break;
                    }

                    var parsed = Deserializer.Deserialize(item, type);
                    if (parsed is null)
                    {
                        Console.Error.WriteLine($"Failed to parse {type.Name} @ {item.Start}");
                        break;
                    }

                    if (idLookupReq.Value is YamlScalarNode idLookupVarNode)
                    {
                        var idLookupVar = idLookupVarNode.Value;
                        var value = ModManager.Instance.SharedVariables.GetOrAdd(idLookupVar!, _ => GetRealNextId(defs));
                        SetId(parsed, ConvertToIdType(value));
                    }

                    var id = ConvertToInt(GetId(parsed));
                    var contained = defs.ContainsIdThreadSafe(id);
                    if (contained && defs[id] != null)
                    {
                        Console.Error.WriteLine($"Failed to add {type.Name} @ {item.Start}; {id} already defined");
                        break;
                    }
                    var parsedAsType = (T)parsed;
                    if (contained)
                        defs[id] = parsedAsType;
                    else
                        defs.Add(parsedAsType);
                    if (!defs[id]?.Equals(parsedAsType) ?? false)
                    {
                        Console.Error.WriteLine($"Failed to validate after adding {type.Name} @ {item.Start}; {id} not found");
                        break;
                    }
                    //defs.RebuildIndexes();
                    var parsedAsString = parsedAsType.ToString();
                    Console.WriteLine($"Added {type.Name} {id}: {parsedAsString.Substring(0, Math.Min(parsedAsString.Length, 64))}");
                    break;
                }

                case "add":
                    Console.Error.WriteLine($"Can't parse add instruction @ {mod.Start}");
                    break;

                case "replace" when mod is YamlMappingNode item: {
                    int id;
                    // don't issue error on explicit id set
                    var idLookupReq = item.FirstOrDefault(kv => kv.Key is YamlScalarNode sk && sk.Value == idFieldNamePrefixed);

                    if (idLookupReq.Value is YamlScalarNode idLookupVarNode)
                    {
                        var idLookupVar = idLookupVarNode.Value;
                        var value = ModManager.Instance.SharedVariables.GetOrAdd(idLookupVar!, _ => GetRealNextId(defs));

                        item.Children.Remove(idLookupReq);

                        id = ConvertToInt(value);
                    }
                    else
                    {
                        var idKvNode = item.FirstOrDefault(kv => kv.Key is YamlScalarNode sk && sk.Value == idFieldName);

                        if (idKvNode.Key == default)
                        {
                            Console.Error.WriteLine($"Failed find key identifier for {type.Name} @ {item.Start}");
                            break;
                        }

                        if (idKvNode.Value is not YamlScalarNode idValNode)
                        {
                            Console.Error.WriteLine($"Failed to parse key identifier for {type.Name} @ {item.Start}");
                            break;
                        }

                        var idStr = idValNode.Value;
                        if (idStr is null)
                        {
                            Console.Error.WriteLine($"Failed to parse key identifier for {type.Name} @ {item.Start}");
                            break;
                        }

                        id = (int)VariableMathDsl.NaN.Parse(idStr).Compile(true)();

                        item.Children.Remove(idKvNode);

                    }

                    {
                        var old = defs[id]!;
                        T? @new;
                        var created = false;
                        var defaultObjectFactory = new DefaultObjectFactory();
                        var dsl = new PropertyMathDsl<T>(double.NaN, old, null!);
                        var dsz = new DeserializerBuilder()
                            .WithNamingConvention(NullNamingConvention.Instance)
                            .IgnoreUnmatchedProperties()
                            .WithObjectFactory(t => {
                                // much hacks
                                var o = defaultObjectFactory.Create(t);
                                dsl.ContextStack.Push(o);
                                if (created || t != typeof(T))
                                    return o;
                                @new = (T)o;
                                SetId(@new, id);
                                dsl.New = @new;
                                created = true;
                                return @new;
                            })
                            .WithTypeInspector(x => new HackyTypeInspectorWrapper(x, dsl,
                                () => dsl.Context,
                                () => dsl.ContextStack.Pop()
                            ))
                            .WithNodeDeserializer(new FormulaScalarNodeDeserializer(dsl))
                            .Build();

                        var parsed = dsz.Deserialize(item, type);
                        if (parsed is null)
                        {
                            Console.Error.WriteLine($"Failed to parse {type.Name} @ {item.Start}");
                            break;
                        }

                        if (defs[id] == null)
                        {
                            Console.Error.WriteLine($"Failed to replace {type.Name} @ {item.Start}; {id} not found");
                            break;
                        }
                        var parsedAsType = (T)parsed;

                        defs[id] = parsedAsType;

                        if (!defs[id]?.Equals(parsedAsType) ?? false)
                        {
                            Console.Error.WriteLine($"Failed to validate after replacing {type.Name} @ {item.Start}; {id} not found");
                            break;
                        }
                        //defs.RebuildIndexes();
                        var parsedAsString = parsedAsType.ToString();
                        Console.WriteLine($"Replaced {type.Name} {id}: {parsedAsString.Substring(0, Math.Min(parsedAsString.Length, 64))}");
                        break;
                    }
                }

                case "replace":
                    Console.Error.WriteLine($"Can't parse replace instruction @ {mod.Start}");
                    break;

                case "update" when mod is YamlMappingNode item: {
                    int id;
                    // don't issue error on explicit id set
                    var idLookupReq = item.FirstOrDefault(kv => kv.Key is YamlScalarNode sk && sk.Value == idFieldNamePrefixed);

                    if (idLookupReq.Value is YamlScalarNode idLookupVarNode)
                    {
                        var idLookupVar = idLookupVarNode.Value;
                        var value = ModManager.Instance.SharedVariables.GetOrAdd(idLookupVar!, _ => GetRealNextId(defs));

                        item.Children.Remove(idLookupReq);

                        id = ConvertToInt(value);
                    }
                    else
                    {
                        var idKvNode = item.FirstOrDefault(kv => kv.Key is YamlScalarNode sk && sk.Value == idFieldName);

                        if (idKvNode.Key == default)
                        {
                            Console.Error.WriteLine($"Failed find key identifier for {type.Name} @ {item.Start}");
                            break;
                        }

                        if (idKvNode.Value is not YamlScalarNode idValNode)
                        {
                            Console.Error.WriteLine($"Failed to parse key identifier for {type.Name} @ {item.Start}");
                            break;
                        }

                        var idStr = idValNode.Value;
                        if (idStr is null)
                        {
                            Console.Error.WriteLine($"Failed to parse key identifier for {type.Name} @ {item.Start}");
                            break;
                        }

                        id = (int)VariableMathDsl.NaN.Parse(idStr).Compile(true)();

                        item.Children.Remove(idKvNode);

                    }

                    {
                        var old = defs[id]!;
                        T? @new;
                        var created = false;
                        var defaultObjectFactory = new DefaultObjectFactory();
                        var dsl = new PropertyMathDsl<T>(double.NaN, old, null!);
                        var dsz = new DeserializerBuilder()
                            .WithNamingConvention(NullNamingConvention.Instance)
                            .IgnoreUnmatchedProperties()
                            .WithObjectFactory(t => {
                                // much hacks
                                if (created || t != typeof(T))
                                    return defaultObjectFactory.Create(t);
                                dsl.ContextStack.Push(old);
                                @new = old;
                                SetId(@new, id);
                                dsl.New = @new;
                                created = true;
                                return @new;
                            })
                            .WithTypeInspector(x => new HackyTypeInspectorWrapper(x, dsl,
                                () => dsl.Context,
                                () => dsl.ContextStack.Pop()
                            ))
                            .WithNodeDeserializer(new FormulaScalarNodeDeserializer(dsl))
                            .Build();

                        var parsed = dsz.Deserialize(item, type);
                        if (parsed is null)
                        {
                            Console.Error.WriteLine($"Failed to parse {type.Name} @ {item.Start}");
                            break;
                        }

                        if (defs[id] == null)
                        {
                            Console.Error.WriteLine($"Failed to update {type.Name} @ {item.Start}; {id} not found");
                            break;
                        }
                        var parsedAsType = (T)parsed;

                        //defs[id] = parsedAsType;

                        if (!old.Equals(parsedAsType))
                        {
                            Console.Error.WriteLine($"Failed to validate after updating {type.Name} @ {item.Start}; {id} not found");
                            break;
                        }

                        if (!defs[id]?.Equals(parsedAsType) ?? false)
                        {
                            Console.Error.WriteLine($"Failed to validate after updating {type.Name} @ {item.Start}; {id} not found");
                            break;
                        }
                        //defs.RebuildIndexes();
                        var parsedAsString = parsedAsType.ToString();
                        Console.WriteLine($"Updated {type.Name} {id}: {parsedAsString.Substring(0, Math.Min(parsedAsString.Length, 64))}");
                        break;
                    }
                    break;
                }

                case "update":
                    Console.Error.WriteLine($"Can't parse update instruction @ {mod.Start}");
                    break;
            }
        }
    }
    private static object GetRealNextId<T>(IndexedList<T> defs)
    {
        var nextId = defs.Count;
        while (defs.ContainsId(nextId))
            nextId += 1;
        //defs.SetNextId(nextId + 1);
        return nextId;
    }
    private static Type GetFieldOrPropertyType(MemberInfo idField)
        => idField is FieldInfo idFi ? idFi.FieldType : ((PropertyInfo)idField).PropertyType;
}

[PublicAPI]
public static class DataUtils
{
    public static byte[] GetDirectoryHash(NonCryptographicHashAlgorithm hasher, string dir)
    {
        ComputeDirectoryHash(hasher, dir);

        var hash = new byte[hasher.HashLengthInBytes];

        if (!hasher.TryGetCurrentHash(hash, out _))
            throw new NotImplementedException();

        return hash;
    }

    public static void ComputeDirectoryHash(NonCryptographicHashAlgorithm hasher, string dir)
    {
        var filePaths = Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories)
            .ToArray();

        Array.Sort(filePaths, StringComparer.OrdinalIgnoreCase);

        foreach (var filePath in filePaths)
        {
            var filePathChars = MemoryMarshal.AsBytes<char>(filePath.ToCharArray());
            hasher.Append(filePathChars);
            ComputeFileHash(hasher, filePath);
            break;
        }
    }

    public static void ComputeFileHash(NonCryptographicHashAlgorithm hasher, string filePath)
    {

        //using var fileStream = File.OpenRead(filePath);
        //var fileLength = fileStream.Length;

        /* for sufficiently big files, spare the memory pressure?
            if (fileLength > int.MaxValue)
            {
                hasher.Append(fileStream);
                return;
            }
            */

        // fast memory mapped file hashing
        var fileLength = new FileInfo(filePath).Length;
        using var mapping = MemoryMappedFile.CreateFromFile(filePath, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
        /*
        using var mapping = MemoryMappedFile.CreateFromFile(
            fileStream,
            null,
            fileLength,
            MemoryMappedFileAccess.Read,
            null,
            0,
            false
        );*/

        // fallback copies for files >2GB
        if (fileLength > int.MaxValue)
        {
            var mapStream = mapping.CreateViewStream(0, fileLength, MemoryMappedFileAccess.Read);

            hasher.Append(mapStream);
            return;
        }

        using var view = mapping.CreateViewAccessor(0, fileLength, MemoryMappedFileAccess.Read);

        unsafe
        {
            byte* p = default;

            view.SafeMemoryMappedViewHandle.AcquirePointer(ref p);
            try
            {
                if (view.PointerOffset != 0)
                    throw new NotImplementedException();

                var viewLength = view.SafeMemoryMappedViewHandle.ByteLength;

                if (viewLength < (ulong)fileLength)
                    throw new NotImplementedException();

                var span = new ReadOnlySpan<byte>(p, (int)fileLength);

                hasher.Append(span);
            }
            finally
            {
                view.SafeMemoryMappedViewHandle.ReleasePointer();
            }
        }
    }
}
