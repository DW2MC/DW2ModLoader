using System.Collections;
using System.Collections.Immutable;
using System.Globalization;
using System.Reflection;
using System.Runtime.ExceptionServices;
using DistantWorlds.Types;
using YamlDotNet.RepresentationModel;

namespace DistantWorlds2.ModLoader;

using static GameDataUtils;

public static class GameDataDefinitionPatching
{
    private static readonly ImmutableDictionary<string, Type> DefTypes = ImmutableDictionary.CreateRange(new KeyValuePair<string, Type>[]
    {
        new(nameof(OrbType), typeof(OrbType)),
        new(nameof(Resource), typeof(Resource)),
        new(nameof(ComponentDefinition), typeof(ComponentDefinition)),
        new(nameof(Race), typeof(Race)),
        new(nameof(Artifact), typeof(Artifact)),
        new(nameof(PlanetaryFacilityDefinition), typeof(PlanetaryFacilityDefinition)),
        new(nameof(ColonyEventDefinition), typeof(ColonyEventDefinition)),
        new(nameof(ResearchProjectDefinition), typeof(ResearchProjectDefinition)),
        new(nameof(TroopDefinition), typeof(TroopDefinition)),
        new(nameof(CreatureType), typeof(CreatureType)),
        new(nameof(SpaceItemType), typeof(SpaceItemType)),
        new(nameof(Government), typeof(Government)),
        new(nameof(DesignTemplate), typeof(DesignTemplate)),
        new(nameof(ShipHull), typeof(ShipHull)),
        new(nameof(FleetTemplate), typeof(FleetTemplate)),
        new(nameof(ArmyTemplate), typeof(ArmyTemplate)),
        new(nameof(GameEvent), typeof(GameEvent)),
        new(nameof(LocationEffectGroupDefinition), typeof(LocationEffectGroupDefinition)),
        new(nameof(CharacterAnimation), typeof(CharacterAnimation)),
        new(nameof(CharacterRoom), typeof(CharacterRoom)),
        new(nameof(EmpirePolicy), typeof(EmpirePolicy))
    });

    private static readonly ImmutableDictionary<string, Func<object>> StaticDefs = ImmutableDictionary.CreateRange(
        new KeyValuePair<string, Func<object>>[]
        {
            new(nameof(OrbType), () => Galaxy.OrbTypesStatic),
            new(nameof(Resource), () => Galaxy.ResourcesStatic),
            new(nameof(ComponentDefinition), () => Galaxy.ComponentsStatic),
            new(nameof(Race), () => Galaxy.RacesStatic),
            new(nameof(Artifact), () => Galaxy.ArtifactsStatic),
            new(nameof(PlanetaryFacilityDefinition), () => Galaxy.PlanetaryFacilitiesStatic),
            new(nameof(ColonyEventDefinition), () => Galaxy.ColonyEventsStatic),
            new(nameof(ResearchProjectDefinition), () => Galaxy.ResearchProjectsStatic),
            new(nameof(TroopDefinition), () => Galaxy.TroopDefinitionsStatic),
            new(nameof(CreatureType), () => Galaxy.CreatureTypesStatic),
            new(nameof(SpaceItemType), () => Galaxy.SpaceItemTypesStatic),
            new(nameof(Government), () => Galaxy.GovernmentTypesStatic),
            new(nameof(DesignTemplate), () => Galaxy.DesignTemplatesStatic),
            new(nameof(ShipHull), () => Galaxy.ShipHullsStatic),
            new(nameof(FleetTemplate), () => Galaxy.FleetTemplatesStatic),
            new(nameof(ArmyTemplate), () => Galaxy.ArmyTemplatesStatic),
            new(nameof(GameEvent), () => Galaxy.GameEventsStatic),
            new(nameof(LocationEffectGroupDefinition), () => Galaxy.LocationEffectGroupDefinitionsStatic),
            new(nameof(CharacterAnimation), () => Galaxy.CharacterAnimationsStatic),
            new(nameof(CharacterRoom), () => Galaxy.CharacterRoomsStatic)
        });

    private static readonly ImmutableDictionary<string, Func<Galaxy, object>> InstanceDefs = ImmutableDictionary.CreateRange(
        new KeyValuePair<string, Func<Galaxy, object>>[]
        {
            new(nameof(OrbType), g => g.OrbTypes),
            new(nameof(Resource), g => g.Resources),
            new(nameof(ComponentDefinition), g => g.Components),
            new(nameof(Race), g => g.Races),
            new(nameof(Artifact), g => g.Artifacts),
            new(nameof(PlanetaryFacilityDefinition), g => g.PlanetaryFacilities),
            new(nameof(ColonyEventDefinition), g => g.ColonyEvents),
            new(nameof(ResearchProjectDefinition), g => g.ResearchProjects),
            new(nameof(TroopDefinition), g => g.TroopDefinitions),
            new(nameof(CreatureType), g => g.CreatureTypes),
            new(nameof(Government), g => g.GovernmentTypes),
            new(nameof(DesignTemplate), g => g.DesignTemplates),
            new(nameof(ShipHull), g => g.ShipHulls),
            new(nameof(FleetTemplate), g => g.FleetTemplates),
            new(nameof(ArmyTemplate), g => g.ArmyTemplates),
            new(nameof(GameEvent), g => g.GameEvents),
            new(nameof(LocationEffectGroupDefinition), g => g.LocationEffectGroupDefinitions),
            new(nameof(CharacterAnimation), g => g.CharacterAnimations),
            new(nameof(CharacterRoom), g => g.CharacterRooms)
        });

    private static readonly ImmutableDictionary<string, Func<Galaxy, object>> GalaxyDefs = ImmutableDictionary.CreateRange(
        new KeyValuePair<string, Func<Galaxy, object>>[]
            { });

    private static readonly ImmutableDictionary<string, Func<Empire, object>> EmpireDefs = ImmutableDictionary.CreateRange(
        new KeyValuePair<string, Func<Empire, object>>[]
        {
            new(nameof(EmpirePolicy), e => e.Policy)
        });

    private static readonly ImmutableDictionary<string, string> DefIdFields = ImmutableDictionary.CreateRange(
        new KeyValuePair<string, string>[]
        {
            new(nameof(OrbType), nameof(OrbType.OrbTypeId)),
            new(nameof(Resource), nameof(Resource.ResourceId)),
            new(nameof(ComponentDefinition), nameof(ComponentDefinition.ComponentId)),
            new(nameof(Race), nameof(Race.RaceId)),
            new(nameof(Artifact), nameof(Artifact.ArtifactId)),
            new(nameof(PlanetaryFacilityDefinition), nameof(PlanetaryFacilityDefinition.PlanetaryFacilityDefinitionId)),
            new(nameof(ColonyEventDefinition), nameof(ColonyEventDefinition.ColonyEventDefinitionId)),
            new(nameof(ResearchProjectDefinition), nameof(ResearchProjectDefinition.ResearchProjectId)),
            new(nameof(TroopDefinition), nameof(TroopDefinition.TroopDefinitionId)),
            new(nameof(CreatureType), nameof(CreatureType.CreatureTypeId)),
            new(nameof(Government), nameof(Government.GovernmentId)),
            new(nameof(DesignTemplate), nameof(DesignTemplate.DesignTemplateId)),
            new(nameof(ShipHull), nameof(ShipHull.ShipHullId)),
            new(nameof(FleetTemplate), nameof(FleetTemplate.FleetTemplateId)),
            new(nameof(ArmyTemplate), nameof(ArmyTemplate.ArmyTemplateId)),
            new(nameof(GameEvent), nameof(GameEvent.Name)),
            new(nameof(LocationEffectGroupDefinition), nameof(LocationEffectGroupDefinition.LocationEffectGroupDefinitionId)),
            new(nameof(CharacterAnimation), nameof(CharacterAnimation.CharacterAnimationId)),
            new(nameof(CharacterRoom), nameof(CharacterRoom.RoomId)),
        });

    private static readonly MethodInfo MiGenericPatchIndexedDefinitions
        = typeof(GameDataDefinitionPatching).GetMethod(nameof(GenericPatchIndexedDefinitions))!;

    public static readonly MethodInfo MiGenericPatchDefinitions
        = typeof(GameDataDefinitionPatching).GetMethod(nameof(GenericPatchDefinitions))!;


    public static readonly MmVariableDsl Dsl = new();

    public static void ApplyContentPatches(string dataPath, Galaxy galaxy)
    {
        Dsl.Variables.Clear();
        Dsl.SetGlobal("loader", ModLoader.ModManager);
        Dsl.SetGlobal("game", ModLoader.ModManager.Game);
        Dsl.SetGlobal("galaxy", galaxy);

        var absPath = new Uri(Path.Combine(Environment.CurrentDirectory, dataPath)).LocalPath;
        foreach (var dataFilePath in Directory.EnumerateFiles(absPath, "*.yml", SearchOption.AllDirectories))
        {
            if (dataFilePath is null) continue;
            Console.WriteLine($"Parsing {dataFilePath} for instance definitions");
            using var s = File.Open(dataFilePath, FileMode.Open, FileAccess.Read);
            var ys = LoadYaml(s);
            foreach (var yd in ys)
            {
                Dsl.Variables.Clear();
                var yr = yd.RootNode;

                if (yr is YamlMappingNode ymr)
                    foreach (var kv in ymr)
                    {
                        var keyNode = kv.Key;
                        var dataNode = kv.Value;

                        if (keyNode is not YamlScalarNode keyScalar)
                        {
                            Console.Error.WriteLine($"Unsupported instruction @ {keyNode.Start}");
                            continue;
                        }

                        if (dataNode is not YamlSequenceNode valueSeq)
                        {
                            Console.Error.WriteLine($"Unsupported instruction @ {dataNode.Start}");
                            continue;
                        }

                        var typeStr = keyScalar.Value;

                        if (typeStr is null)
                        {
                            Console.Error.WriteLine($"Missing definition type @ {keyScalar.Start}");
                            continue;
                        }

                        if (!DefTypes.TryGetValue(typeStr, out var type))
                        {
                            Console.Error.WriteLine($"Unknown definition type {typeStr} @ {keyScalar.Start}");
                            continue;
                        }

                        if (StaticDefs.ContainsKey(typeStr))
                            continue;

                        if (GalaxyDefs.TryGetValue(typeStr, out var getGlxDef))
                        {
                            var def = getGlxDef(galaxy);
                            PatchDynamicDefinition(type, def, valueSeq);
                            continue;
                        }

                        if (EmpireDefs.TryGetValue(typeStr, out var getEmpDef))
                        {
                            foreach (var e in galaxy.Empires)
                            {
                                var def = getEmpDef(e);
                                Dsl["empire"] = e;
                                PatchDynamicDefinition(type, def, valueSeq);
                            }
                            continue;
                        }

                        Console.Error.WriteLine($"Can't find defs for {typeStr} @ {keyScalar.Start}");
                    }
                else
                    Console.Error.WriteLine($"Unsupported root node type @ {yr.Start}");
            }
        }
    }

    public static void ApplyContentPatches(string dataPath)
    {
        Dsl.Variables.Clear();
        Dsl.SetGlobal("loader", ModLoader.ModManager);
        Dsl.SetGlobal("game", ModLoader.ModManager.Game);

        var absPath = new Uri(Path.Combine(Environment.CurrentDirectory, dataPath)).LocalPath;
        foreach (var dataFilePath in Directory.EnumerateFiles(absPath, "*.yml", SearchOption.AllDirectories))
        {
            if (dataFilePath is null) continue;
            Console.WriteLine($"Parsing {dataFilePath} for static definitions");
            using var s = File.Open(dataFilePath, FileMode.Open, FileAccess.Read);
            var ys = LoadYaml(s);
            foreach (var yd in ys)
            {
                var yr = yd.RootNode;

                if (yr is YamlMappingNode ymr)
                    foreach (var kv in ymr)
                    {
                        var keyNode = kv.Key;
                        var dataNode = kv.Value;

                        if (keyNode is not YamlScalarNode keyScalar)
                        {
                            Console.Error.WriteLine($"Unsupported instruction @ {keyNode.Start}");
                            continue;
                        }

                        if (dataNode is not YamlSequenceNode valueSeq)
                        {
                            Console.Error.WriteLine($"Unsupported instruction @ {dataNode.Start}");
                            continue;
                        }

                        var typeStr = keyScalar.Value;

                        if (typeStr is null)
                        {
                            Console.Error.WriteLine($"Missing definition type @ {keyScalar.Start}");
                            continue;
                        }

                        if (!DefTypes.TryGetValue(typeStr, out var type))
                        {
                            Console.Error.WriteLine($"Unknown definition type {typeStr} @ {keyScalar.Start}");
                            continue;
                        }

                        if (StaticDefs.TryGetValue(typeStr, out var getStaticDefs))
                        {
                            DefIdFields.TryGetValue(typeStr, out var idFieldName);

                            var defs = getStaticDefs();

                            if (typeof(IndexedList<>).MakeGenericType(type).IsInstanceOfType(defs))
                                PatchIndexedDefinitions(type, defs, valueSeq, idFieldName);
                            else
                                PatchDefinitions(type, defs, valueSeq, idFieldName);
                            continue;
                        }

                        if (GalaxyDefs.ContainsKey(typeStr))
                            continue;

                        if (EmpireDefs.ContainsKey(typeStr))
                            continue;

                        Console.Error.WriteLine($"Can't find defs for {typeStr} @ {keyScalar.Start}");
                    }
                else
                    Console.Error.WriteLine($"Unsupported root node type @ {yr.Start}");
            }
        }
    }

    public static void PatchDynamicDefinition(Type type, object def, YamlSequenceNode mods)
    {
        Dsl["def"] = def;

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
                case "test" when mod is YamlSequenceNode tests: {

                    foreach (var testNode in tests)
                    {
                        if (testNode is not YamlScalarNode testScalar)
                        {
                            Console.Error.WriteLine($"Can't parse test @ {testNode.Start}");
                            continue;
                        }
                        var testStr = testScalar.Value;
                        if (testStr is null)
                        {
                            Console.Error.WriteLine($"Can't parse test @ {testScalar.Start}");
                            continue;
                        }

                        Dsl["def"] = null;
                        Dsl["value"] = null;
                        Func<object> testFn;
                        try
                        {
                            testFn = Dsl.Parse(testStr).Compile(true);
                        }
                        catch
                        {
                            Console.Error.WriteLine($"Can't parse test @ {testScalar.Start}");
                            continue;
                        }
                        bool pass;
                        try
                        {
                            pass = ((IConvertible)testFn()).ToBoolean(NumberFormatInfo.InvariantInfo);
                        }
                        catch
                        {
                            Console.Error.WriteLine($"Can't parse test @ {testScalar.Start}");
                            continue;
                        }

                        if (pass) continue;

                        Console.Error.WriteLine($"Test failed to pass, skipping document @ {testScalar.Start}");
                        return;
                    }

                    break;
                }

                case "test":
                    Console.Error.WriteLine($"Can't parse test instruction @ {mod.Start}");
                    break;

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
                            if (ModLoader.ModManager.SharedVariables.TryRemove(keyStr, out _))
                                continue;

                            Console.Error.WriteLine($"Failed to remove {keyStr} from state @ {valScalar.Start}");
                            continue;
                        }

                        try
                        {
                            Dsl["value"] = null;
                            ModLoader.ModManager.SharedVariables.AddOrUpdate(keyStr,
                                _ => Dsl.Parse(valStr).Compile(true)(),
                                (_, old) => {
                                    Dsl["value"] = old;
                                    return Dsl.Parse(valStr).Compile(true)();
                                });
                        }
                        catch
                        {

                            Console.Error.WriteLine($"Failed to manipulate state of {keyStr} @ {valScalar.Start}");
                        }
                    }

                    break;
                }

                case "state":
                    Console.Error.WriteLine($"Can't parse state manipulation instruction @ {mod.Start}");
                    break;

                case "update" when mod is YamlMappingNode item: {

                    Dsl["value"] = null;

                    var whereKv = item.FirstOrDefault(kv => kv.Key is YamlScalarNode { Value: "$where" });

                    if (whereKv.Value is not YamlScalarNode whereNode)
                    {
                        Console.Error.WriteLine($"Can't parse update instruction @ {item.Start}");
                        break;
                    }

                    item.Children.Remove(whereKv);
                    var whereStr = whereNode.Value;

                    if (whereStr is null)
                    {
                        Console.Error.WriteLine($"Can't parse update where clause @ {whereNode.Start}");
                        break;
                    }

                    Func<object> whereFn;
                    try
                    {
                        whereFn = Dsl.Parse(whereStr).Compile(true);
                    }
                    catch
                    {
                        Console.Error.WriteLine($"Can't parse update where clause @ {whereNode.Start}");
                        break;
                    }

                    bool pass;

                    try
                    {
                        pass = ((IConvertible)whereFn()).ToBoolean(NumberFormatInfo.InvariantInfo);
                    }
                    catch
                    {
                        Console.Error.WriteLine($"Can't parse update where clause @ {whereNode.Start}");
                        break;
                    }

                    if (!pass)
                        continue;

                    ProcessObjectUpdate(type, def, item,
                        (_, expr) => Dsl.Parse(expr).Compile(true));

                    Console.WriteLine($"Updated {type.Name} where {whereStr}");

                    break;
                }

                case "update":
                    Console.Error.WriteLine($"Can't parse update instruction @ {mod.Start}");
                    break;
            }
        }
    }

    public static void PatchDefinitions(Type type, object defs, YamlSequenceNode mods, string? idFieldName = null)
    {
        var m = MiGenericPatchDefinitions.MakeGenericMethod(type);
        m.Invoke(null, new[] { defs, mods, idFieldName });
    }
    public static void GenericPatchDefinitions<T>(List<T> defs, YamlSequenceNode mods, string? idFieldName = null) where T : class
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

        idField ??= (MemberInfo?)fieldInfos.FirstOrDefault(f => f.Name.EndsWith("Id"))
            ?? propInfos.FirstOrDefault(p => p.Name.EndsWith("Id"));

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
                idFieldAsFieldInfo!.SetValue(item, ((IConvertible)id).ToType(idFieldAsFieldInfo.FieldType, NumberFormatInfo.InvariantInfo));
            else
                idFieldAsPropInfo!.SetValue(item, ((IConvertible)id).ToType(idFieldAsPropInfo.PropertyType, NumberFormatInfo.InvariantInfo));
        }

        object ConvertToIdType(object id)
            => ((IConvertible)id).ToType(idFieldType!, NumberFormatInfo.InvariantInfo);

        int ConvertToInt(object id)
            => ((IConvertible)id).ToInt32(NumberFormatInfo.InvariantInfo);

        idFieldName ??= idField.Name;

        var idFieldNamePrefixed = "$" + idFieldName;

        foreach (var instrModNode in mods)
        {
            Dsl.Variables.Clear();

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
                            if (ModLoader.ModManager.SharedVariables.TryRemove(keyStr, out _))
                                continue;

                            Console.Error.WriteLine($"Failed to remove {keyStr} from state @ {valScalar.Start}");
                            continue;
                        }

                        try
                        {
                            Dsl["value"] = null;
                            ModLoader.ModManager.SharedVariables.AddOrUpdate(keyStr,
                                _ => Dsl.Parse(valStr).Compile(true)(),
                                (_, old) => {
                                    Dsl["value"] = old;
                                    return Dsl.Parse(valStr).Compile(true)();
                                });
                        }
                        catch
                        {

                            Console.Error.WriteLine($"Failed to manipulate state of {keyStr} @ {valScalar.Start}");
                        }
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

                        var idVal = ((IConvertible)idStr).ToType(idFieldType, NumberFormatInfo.InvariantInfo);

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

                    var idLookupReq = item.FirstOrDefault(kv => kv.Key is YamlScalarNode sk && sk.Value == idFieldNamePrefixed);
                    // don't issue error on explicit id set
                    if (idLookupReq.Key == default && !item.Any(kv => kv.Key is YamlScalarNode sk && sk.Value == idFieldName))
                    {
                        Console.Error.WriteLine($"Failed to parse {type.Name} @ {item.Start}");
                        break;
                    }

                    var def = Prepopulate(Activator.CreateInstance<T>());

                    if (idLookupReq.Value is YamlScalarNode idLookupVarNode)
                    {
                        var idLookupVar = idLookupVarNode.Value;
                        var value = ModLoader.ModManager.SharedVariables.GetOrAdd(idLookupVar!, _ => GetRealNextId(defs));
                        SetId(def, ConvertToIdType(value));
                        item.Children.Remove(idLookupReq);
                    }

                    Dsl["value"] = null;
                    Dsl["def"] = def;

                    try
                    {
                        ProcessObjectUpdate(type, def, item,
                            (_, expr) => Dsl.Parse(expr).Compile(true));
                    }
                    catch (Exception ex)
                    {
                        ModLoader.ModManager.OnUnhandledException(ExceptionDispatchInfo.Capture(ex));
                        Console.Error.WriteLine($"Failed to parse {type.Name} @ {item.Start}");
                        break;
                    }
                    defs.Add(def);
                    break;
                }

                case "add":
                    Console.Error.WriteLine($"Can't parse add instruction @ {mod.Start}");
                    break;

                case "update" when mod is YamlMappingNode item: {
                    object idObj;
                    // don't issue error on explicit id set
                    var idLookupReq = item.FirstOrDefault(kv => kv.Key is YamlScalarNode sk && sk.Value == idFieldNamePrefixed);

                    if (idLookupReq.Value is YamlScalarNode idLookupVarNode)
                    {
                        var idLookupVar = idLookupVarNode.Value;
                        var value = ModLoader.ModManager.SharedVariables.GetOrAdd(idLookupVar!, _ => GetRealNextId(defs));

                        item.Children.Remove(idLookupReq);

                        idObj = ConvertToIdType(value);
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

                        Dsl["value"] = null;
                        idObj = ((IConvertible)Dsl.Parse(idStr).Compile(true)()).ToInt32(NumberFormatInfo.InvariantInfo);

                        item.Children.Remove(idKvNode);

                    }

                    if (idObj is sbyte or short or int
                        or byte or ushort)
                    {
                        var id = ConvertToInt(idObj);

                        var def = defs.Count > id ? defs[id] : null;

                        if (def == null || !id.Equals(ConvertToInt(GetId(def))))
                            def = defs.First(x => id.Equals(ConvertToInt(GetId(x))));

                        Dsl["value"] = null;
                        Dsl["def"] = def;

                        ProcessObjectUpdate(type, def, item,
                            (_, expr) => Dsl.Parse(expr).Compile(true));

                        Console.WriteLine($"Updated {type.Name} {id}");
                        break;
                    }

                    Console.Error.WriteLine($"Non-integer update is not implemented @ {item.Start}");
                    break;
                }

                case "update":
                    Console.Error.WriteLine($"Can't parse update instruction @ {mod.Start}");
                    break;

                case "update-all" when mod is YamlMappingNode item: {
                    var whereKv = item.FirstOrDefault(kv => kv.Key is YamlScalarNode { Value: "$where" });

                    if (whereKv.Value is not YamlScalarNode whereNode)
                    {
                        Console.Error.WriteLine($"Can't parse update-all instruction @ {item.Start}");
                        break;
                    }
                    item.Children.Remove(whereKv);
                    var whereStr = whereNode.Value;

                    if (whereStr is null)
                    {
                        Console.Error.WriteLine($"Can't parse update-all where clause @ {whereNode.Start}");
                        break;
                    }

                    foreach (var def in defs)
                    {
                        var idObj = GetId(def);

                        var idVal = ((IConvertible)idObj).ToDouble(NumberFormatInfo.InvariantInfo);

                        Dsl["value"] = idVal;
                        Dsl["def"] = def;

                        Func<object> whereFn;
                        try
                        {
                            whereFn = Dsl.Parse(whereStr).Compile(true);
                        }
                        catch
                        {
                            Console.Error.WriteLine($"Can't parse update-all where clause @ {whereNode.Start}");
                            break;
                        }

                        bool pass;

                        try
                        {
                            pass = ((IConvertible)whereFn()).ToBoolean(NumberFormatInfo.InvariantInfo);
                        }
                        catch
                        {
                            Console.Error.WriteLine($"Can't parse update-all where clause @ {whereNode.Start}");
                            break;
                        }

                        if (!pass)
                            continue;

                        ProcessObjectUpdate(type, def, item,
                            (_, expr) => Dsl.Parse(expr).Compile(true));

                        Console.WriteLine($"Updated {type.Name} {idVal}");
                    }

                    break;
                }

                case "update-all":
                    Console.Error.WriteLine($"Can't parse update-all instruction @ {mod.Start}");
                    break;
            }
        }
    }
    private static object Prepopulate(object obj, Type? objType = null)
    {
        if (obj is null) throw new ArgumentNullException(nameof(obj));

        objType ??= obj.GetType();
        if (!objType.IsClass || objType == typeof(string)) return obj;
        var fieldOrProps = objType
            .GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
            .Where(m => m is FieldInfo { IsStatic: false } or PropertyInfo { GetMethod.IsStatic: false });

        foreach (var fieldOrProp in fieldOrProps)
        {
            var fieldOrPropType = GetFieldOrPropertyType(fieldOrProp);
            if (!fieldOrPropType.IsClass) continue;
            if (GetValue(obj, fieldOrProp) is null)
                SetValue(obj, fieldOrProp, Prepopulate(Activator.CreateInstance(fieldOrPropType), fieldOrPropType));
        }

        return obj;
    }

    private static T Prepopulate<T>(T obj) where T : class
        => (T)Prepopulate(obj, typeof(T));


    public static void PatchIndexedDefinitions(Type type, object defs, YamlSequenceNode mods, string? idFieldName = null)
    {
        var m = MiGenericPatchIndexedDefinitions.MakeGenericMethod(type);
        m.Invoke(null, new[] { defs, mods, idFieldName });
    }
    public static void GenericPatchIndexedDefinitions<T>(IndexedList<T> defs, YamlSequenceNode mods, string? idFieldName = null) where T : class
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

        idField ??= (MemberInfo?)fieldInfos.FirstOrDefault(f => f.Name.EndsWith("Id"))
            ?? propInfos.FirstOrDefault(p => p.Name.EndsWith("Id"));

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
                idFieldAsFieldInfo!.SetValue(item, ((IConvertible)id).ToType(idFieldAsFieldInfo.FieldType, NumberFormatInfo.InvariantInfo));
            else
                idFieldAsPropInfo!.SetValue(item, ((IConvertible)id).ToType(idFieldAsPropInfo.PropertyType, NumberFormatInfo.InvariantInfo));
        }

        object ConvertToIdType(object id)
            => ((IConvertible)id).ToType(idFieldType!, NumberFormatInfo.InvariantInfo);

        int ConvertToInt(object id)
            => ((IConvertible)id).ToInt32(NumberFormatInfo.InvariantInfo);

        idFieldName ??= idField.Name;

        var idFieldNamePrefixed = "$" + idFieldName;

        foreach (var instrModNode in mods)
        {
            Dsl.Variables.Clear();

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
                            if (ModLoader.ModManager.SharedVariables.TryRemove(keyStr, out _))
                                continue;

                            Console.Error.WriteLine($"Failed to remove {keyStr} from state @ {valScalar.Start}");
                            continue;
                        }

                        Dsl["value"] = null;
                        ModLoader.ModManager.SharedVariables.AddOrUpdate(keyStr,
                            _ => Dsl.Parse(valStr).Compile(true)(),
                            (_, old) => {
                                Dsl["value"] = old;
                                return Dsl.Parse(valStr).Compile(true)();
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

                        if (!int.TryParse(value, NumberStyles.Number, NumberFormatInfo.InvariantInfo, out var id))
                        {
                            // check if its a shared variable
                            if (!ModLoader.ModManager.SharedVariables.TryGetValue(value!, out var varValue))
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

                    var def = Prepopulate(Activator.CreateInstance<T>());

                    if (idLookupReq.Value is YamlScalarNode idLookupVarNode)
                    {
                        var idLookupVar = idLookupVarNode.Value;
                        var value = ModLoader.ModManager.SharedVariables.GetOrAdd(idLookupVar!, _ => GetRealNextId(defs));
                        SetId(def, ConvertToIdType(value));
                        item.Children.Remove(idLookupReq);
                    }

                    Dsl["value"] = null;
                    Dsl["def"] = def;

                    try
                    {
                        ProcessObjectUpdate(type, def, item,
                            (_, expr) => Dsl.Parse(expr).Compile(true));
                    }
                    catch (Exception ex)
                    {
                        ModLoader.ModManager.OnUnhandledException(ExceptionDispatchInfo.Capture(ex));
                        Console.Error.WriteLine($"Failed to parse {type.Name} @ {item.Start}");
                        break;
                    }

                    var id = ConvertToInt(GetId(def));
                    var contained = defs.ContainsIdThreadSafe(id);
                    if (contained && defs[id] != null)
                    {
                        Console.Error.WriteLine($"Failed to add {type.Name} @ {item.Start}; {id} already defined");
                        break;
                    }
                    if (contained)
                        defs[id] = def;
                    else
                        defs.Add(def);
                    if (!defs[id]?.Equals(def) ?? false)
                    {
                        Console.Error.WriteLine($"Failed to validate after adding {type.Name} @ {item.Start}; {id} not found");
                        break;
                    }
                    //defs.RebuildIndexes();
                    break;
                }

                case "add":
                    Console.Error.WriteLine($"Can't parse add instruction @ {mod.Start}");
                    break;

                case "update" when mod is YamlMappingNode item: {
                    int id;
                    // don't issue error on explicit id set
                    var idLookupReq = item.FirstOrDefault(kv => kv.Key is YamlScalarNode sk && sk.Value == idFieldNamePrefixed);

                    if (idLookupReq.Value is YamlScalarNode idLookupVarNode)
                    {
                        var idLookupVar = idLookupVarNode.Value;
                        var value = ModLoader.ModManager.SharedVariables.GetOrAdd(idLookupVar!, _ => GetRealNextId(defs));

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

                        id = ((IConvertible)Dsl.Parse(idStr).Compile(true)()).ToInt32(NumberFormatInfo.InvariantInfo);

                        item.Children.Remove(idKvNode);

                    }

                    var index = defs.GetIndex(id);

                    var def = defs.Count > index ? defs[index] : defs.Count > id ? defs[id] : null;

                    if (def == null || !id.Equals(ConvertToInt(GetId(def))))
                        def = defs.First(x => id.Equals(ConvertToInt(GetId(x))));

                    Dsl["value"] = null;
                    Dsl["def"] = def;

                    ProcessObjectUpdate(type, def, item,
                        (_, expr) => Dsl.Parse(expr).Compile(true));

                    Console.WriteLine($"Updated {type.Name} {id}");

                    break;
                }

                case "update":
                    Console.Error.WriteLine($"Can't parse update instruction @ {mod.Start}");
                    break;

                case "update-all" when mod is YamlMappingNode item: {
                    var whereKv = item.FirstOrDefault(kv => kv.Key is YamlScalarNode { Value: "$where" });

                    if (whereKv.Value is not YamlScalarNode whereNode)
                    {
                        Console.Error.WriteLine($"Can't parse update-all instruction @ {item.Start}");
                        break;
                    }
                    item.Children.Remove(whereKv);
                    var whereStr = whereNode.Value;

                    if (whereStr is null)
                    {
                        Console.Error.WriteLine($"Can't parse update-all where clause @ {whereNode.Start}");
                        break;
                    }

                    Func<object> whereFn;
                    try
                    {
                        whereFn = Dsl.Parse(whereStr).Compile(true);
                    }
                    catch
                    {
                        Console.Error.WriteLine($"Can't parse update-all where clause @ {whereNode.Start}");
                        break;
                    }

                    foreach (var def in defs)
                    {
                        var idObj = GetId(def);

                        var idVal = ((IConvertible)idObj).ToDouble(NumberFormatInfo.InvariantInfo);

                        Dsl["value"] = idVal;
                        Dsl["def"] = def;

                        bool pass;

                        try
                        {
                            pass = ((IConvertible)whereFn()).ToBoolean(NumberFormatInfo.InvariantInfo);
                        }
                        catch
                        {
                            Console.Error.WriteLine($"Can't parse update-all where clause @ {whereNode.Start}");
                            break;
                        }

                        if (!pass) continue;

                        ProcessObjectUpdate(type, def, item,
                            (_, expr) => Dsl.Parse(expr).Compile(true));

                        Console.WriteLine($"Updated {type.Name} {idVal}");
                    }

                    break;
                }

                case "update-all":
                    Console.Error.WriteLine($"Can't parse update-all instruction @ {mod.Start}");
                    break;
            }
        }
    }
    private static object ProcessObjectUpdate(Type type, object obj, YamlMappingNode item, Func<object, string, Func<object>> compileFn)
    {
        foreach (var kv in item)
        {
            if (kv.Key is null)
            {
                Console.Error.WriteLine("Warning: null key in mapping?!");
                continue;
            }
            if (kv.Key is not YamlScalarNode key)
                throw new NotSupportedException(kv.Key.Start.ToString());

            if (key.Value is null)
                throw new NotSupportedException(key.Start.ToString());

            var name = key.Value;

            var isFormula = false;
            if (name[0] == '$')
            {
                name = name.Substring(1);
                isFormula = true;
            }

            var member = GetInstancePropertyOrField(type, name);

            if (member is null)
                throw new NotSupportedException(key.Start.ToString());

            var valType = GameDataUtils.GetType(member);

            var initValue = GetValue(obj, member);

            var valNode = kv.Value;

            if (typeof(IList).IsAssignableFrom(valType))
            {
                initValue ??= CreateInstance(valType);
                switch (valNode)
                {
                    case YamlScalarNode scalar: {
                        if (scalar.Value == "delete()")
                            SetValue(obj, member, null);
                        else
                            throw new NotSupportedException(valNode.ToString());
                        break;
                    }
                    case YamlSequenceNode seq: {
                        ParseCollectionUpdate(valType, (IList)initValue, seq, compileFn);
                        break;
                    }
                    case YamlMappingNode map: {
                        ParseCollectionUpdate(valType, (IList)initValue, map, compileFn);
                        break;
                    }
                }
            }
            else if (
                valType.IsPrimitive
                && Type.GetTypeCode(valType) is not
                    (TypeCode.Char
                    or TypeCode.Decimal
                    or TypeCode.DateTime)
            )
                switch (valNode)
                {
                    case YamlScalarNode scalar: {
                        IConvertible newValue;
                        var valStr = scalar.Value!;
                        try
                        {
                            Dsl["value"] = initValue;
                            var fn = compileFn(member, valStr);
                            newValue = (IConvertible)fn();
                        }
                        catch
                        {
                            newValue = valStr;
                        }
                        SetValue(obj, member, newValue.ToType(valType, NumberFormatInfo.InvariantInfo));
                        break;
                    }
                    case YamlMappingNode map: {
                        ProcessObjectUpdate(valType, initValue, map, compileFn);
                        break;
                    }
                    default:
                        throw new NotSupportedException(valNode.Start.ToString());
                }
            else if (valType == typeof(string))
                switch (valNode)
                {
                    case YamlScalarNode scalar: {
                        var valStr = scalar.Value!;
                        if (isFormula)
                        {
                            IConvertible newValue;
                            try
                            {
                                Dsl["value"] = initValue;
                                var fn = compileFn(member, valStr);
                                newValue = (IConvertible)fn();
                            }
                            catch
                            {
                                newValue = valStr;
                            }
                            SetValue(obj, member, newValue.ToType(valType, NumberFormatInfo.InvariantInfo));
                        }
                        else
                            SetValue(obj, member, valStr);
                        break;
                    }
                    default:
                        throw new NotSupportedException(valNode.Start.ToString());
                }
            else if (valType.IsEnum)
                switch (valNode)
                {
                    case YamlScalarNode scalar: {
                        var valStr = scalar.Value!;
                        if (isFormula)
                        {
                            IConvertible newValue;
                            try
                            {
                                Dsl["value"] = Enum.GetName(valType, initValue!);
                                var fn = compileFn(member, valStr);
                                newValue = (IConvertible)fn();
                            }
                            catch
                            {
                                newValue = valStr;
                            }
                            SetValue(obj, member,
                                Enum.Parse(valType, newValue.ToString(NumberFormatInfo.InvariantInfo),
                                    true));
                        }
                        else
                            SetValue(obj, member, Enum.Parse(valType, valStr, true));
                        break;
                    }
                    default:
                        throw new NotSupportedException(valNode.Start.ToString());
                }
            else if (valType.IsClass)
            {
                initValue ??= Prepopulate(CreateInstance(valType));
                switch (valNode)
                {
                    case YamlMappingNode map: {
                        ProcessObjectUpdate(valType, initValue, map, compileFn);
                        break;
                    }
                    default:
                        throw new NotSupportedException(valNode.Start.ToString());
                }
            }
            else if (valType.IsValueType)
            {
                initValue ??= CreateInstance(valType);
                switch (valNode)
                {
                    case YamlMappingNode map: {
                        ProcessObjectUpdate(valType, initValue, map, compileFn);
                        break;
                    }
                    default:
                        throw new NotSupportedException(valNode.Start.ToString());
                }
            }
            else
                Console.WriteLine($"Warning, field with unsupported type {valType.FullName} @ {valNode.Start}");
        }
        return obj;
    }
    private static void ParseCollectionUpdate(Type collectionType, IList collection, YamlSequenceNode seq,
        Func<object, string, Func<object>> compileFn)
    {
        var itemType = collectionType.GetInterfaces()
            .First(t => t.IsGenericType && typeof(IList<>) == t.GetGenericTypeDefinition())
            .GetGenericArguments()[0];

        var index = -1;
        foreach (var valNode in seq)
        {
            ++index;

            if (index < collection.Count)
                collection[index] = ProcessCollectionItemUpdate(valNode, itemType, collection[index], compileFn);
            else
                collection.Add(ProcessCollectionItemUpdate(valNode, itemType,
                    CreateInstance(itemType), compileFn));
        }
    }
    private static object CreateInstance(Type itemType)
        => itemType == typeof(string) ? "" : Activator.CreateInstance(itemType);

    private static void ParseCollectionUpdate(Type collectionType, IList collection, YamlMappingNode item,
        Func<object, string, Func<object>> compileFn)
    {
        var itemType = collectionType.GetInterfaces()
            .First(t => t.IsGenericType && typeof(IList<>) == t.GetGenericTypeDefinition())
            .GetGenericArguments()[0];

        foreach (var kv in item)
        {
            if (kv.Key is not YamlScalarNode key)
                throw new NotSupportedException(kv.Key.Start.ToString());

            var keyStr = key.Value;

            if (keyStr is null)
                throw new NotSupportedException(key.Start.ToString());

            if (!int.TryParse(keyStr, NumberStyles.Number, NumberFormatInfo.InvariantInfo, out var idVal))
            {
                try
                {
                    Dsl["value"] = null;
                    var result = compileFn("", keyStr)(); // no state, always cache
                    idVal = ((IConvertible)result).ToInt32(NumberFormatInfo.InvariantInfo);
                }
                catch
                {
                    throw new NotSupportedException(key.Start.ToString());
                }
            }

            object initValue;
            if (collection.Count > idVal)
            {
                initValue = collection[idVal];

            }
            else
            {
                initValue = Prepopulate(CreateInstance(itemType));
            }
            var valNode = kv.Value;

            ProcessCollectionItemUpdate(valNode, itemType, initValue, compileFn);
        }
    }
    private static object? ProcessCollectionItemUpdate(YamlNode valNode, Type itemType, object initValue,
        Func<object, string, Func<object>> compileFn)
    {
        if (
            itemType.IsPrimitive
            && Type.GetTypeCode(itemType) is not
                (TypeCode.Char
                or TypeCode.Decimal
                or TypeCode.DateTime)
            || itemType == typeof(string)
        )
            switch (valNode)
            {
                case YamlScalarNode scalar: {
                    IConvertible newValue;
                    var valStr = scalar.Value!;
                    try
                    {
                        Dsl["value"] = initValue;
                        var fn = Dsl.Parse(valStr).Compile(true);
                        newValue = (IConvertible)fn();
                    }
                    catch
                    {
                        newValue = valStr;
                    }
                    return newValue.ToType(itemType, NumberFormatInfo.InvariantInfo);
                }
                case YamlSequenceNode seq: {
                    throw new NotSupportedException(valNode.Start.ToString());
                }
                case YamlMappingNode map: {
                    if (map.Children.Count == 1)
                    {
                        var firstKv = map.First();
                        if (firstKv.Key is YamlScalarNode typeNode)
                        {
                            if (typeNode.Value == itemType.Name)
                            {
                                if (firstKv.Value is YamlMappingNode subMap)
                                    return ProcessObjectUpdate(itemType, initValue, subMap, compileFn);
                                throw new NotImplementedException(firstKv.Value.Start.ToString());
                            }

                            // TODO: handle descendent types
                            throw new NotImplementedException(firstKv.Value.Start.ToString());
                        }
                        throw new NotImplementedException(firstKv.Key.Start.ToString());
                    }
                    return ProcessObjectUpdate(itemType, initValue, map, compileFn);
                }
            }
        else
            switch (valNode)
            {
                case YamlScalarNode scalar: {
                    if (scalar.Value == "delete()")
                        return itemType.IsClass ? null : Activator.CreateInstance(itemType);
                    throw new NotSupportedException(valNode.ToString());
                }
                case YamlSequenceNode seq: {
                    if (initValue is not IList list)
                        throw new NotImplementedException(valNode.Start.ToString());
                    ParseCollectionUpdate(itemType, list, seq, compileFn);
                    return list;
                }
                case YamlMappingNode map: {
                    if (initValue is not IList list)
                        return ProcessObjectUpdate(itemType, initValue, map, compileFn);
                    ParseCollectionUpdate(itemType, list, map, compileFn);
                    return list;
                }
            }

        throw new NotImplementedException(valNode.Start.ToString());
    }
}
