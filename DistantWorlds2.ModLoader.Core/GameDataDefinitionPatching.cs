using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Text.RegularExpressions;
using DistantWorlds.Types;
using FastExpressionCompiler.LightExpression;
using Xenko.Core.IO;
using YamlDotNet.RepresentationModel;
using Sys = System.Linq.Expressions;

namespace DistantWorlds2.ModLoader;

using static GameDataUtils;

public static class GameDataDefinitionPatching
{
    private static readonly ImmutableSortedDictionary<string, Type> DefTypes
        = ImmutableSortedDictionary.CreateRange(new KeyValuePair<string, Type>[]
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

    private static readonly ImmutableSortedDictionary<string, StaticDefFieldInfo>
        StaticDefs = ImmutableSortedDictionary.CreateRange(
        new KeyValuePair<string, StaticDefFieldInfo>[]
        {
            new(nameof(OrbType), new(nameof(Galaxy.OrbTypesStatic), () => Galaxy.OrbTypesStatic, v => Galaxy.OrbTypesStatic = (OrbTypeList)v)),
            new(nameof(Resource), new(nameof(Galaxy.ResourcesStatic), () => Galaxy.ResourcesStatic, v => Galaxy.ResourcesStatic = (ResourceList)v)),
            new(nameof(ComponentDefinition), new(nameof(Galaxy.ComponentsStatic), () => Galaxy.ComponentsStatic, v => Galaxy.ComponentsStatic = (ComponentDefinitionList)v)),
            new(nameof(Race), new(nameof(Galaxy.RacesStatic), () => Galaxy.RacesStatic, v => Galaxy.RacesStatic = (RaceList)v)),
            new(nameof(Artifact), new(nameof(Galaxy.ArtifactsStatic), () => Galaxy.ArtifactsStatic, v => Galaxy.ArtifactsStatic = (ArtifactList)v)),
            new(nameof(PlanetaryFacilityDefinition), new(nameof(Galaxy.PlanetaryFacilitiesStatic), () => Galaxy.PlanetaryFacilitiesStatic, v => Galaxy.PlanetaryFacilitiesStatic = (PlanetaryFacilityDefinitionList)v)),
            new(nameof(ColonyEventDefinition), new(nameof(Galaxy.ColonyEventsStatic), () => Galaxy.ColonyEventsStatic, v => Galaxy.ColonyEventsStatic = (ColonyEventDefinitionList)v)),
            new(nameof(ResearchProjectDefinition), new(nameof(Galaxy.ResearchProjectsStatic), () => Galaxy.ResearchProjectsStatic, v => Galaxy.ResearchProjectsStatic = (ResearchProjectDefinitionList)v)),
            new(nameof(TroopDefinition), new(nameof(Galaxy.TroopDefinitionsStatic), () => Galaxy.TroopDefinitionsStatic, v => Galaxy.TroopDefinitionsStatic = (TroopDefinitionList)v)),
            new(nameof(CreatureType), new(nameof(Galaxy.CreatureTypesStatic), () => Galaxy.CreatureTypesStatic, v => Galaxy.CreatureTypesStatic = (CreatureTypeList)v)),
            new(nameof(Government), new(nameof(Galaxy.GovernmentTypesStatic), () => Galaxy.GovernmentTypesStatic, v => Galaxy.GovernmentTypesStatic = (GovernmentList)v)),
            new(nameof(DesignTemplate), new(nameof(Galaxy.DesignTemplatesStatic), () => Galaxy.DesignTemplatesStatic, v => Galaxy.DesignTemplatesStatic = (DesignTemplateList)v)),
            new(nameof(ShipHull), new(nameof(Galaxy.ShipHullsStatic), () => Galaxy.ShipHullsStatic, v => Galaxy.ShipHullsStatic = (ShipHullList)v)),
            new(nameof(FleetTemplate), new(nameof(Galaxy.FleetTemplatesStatic), () => Galaxy.FleetTemplatesStatic, v => Galaxy.FleetTemplatesStatic = (FleetTemplateList)v)),
            new(nameof(ArmyTemplate), new(nameof(Galaxy.ArmyTemplatesStatic), () => Galaxy.ArmyTemplatesStatic, v => Galaxy.ArmyTemplatesStatic = (ArmyTemplateList)v)),
            new(nameof(GameEvent), new(nameof(Galaxy.GameEventsStatic), () => Galaxy.GameEventsStatic, v => Galaxy.GameEventsStatic = (GameEventList)v)),
            new(nameof(LocationEffectGroupDefinition), new(nameof(Galaxy.LocationEffectGroupDefinitionsStatic), () => Galaxy.LocationEffectGroupDefinitionsStatic, v => Galaxy.LocationEffectGroupDefinitionsStatic = (LocationEffectGroupDefinitionList)v)),
            new(nameof(CharacterAnimation), new(nameof(Galaxy.CharacterAnimationsStatic), () => Galaxy.CharacterAnimationsStatic, v => Galaxy.CharacterAnimationsStatic = (CharacterAnimationList)v)),
            new(nameof(CharacterRoom), new(nameof(Galaxy.CharacterRoomsStatic), () => Galaxy.CharacterRoomsStatic, v => Galaxy.CharacterRoomsStatic = (CharacterRoomList)v))
        });
    

    private static readonly ImmutableSortedDictionary<string, StaticDefFieldInfo> LateStaticDefs
        = ImmutableSortedDictionary.CreateRange(
        new KeyValuePair<string, StaticDefFieldInfo>[]
        {
            new(nameof(Race), new(nameof(Galaxy.RacesStatic),() => Galaxy.RacesStatic,v => Galaxy.RacesStatic = (RaceList)v)),
            new(nameof(Artifact), new(nameof(Galaxy.ArtifactsStatic),() => Galaxy.ArtifactsStatic,v => Galaxy.ArtifactsStatic = (ArtifactList)v)),
            new(nameof(GameEvent), new(nameof(Galaxy.GameEventsStatic),() => Galaxy.GameEventsStatic,v => Galaxy.GameEventsStatic = (GameEventList)v))
        });

    private static readonly ImmutableSortedDictionary<string, InstanceDefFieldInfo<Galaxy>> InstanceDefs
        = ImmutableSortedDictionary.CreateRange(
        new KeyValuePair<string, InstanceDefFieldInfo<Galaxy>>[]
        {
            new(nameof(OrbType), new(nameof(Galaxy.OrbTypes),g => g.OrbTypes, (g,v) => g.OrbTypes = (OrbTypeList)v)),
            new(nameof(Resource), new(nameof(Galaxy.OrbTypes),g => g.Resources, (g,v) => g.Resources = (ResourceList)v)),
            new(nameof(ComponentDefinition), new(nameof(Galaxy.OrbTypes),g => g.Components, (g,v) => g.Components = (ComponentDefinitionList)v)),
            new(nameof(Race), new(nameof(Galaxy.OrbTypes),g => g.Races, (g,v) => g.Races = (RaceList)v)),
            new(nameof(Artifact), new(nameof(Galaxy.OrbTypes),g => g.Artifacts, (g,v) => g.Artifacts = (ArtifactList)v)),
            new(nameof(PlanetaryFacilityDefinition), new(nameof(Galaxy.OrbTypes),g => g.PlanetaryFacilities, (g,v) => g.PlanetaryFacilities = (PlanetaryFacilityDefinitionList)v)),
            new(nameof(ColonyEventDefinition), new(nameof(Galaxy.OrbTypes),g => g.ColonyEvents, (g,v) => g.ColonyEvents = (ColonyEventDefinitionList)v)),
            new(nameof(ResearchProjectDefinition), new(nameof(Galaxy.OrbTypes),g => g.ResearchProjects, (g,v) => g.ResearchProjects = (ResearchProjectDefinitionList)v)),
            new(nameof(TroopDefinition), new(nameof(Galaxy.OrbTypes),g => g.TroopDefinitions, (g,v) => g.TroopDefinitions = (TroopDefinitionList)v)),
            new(nameof(CreatureType), new(nameof(Galaxy.OrbTypes),g => g.CreatureTypes, (g,v) => g.CreatureTypes = (CreatureTypeList)v)),
            new(nameof(Government), new(nameof(Galaxy.OrbTypes),g => g.GovernmentTypes, (g,v) => g.GovernmentTypes = (GovernmentList)v)),
            new(nameof(DesignTemplate), new(nameof(Galaxy.OrbTypes),g => g.DesignTemplates, (g,v) => g.DesignTemplates = (DesignTemplateList)v)),
            new(nameof(ShipHull), new(nameof(Galaxy.OrbTypes),g => g.ShipHulls, (g,v) => g.ShipHulls = (ShipHullList)v)),
            new(nameof(FleetTemplate), new(nameof(Galaxy.OrbTypes),g => g.FleetTemplates, (g,v) => g.FleetTemplates = (FleetTemplateList)v)),
            new(nameof(ArmyTemplate), new(nameof(Galaxy.OrbTypes),g => g.ArmyTemplates, (g,v) => g.ArmyTemplates = (ArmyTemplateList)v)),
            new(nameof(GameEvent), new(nameof(Galaxy.OrbTypes),g => g.GameEvents, (g,v) => g.GameEvents = (GameEventList)v)),
            new(nameof(LocationEffectGroupDefinition), new(nameof(Galaxy.OrbTypes),g => g.LocationEffectGroupDefinitions, (g,v) => g.LocationEffectGroupDefinitions = (LocationEffectGroupDefinitionList)v)),
            new(nameof(CharacterAnimation), new(nameof(Galaxy.OrbTypes),g => g.CharacterAnimations, (g,v) => g.CharacterAnimations = (CharacterAnimationList)v)),
            new(nameof(CharacterRoom), new(nameof(Galaxy.OrbTypes),g => g.CharacterRooms, (g,v) => g.CharacterRooms = (CharacterRoomList)v))
        });

    private static readonly ImmutableSortedDictionary<string, InstanceDefFieldInfo<Galaxy>> GalaxyDefs
        = ImmutableSortedDictionary.CreateRange(
        new KeyValuePair<string, InstanceDefFieldInfo<Galaxy>>[]
            { });

    private static readonly ImmutableSortedDictionary<string, InstanceDefFieldInfo<Empire>> EmpireDefs
        = ImmutableSortedDictionary.CreateRange(
        new KeyValuePair<string, InstanceDefFieldInfo<Empire>>[]
        {
            new(nameof(EmpirePolicy), new (nameof(Empire.Policy), e => e.Policy, (e,v) => e.Policy = (EmpirePolicy)v))
        });

    private static readonly ImmutableSortedDictionary<string, string> DefIdFields
        = ImmutableSortedDictionary.CreateRange(
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
            new(nameof(ComponentBay), nameof(ComponentBay.ComponentBayId))
        });

    private static readonly MethodInfo MiGenericPatchIndexedDefinitions
        = typeof(GameDataDefinitionPatching).GetMethod(nameof(GenericPatchIndexedDefinitions))!;

    public static readonly MethodInfo MiGenericPatchDefinitions
        = typeof(GameDataDefinitionPatching).GetMethod(nameof(GenericPatchDefinitions))!;

    public static readonly MmVariableDsl Dsl = new();


    public static void ApplyContentPatches()
    {
            foreach (var dataPath in ModLoader.ModManager.PatchedDataStack)
            {
                try
                {
                    if (ModLoader.DebugMode)
                        Console.WriteLine($"Applying content patches from {dataPath}");
                    GameDataDefinitionPatching.ApplyContentPatches(dataPath);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Failure applying content patches from {dataPath}");
                    ModLoader.ModManager.OnUnhandledException(ExceptionDispatchInfo.Capture(ex));
                }
            }
    }

    public static bool IsIndexedList(IList list)
    {
        var mi = list.GetType().GetMethod("RebuildIndexes", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return mi != null;
    }

    public static void RebuildIndices(IList list)
    {
        var mi = list.GetType().GetMethod("RebuildIndexes", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        mi.Invoke(list, null);
    }

    public static void ApplyLateContentPatches(Galaxy galaxy)
    {
        if (!ModLoader.MaybeWaitForLoaded()) return;

        foreach (var dataPath in ModLoader.ModManager.PatchedDataStack)
        {
            try
            {
                if (ModLoader.DebugMode)
                    Console.WriteLine($"Applying content patches from {dataPath}");
                ApplyDynamicContentPatches(dataPath, galaxy);
            }
            catch (Exception ex)
            {
                try
                {
                    Console.Error.WriteLine($"Failure applying content patches from {dataPath}");
                    ModLoader.ModManager.OnUnhandledException(ExceptionDispatchInfo.Capture(ex));
                }
                catch
                {
                    Console.Error.WriteLine("Failure reporting exception.");
                }
            }
        }

        Galaxy.LoadImagesForItems(Galaxy.Assets, (IList<IDrawableSummary>)Galaxy.RacesStatic.ToArray());
        Galaxy.LoadRaceFlagImages(Galaxy.Assets, Galaxy.RacesStatic);
        Galaxy.LoadImagesForItems(Galaxy.Assets, (IList<IDrawableSummary>)Galaxy.ArtifactsStatic.ToArray());

        if (galaxy == null)
            return;

        galaxy.Artifacts = Galaxy.ArtifactsStatic;
        galaxy.Races = Galaxy.RacesStatic;
        galaxy.GameEvents = Galaxy.GameEventsStatic;
    }

    public static void ApplyDynamicDefinitions(Galaxy galaxy)
    {
        foreach (var dataPath in ModLoader.ModManager.PatchedDataStack)
        {
            try
            {
                if (ModLoader.DebugMode)
                    Console.WriteLine($"Applying content patches from {dataPath}");
                ApplyDynamicContentPatches(dataPath, galaxy);
            }
            catch (Exception ex)
            {
                try
                {
                    Console.Error.WriteLine($"Failure applying content patches from {dataPath}");
                    ModLoader.ModManager.OnUnhandledException(ExceptionDispatchInfo.Capture(ex));
                }
                catch
                {
                    Console.Error.WriteLine("Failure reporting exception.");
                }
            }
        }
    }


    public static void ApplyDynamicContentPatches(string dataPath, Galaxy galaxy)
    {
        bool IsIndexed<T>(T defs, Type type)
        {
            if (typeof(IndexedList<>).MakeGenericType(type).IsInstanceOfType(defs))
                return true;
            MethodInfo mi = defs!.GetType().GetMethod("RebuildIndexes");
            if (mi != null)
                return true;
            return false;
        }

        Dsl.Variables.Clear();
        Dsl.SetGlobal("loader", ModLoader.ModManager);
        Dsl.SetGlobal("game", ModLoader.ModManager.Game);
        Dsl.SetGlobal("galaxy", galaxy);


        var absPath = new Uri(Path.Combine(Environment.CurrentDirectory, dataPath)).LocalPath;
        foreach (var dataFilePath in Directory.EnumerateFiles(absPath, "*.yml", SearchOption.AllDirectories))
        {
            if (dataFilePath is null) continue;
            Console.WriteLine($"Parsing {dataFilePath} for late static and instance definitions");
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

                        if (StaticDefs.ContainsKey(typeStr) || LateStaticDefs.ContainsKey(typeStr))
                            continue;

                        Console.Error.WriteLine($"Can't find defs for {typeStr} @ {keyScalar.Start}");
                    }
                else
                    Console.Error.WriteLine($"Unsupported root node type @ {yr.Start}");
            }
        }
    }

    public static void ApplyLateContentPatches(string dataPath)
    {
        bool IsIndexed<T>(T defs, Type type)
        {
            if (typeof(IndexedList<>).MakeGenericType(type).IsInstanceOfType(defs))
                return true;
            MethodInfo mi = defs!.GetType().GetMethod("RebuildIndexes");
            if (mi != null)
                return true;
            return false;
        }

        Dsl.Variables.Clear();
        Dsl.SetGlobal("loader", ModLoader.ModManager);
        Dsl.SetGlobal("game", ModLoader.ModManager.Game);

        var absPath = new Uri(Path.Combine(Environment.CurrentDirectory, dataPath)).LocalPath;
        foreach (var dataFilePath in Directory.EnumerateFiles(absPath, "*.yml", SearchOption.AllDirectories))
        {
            if (dataFilePath is null) continue;
            Console.WriteLine($"Parsing {dataFilePath} for late static and instance definitions");
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

                        if (LateStaticDefs.TryGetValue(typeStr, out var staticDefs))
                        {
                            DefIdFields.TryGetValue(typeStr, out var idFieldName);

                            var defs = staticDefs.Get();

                            if (IsIndexed(defs, type))
                                PatchIndexedDefinitions(type, defs, valueSeq, idFieldName);
                            else
                                PatchDefinitions(type, defs, valueSeq, idFieldName);
                            continue;
                        }

                        if (StaticDefs.ContainsKey(typeStr) || GalaxyDefs.ContainsKey(typeStr) || EmpireDefs.ContainsKey(typeStr))
                            continue;

                        Console.Error.WriteLine($"Can't find defs for {typeStr} @ {keyScalar.Start}");
                    }
                else
                    Console.Error.WriteLine($"Unsupported root node type @ {yr.Start}");
            }
        }
    }

    public static void ApplyContentPatches(string dataPath)
    {
        bool IsIndexed<T>(T defs, Type type)
        {
            if (typeof(IndexedList<>).MakeGenericType(type).IsInstanceOfType(defs))
                return true;
            MethodInfo mi = defs!.GetType().GetMethod("RebuildIndexes");
            if (mi != null)
                return true;
            return false;
        }

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

                        if (StaticDefs.TryGetValue(typeStr, out var staticDefs))
                        {
                            DefIdFields.TryGetValue(typeStr, out var idFieldName);

                            var defs = staticDefs.Get();

                            if (IsIndexed(defs, type))
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
                        Dsl["item"] = null;
                        Dsl["value"] = null;
                        Func<object> testFn;
                        try
                        {
                            testFn = Dsl.Parse(testStr).CompileFast();
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

                        if (pass)
                            continue;

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

                        if (valStr.Trim().Equals("(delete)", StringComparison.OrdinalIgnoreCase))
                        {
                            if (ModLoader.ModManager.SharedVariables.TryRemove(keyStr, out _))
                                continue;

                            Console.Error.WriteLine($"Failed to remove {keyStr} from state @ {valScalar.Start}");
                            continue;
                        }

                        try
                        {
                            Dsl["item"] = null;
                            Dsl["value"] = null;
                            ModLoader.ModManager.SharedVariables.AddOrUpdate(keyStr,
                                _ => Dsl.Parse(valStr).CompileFast()(),
                                (_, old) => {
                                    Dsl["value"] = old;
                                    return Dsl.Parse(valStr).CompileFast()();
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
                    Dsl["item"] = null;
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
                        whereFn = Dsl.Parse(whereStr).CompileFast();
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
                        (_, expr) => Dsl.Parse(expr).CompileFast());

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
                        Dsl["item"] = null;
                        Dsl["value"] = null;
                        Func<object> testFn;
                        try
                        {
                            testFn = Dsl.Parse(testStr).CompileFast();
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

                        if (pass)
                            continue;

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

                        if (valStr.Trim().Equals("(delete)", StringComparison.OrdinalIgnoreCase))
                        {
                            if (ModLoader.ModManager.SharedVariables.TryRemove(keyStr, out _))
                                continue;

                            Console.Error.WriteLine($"Failed to remove {keyStr} from state @ {valScalar.Start}");
                            continue;
                        }

                        try
                        {
                            Dsl["item"] = null;
                            Dsl["value"] = null;
                            ModLoader.ModManager.SharedVariables.AddOrUpdate(keyStr,
                                _ => Dsl.Parse(valStr).CompileFast()(),
                                (_, old) => {
                                    Dsl["value"] = old;
                                    return Dsl.Parse(valStr).CompileFast()();
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
                
                case "template" when mod is YamlMappingNode item: {

                    var oldIdExpr = item.FirstOrDefault(kv => kv.Key is YamlScalarNode sk && sk.Value == idFieldName);
                    var newIdExpr = item.FirstOrDefault(kv => kv.Key is YamlScalarNode sk && sk.Value == idFieldNamePrefixed);

                    if (newIdExpr.Key == default || oldIdExpr.Key == default)
                    {
                        Console.Error.WriteLine($"Failed to parse {type.Name} @ {item.Start}");
                        break;
                    }
                    
                    if (oldIdExpr.Value is not YamlScalarNode oldIdScalar) {
                        Console.Error.WriteLine($"Failed to parse {type.Name} @ {oldIdExpr.Value.Start}");
                        break;
                    }
                    
                    if (newIdExpr.Value is not YamlScalarNode newIdScalar) {
                        Console.Error.WriteLine($"Failed to parse {type.Name} @ {newIdExpr.Value.Start}");
                        break;
                    }

                    var oldIdStr = oldIdScalar.Value;
                    var oldId = ConvertToIdType(Dsl.Parse(oldIdStr!).CompileFast());
                    var newIdStr = newIdScalar.Value;
                    object? newId = null;
                    if (int.TryParse(newIdStr, out var newIdInt))
                        newId = ConvertToIdType(newIdInt);

                    var old = defs[ConvertToInt(oldId)];

                    var def = DeepCloneTyped(Activator.CreateInstance<T>(), old);

                    if (def is null) throw new NotImplementedException();

                    if (newId is null) {
                        var newIdValue = ModLoader.ModManager.SharedVariables.GetOrAdd(newIdStr!, _ => GetRealNextId(defs));
                        SetId(def, ConvertToIdType(newIdValue));
                    }

                    defs.Add(def);
                    break;
                }

                case "template":
                    Console.Error.WriteLine($"Can't parse template instruction @ {mod.Start}");
                    break;


                case "add" when mod is YamlMappingNode item: {

                    var idLookupReq = item.FirstOrDefault(kv => kv.Key is YamlScalarNode sk && sk.Value == idFieldNamePrefixed);
                    // don't issue error on explicit id set
                    if (idLookupReq.Key == default && !item.Any(kv => kv.Key is YamlScalarNode sk && sk.Value == idFieldName))
                    {
                        Console.Error.WriteLine($"Failed to parse {type.Name} @ {item.Start}");
                        break;
                    }

                    var def = PrepopulateTyped(Activator.CreateInstance<T>())!;

                    if (idLookupReq.Value is YamlScalarNode idLookupVarNode)
                    {
                        var idLookupVar = idLookupVarNode.Value;
                        var value = ModLoader.ModManager.SharedVariables.GetOrAdd(idLookupVar!, _ => GetRealNextId(defs));
                        SetId(def, ConvertToIdType(value));
                        item.Children.Remove(idLookupReq);
                    }

                    Dsl["item"] = null;
                    Dsl["value"] = null;
                    Dsl["collection"] = null;
                    Dsl["def"] = def;

                    try
                    {
                        ProcessObjectUpdate(type, def, item,
                            (_, expr) => Dsl.Parse(expr).CompileFast());
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

                        Dsl["item"] = null;
                        Dsl["value"] = null;
                        idObj = ((IConvertible)Dsl.Parse(idStr).CompileFast()()).ToInt32(NumberFormatInfo.InvariantInfo);

                        item.Children.Remove(idKvNode);

                    }

                    if (idObj is sbyte or short or int
                        or byte or ushort)
                    {
                        var id = ConvertToInt(idObj);

                        var def = defs.Count > id ? defs[id] : null;

                        if (def == null || !id.Equals(ConvertToInt(GetId(def))))
                            def = defs.First(x => id.Equals(ConvertToInt(GetId(x))));

                        Dsl["item"] = null;
                        Dsl["value"] = null;
                        Dsl["collection"] = null;
                        Dsl["def"] = def;

                        ProcessObjectUpdate(type, def, item,
                            (_, expr) => Dsl.Parse(expr).CompileFast());

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

                        Dsl["item"] = null;
                        Dsl["collection"] = null;
                        Dsl["value"] = idVal;
                        Dsl["def"] = def;

                        Func<object> whereFn;
                        try
                        {
                            whereFn = Dsl.Parse(whereStr).CompileFast();
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
                            (_, expr) => Dsl.Parse(expr).CompileFast());

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


    private static readonly ConcurrentDictionary<Type, object?> CreationCache = new()
    {
        [typeof(Xenko.Graphics.Texture)] = null!,
    };

    private static readonly Regex RxStringListItemSubExpression = new Regex(@"{{((?!{)((?<![""\\])""(?:[^""\\]|\\.)*?""(?!=[""\\])|.*?)*)}}");

    private static object? Prepopulate(object? obj, Type? objType = null)
    {
        if (obj is null) return null;

        objType ??= obj.GetType();

        if (CreationCache.TryGetValue(objType, out var newObj))
            return newObj;

        if (!objType.IsClass || objType == typeof(string)) return obj;
        var fieldOrProps = objType
            .GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
            .Where(m => m is FieldInfo { IsStatic: false } or PropertyInfo { GetMethod.IsStatic: false });

        foreach (var fieldOrProp in fieldOrProps)
        {
            var fieldOrPropType = GetFieldOrPropertyType(fieldOrProp);
            if (!fieldOrPropType.IsClass || !typeof(IList).IsAssignableFrom(fieldOrPropType)) continue;
            if (HasSetter(fieldOrProp) && GetValue(obj, fieldOrProp) is null)
                SetValue(obj, fieldOrProp, Prepopulate(CreateInstance(fieldOrPropType), fieldOrPropType));
        }

        return obj;
    }

    private static T? PrepopulateTyped<T>(T obj) where T : class
        => (T?)Prepopulate(obj, typeof(T));

    private static object? DeepClone(object? obj, object src, Type? objType = null)
    {
        if (obj is null) return null;

        objType ??= obj.GetType();

        var fieldOrProps = objType
            .GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
            .Where(m => m is FieldInfo { IsStatic: false } or PropertyInfo { GetMethod.IsStatic: false });

        foreach (var fieldOrProp in fieldOrProps)
        {
            if (HasSetter(fieldOrProp))
                SetValue(obj, fieldOrProp, GetValue(src, fieldOrProp));
        }

        return obj;
    }

    private static T? DeepCloneTyped<T>(T obj, T source) where T : class
        => (T?)DeepClone(obj, source, typeof(T));

    private static object? CreateInstance(Type itemType)
    {
        try
        {
            if (itemType == typeof(string))
                return "";

            if (CreationCache.TryGetValue(itemType, out var item))
                return item;

            return Activator.CreateInstance(itemType);
        }
        catch
        {
            return null;
        }
    }


    public static void PatchIndexedDefinitions(Type type, object defs, YamlSequenceNode mods, string? idFieldName = null)
    {
        var m = MiGenericPatchIndexedDefinitions.MakeGenericMethod(type);
        m.Invoke(null, new[] { defs, mods, idFieldName });
    }
    public static void GenericPatchIndexedDefinitions<T>(List<T> defs, YamlSequenceNode mods, string? idFieldName = null) where T : class
    {
        try
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
                    idFieldAsPropInfo!.SetValue(item,
                        ((IConvertible)id).ToType(idFieldAsPropInfo.PropertyType, NumberFormatInfo.InvariantInfo));
            }

            object ConvertToIdType(object id)
                => ((IConvertible)id).ToType(idFieldType!, NumberFormatInfo.InvariantInfo);

            int ConvertToInt(object id)
                => ((IConvertible)id).ToInt32(NumberFormatInfo.InvariantInfo);

            int GetIndex(List<T> defs, object id)
            {
                switch (defs)
                {
                    case IndexedList<T> indexed when id is int:
                        return indexed.GetIndex(ConvertToInt(id));
                    default:
                        MethodInfo mi = defs.GetType().GetMethod("GetIndex");
                        return (int)mi.Invoke(defs, new object[] { id });
                }
            }

            bool ContainsId(List<T> defs, object id)
            {
                switch (defs)
                {
                    case IndexedList<T> indexed:
                        return indexed.ContainsId(ConvertToInt(id));
                    default:
                        MethodInfo mi = defs.GetType().GetMethod("ContainsId");
                        return (bool)mi.Invoke(defs, new object[] { id });
                }
            }

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
                            Dsl["item"] = null;
                            Dsl["value"] = null;
                            Func<object> testFn;
                            try
                            {
                                testFn = Dsl.Parse(testStr).CompileFast();
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

                            if (pass)
                                continue;

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

                            if (valStr.Trim().Equals("(delete)", StringComparison.OrdinalIgnoreCase))
                            {
                                if (ModLoader.ModManager.SharedVariables.TryRemove(keyStr, out _))
                                    continue;

                                Console.Error.WriteLine($"Failed to remove {keyStr} from state @ {valScalar.Start}");
                                continue;
                            }

                            Dsl["item"] = null;
                            Dsl["value"] = null;
                            ModLoader.ModManager.SharedVariables.AddOrUpdate(keyStr,
                                _ => Dsl.Parse(valStr).CompileFast()(),
                                (_, old) => {
                                    Dsl["value"] = old;
                                    return Dsl.Parse(valStr).CompileFast()();
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

                            var index = GetIndex(defs, id);

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
                            Console.WriteLine(
                                $"Removed {type.Name} {id}: {removedAsString.Substring(0, Math.Min(removedAsString.Length, 64))}");

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

                        var def = PrepopulateTyped(Activator.CreateInstance<T>())!;

                        if (idLookupReq.Value is YamlScalarNode idLookupVarNode)
                        {
                            var idLookupVar = idLookupVarNode.Value;
                            var value = ModLoader.ModManager.SharedVariables.GetOrAdd(idLookupVar!, _ => GetRealNextId(defs));
                            SetId(def, ConvertToIdType(value));
                            item.Children.Remove(idLookupReq);
                        }

                        Dsl["item"] = null;
                        Dsl["value"] = null;
                        Dsl["collection"] = null;
                        Dsl["def"] = def;

                        try
                        {
                            ProcessObjectUpdate(type, def, item,
                                (_, expr) => Dsl.Parse(expr).CompileFast());
                        }
                        catch (Exception ex)
                        {
                            ModLoader.ModManager.OnUnhandledException(ExceptionDispatchInfo.Capture(ex));
                            Console.Error.WriteLine($"Failed to parse {type.Name} @ {item.Start}");
                            break;
                        }

                        var rawId = GetId(def);                        
                        var contained = ContainsId(defs, rawId);
                        var id = ConvertToInt(rawId);
                        if (contained && defs[id] is not null)
                        {
                            Console.Error.WriteLine($"Failed to add {type.Name} @ {item.Start}; {id} already defined");
                            break;
                        }
                        if (contained)
                            defs[id] = def;
                        else
                            defs.Add(def);
                        /*
                        if (!defs[id]?.Equals(def) ?? false)
                        {
                            Console.Error.WriteLine($"Failed to validate after adding {type.Name} @ {item.Start}; {id} not found");
                            break;
                        }
                        */
                        break;
                    }

                    case "add":
                        Console.Error.WriteLine($"Can't parse add instruction @ {mod.Start}");
                        break;
                
                case "template" when mod is YamlMappingNode item: {

                    var oldIdExpr = item.FirstOrDefault(kv => kv.Key is YamlScalarNode sk && sk.Value == idFieldName);
                    var newIdExpr = item.FirstOrDefault(kv => kv.Key is YamlScalarNode sk && sk.Value == idFieldNamePrefixed);

                    if (newIdExpr.Key == default || oldIdExpr.Key == default)
                    {
                        Console.Error.WriteLine($"Failed to parse {type.Name} @ {item.Start}");
                        break;
                    }
                    
                    if (oldIdExpr.Value is not YamlScalarNode oldIdScalar) {
                        Console.Error.WriteLine($"Failed to parse {type.Name} @ {oldIdExpr.Value.Start}");
                        break;
                    }
                    
                    if (newIdExpr.Value is not YamlScalarNode newIdScalar) {
                        Console.Error.WriteLine($"Failed to parse {type.Name} @ {newIdExpr.Value.Start}");
                        break;
                    }

                    var oldIdStr = oldIdScalar.Value;
                    var oldId = ConvertToIdType(Dsl.Parse(oldIdStr!).CompileFast());
                    var newIdStr = newIdScalar.Value;
                    object? newId = null;
                    if (int.TryParse(newIdStr, out var newIdInt))
                        newId = ConvertToIdType(newIdInt);

                    var old = defs[ConvertToInt(oldId)];

                    var def = DeepCloneTyped(Activator.CreateInstance<T>(), old);

                    if (def is null) throw new NotImplementedException();

                    if (newId is null) {
                        var newIdValue = ModLoader.ModManager.SharedVariables.GetOrAdd(newIdStr!, _ => GetRealNextId(defs));
                        SetId(def, ConvertToIdType(newIdValue));
                    }

                    defs.Add(def);
                    break;
                }

                case "template":
                    Console.Error.WriteLine($"Can't parse template instruction @ {mod.Start}");
                    break;

                    case "update" when mod is YamlMappingNode item: {
                        object raw_id;
                        // don't issue error on explicit id set
                        var idLookupReq = item.FirstOrDefault(kv => kv.Key is YamlScalarNode sk && sk.Value == idFieldNamePrefixed);

                        if (idLookupReq.Value is YamlScalarNode idLookupVarNode)
                        {
                            var idLookupVar = idLookupVarNode.Value;
                            raw_id = ConvertToIdType(ModLoader.ModManager.SharedVariables.GetOrAdd(idLookupVar!, _ => GetRealNextId(defs)));

                            item.Children.Remove(idLookupReq);                            
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

                            raw_id = ConvertToIdType((IConvertible)Dsl.Parse(idStr).CompileFast()());

                            item.Children.Remove(idKvNode);
                        }

                        var index = GetIndex(defs, raw_id);
                        var id = ConvertToInt(raw_id);
                        var def = defs.Count > index ? defs[index] : defs.Count > id ? defs[id] : null;

                        if (def == null || !id.Equals(ConvertToInt(GetId(def))))
                            def = defs.First(x => id.Equals(ConvertToInt(GetId(x))));

                        Dsl["item"] = null;
                        Dsl["value"] = null;
                        Dsl["collection"] = null;
                        Dsl["def"] = def;

                        ProcessObjectUpdate(type, def, item,
                            (_, expr) => Dsl.Parse(expr).CompileFast());

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

                        foreach (var def in defs)
                        {
                            Dsl["item"] = null;
                            Dsl["value"] = null;
                            Dsl["collection"] = null;
                            Dsl["def"] = def;

                            Func<object> whereFn;
                            try
                            {
                                whereFn = Dsl.Parse(whereStr).CompileFast();
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

                            var idObj = GetId(def);

                            var idVal = ((IConvertible)idObj).ToDouble(NumberFormatInfo.InvariantInfo);

                            if (!pass)
                                continue;

                            ProcessObjectUpdate(type, def, item,
                                (_, expr) => Dsl.Parse(expr).CompileFast());

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
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to apply patches to {defs!.GetType().FullName} @ {mods.Start}");
            ModLoader.OnUnhandledException(ExceptionDispatchInfo.Capture(ex));
        }
        finally
        {
            if (defs is not null)
            {
                try
                {
                    if(defs is IndexedList<T> indexed)
                        indexed.RebuildIndexes();
                    else
                    {
                        MethodInfo mi = defs.GetType().GetMethod("RebuildIndexes");
                        mi.Invoke(defs, null);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to rebuild indexes for {defs.GetType().FullName}!");
                    ModLoader.OnUnhandledException(ExceptionDispatchInfo.Capture(ex));
                }
            }
        }
    }
    private static object ProcessObjectUpdate(Type type, object obj, YamlMappingNode item, Func<object, string, Func<object>> compileFn,
        IList? collection = null)
    {
        foreach (var kv in item)
        {
            if (kv.Key is null)
            {
                Console.Error.WriteLine("Warning: null key in mapping?!");
                continue;
            }
            if (kv.Key is not YamlScalarNode key)
                throw new NotImplementedException(kv.Key.Start.ToString());

            if (key.Value is null)
                throw new NotSupportedException(key.Start.ToString());

            var name = key.Value;

            var isFormula = false;
            if (name[0] == '$')
            {
                name = name.Substring(1);
                isFormula = true;
            }

            MemberInfo member;
            try
            {
                member = GetInstancePropertyOrField(type, name);
            }
            catch (Exception ex)
            {
                // compatibility with XML Serializer layout
                if (name == type.Name
                    && item.Children.Count == 1
                    && kv.Value is YamlMappingNode actualItem)
                    return ProcessObjectUpdate(type, obj, actualItem, compileFn);
                throw new NotSupportedException(key.Start.ToString(), ex);
            }

            if (member is null)
                throw new NotSupportedException(key.Start.ToString());

            var valType = GameDataUtils.GetType(member);

            var initValue = GetValue(obj, member);

            var valNode = kv.Value;

            if (typeof(IList).IsAssignableFrom(valType))
            {
                initValue ??= CreateInstance(valType)!;
                switch (valNode)
                {
                    case YamlScalarNode scalar: {
                        if (scalar.Value?.Trim().Equals("(delete)",StringComparison.OrdinalIgnoreCase) ?? false)
                            SetValue(obj, member, null);
                        else
                            throw new NotImplementedException(valNode.ToString());
                        break;
                    }
                    case YamlSequenceNode seq: {
                        ParseCollectionUpdate(valType, ref initValue, seq, compileFn);
                        break;
                    }
                    case YamlMappingNode map: {
                        ParseCollectionUpdate(valType, ref initValue, map, compileFn);
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
                        var valStr = scalar.Value!;
                        if (isFormula && collection is not null)
                        {
                            if (DefIdFields.TryGetValue(type.Name, out var idField) && idField == name)
                            {
                                var newKey = ((IConvertible)collection.Count).ToType(valType, NumberFormatInfo.InvariantInfo);
                                ModLoader.ModManager.SharedVariables[valStr] = newKey;
                                SetValue(obj, member, newKey);
                                break;
                            }
                        }
                        IConvertible newValue;
                        try
                        {
                            Dsl["value"] = initValue;
                            var fn = compileFn(member, valStr);
                            newValue = (IConvertible)fn();
                        }
                        catch
                        {
                            if (isFormula)
                                throw new NotSupportedException(scalar.Start.ToString());
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
                        throw new NotImplementedException(valNode.Start.ToString());
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
                        throw new NotImplementedException(valNode.Start.ToString());
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
                        throw new NotImplementedException(valNode.Start.ToString());
                }
            else if (valType.IsClass)
            {
                initValue ??= PrepopulateTyped(CreateInstance(valType)!)!;
                switch (valNode)
                {
                    case YamlMappingNode map: {
                        ProcessObjectUpdate(valType, initValue, map, compileFn);
                        break;
                    }
                    default:
                        throw new NotImplementedException(valNode.Start.ToString());
                }
            }
            else if (valType.IsValueType)
            {
                initValue ??= CreateInstance(valType)!;
                switch (valNode)
                {
                    case YamlMappingNode map: {
                        ProcessObjectUpdate(valType, initValue, map, compileFn);
                        break;
                    }
                    default:
                        throw new NotImplementedException(valNode.Start.ToString());
                }
            }
            else
                Console.WriteLine($"Warning, field with unsupported type {valType.FullName} @ {valNode.Start}");
        }
        return obj;
    }

    private static void ParseCollectionUpdate(Type collectionType, ref object collection, YamlSequenceNode seq,
        Func<object, string, Func<object>> compileFn)
    {
        if (collection is IList)
            ParseCollectionUpdate(collectionType, ref Unsafe.As<object, IList>(ref collection), seq, compileFn);
        else
            throw new NotImplementedException("Non-IList based collection.");
    }

    private static void ParseCollectionUpdate(Type collectionType, ref IList collection, YamlSequenceNode seq,
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
                collection[index] = ProcessCollectionItemUpdate(valNode, itemType, collection[index], compileFn, collection);
            else
            {
                if (collection.IsFixedSize)
                {
                    if (!collectionType.IsArray)
                        throw new NotImplementedException("Can't grow non-array fixed size collection.");

                    var newCollection = Array.CreateInstance(itemType, collection.Count + 1);
                    collection.CopyTo(newCollection, 0);
                    collection = newCollection;

                    collection[index] = ProcessCollectionItemUpdate(valNode, itemType,
                        CreateInstance(itemType)!, compileFn, collection);
                    continue;
                }
                collection.Add(ProcessCollectionItemUpdate(valNode, itemType,
                    CreateInstance(itemType)!, compileFn, collection));
            }
        }
    }

    private static void ParseCollectionUpdate(Type collectionType, ref object collection, YamlMappingNode map,
        Func<object, string, Func<object>> compileFn)
    {
        if (collection is IList list)
        {
            ParseCollectionUpdate(collectionType, ref list, map, compileFn);
            if (IsIndexedList(list))
            {
                RebuildIndices(list);
            }
        }
        else
            throw new NotImplementedException("Non-IList based collection.");       
    }

    private static void ParseCollectionUpdate(Type collectionType, ref IList collection, YamlMappingNode map,
        Func<object, string, Func<object>> compileFn)
    {
        var itemType = collectionType.GetInterfaces()
            .First(t => t.IsGenericType && typeof(IList<>) == t.GetGenericTypeDefinition())
            .GetGenericArguments()[0];

        foreach (var kv in map)
        {
            if (kv.Key is not YamlScalarNode key)
                throw new NotImplementedException(kv.Key.Start.ToString());

            var keyStr = key.Value;

            if (keyStr is null)
                throw new NotSupportedException(key.Start.ToString());

            var valNode = kv.Value;

            if (!int.TryParse(keyStr, NumberStyles.Number, NumberFormatInfo.InvariantInfo, out var idVal))
            {
                if (keyStr == "$add")
                {
                    if (kv.Value is not YamlSequenceNode seq)
                        throw new NotImplementedException(key.Start.ToString());
                    foreach (var seqItem in seq)
                    {
                        Dsl["collection"] = collection;
                        collection.Add(
                            ProcessCollectionItemUpdate(seqItem,
                                itemType,
                                Prepopulate(CreateInstance(itemType))!,
                                compileFn, collection));
                    }
                    return;
                }

                if (keyStr[0] == '(' && keyStr[keyStr.Length - 1] == ')')
                {
                    // no state, always cache
                    try
                    {
                        for (var i = 0; i < collection.Count; i++)
                        {
                            var item = collection[i];
                            Dsl["item"] = item;
                            Dsl["value"] = i;
                            Dsl["collection"] = collection;
                            object result;
                            try
                            {
                                result = compileFn("", keyStr.Substring(1, keyStr.Length - 2))();
                            }
                            catch
                            {
                                throw new NotSupportedException(key.Start.ToString());
                            }
                            var pass = ((IConvertible)result).ToBoolean(NumberFormatInfo.InvariantInfo);
                            if (!pass) continue;
                            Dsl["item"] = null;
                            Dsl["value"] = null;
                            collection[i] = ProcessCollectionItemUpdate(valNode, itemType, collection[i], compileFn, collection);
                        }
                        return;
                    }
                    catch (Exception ex)
                    {
                        throw new NotImplementedException(key.Start.ToString(), ex);
                    }
                }

            }

            object initValue;
            Dsl["collection"] = collection;
            if (collection.Count > idVal)
            {
                initValue = collection[idVal];
                ProcessCollectionItemUpdate(valNode, itemType, initValue, compileFn, collection);

            }
            else
            {
                initValue = PrepopulateTyped(CreateInstance(itemType)!)!;
                collection.Add(ProcessCollectionItemUpdate(valNode, itemType, initValue, compileFn, collection));
            }
        }
    }
    private static object? ProcessCollectionItemUpdate(YamlNode valNode, Type itemType, object initValue,
        Func<object, string, Func<object>> compileFn, IList collection)
    {
        var isString = itemType == typeof(string);
        if (
            itemType.IsPrimitive
            && Type.GetTypeCode(itemType) is not
                (TypeCode.Char
                or TypeCode.Decimal
                or TypeCode.DateTime)
            || isString
        )
            switch (valNode)
            {
                case YamlScalarNode scalar: {
                    var valStr = scalar.Value!;
                    
                    /*
                     * x: # string[]
                     *  - a # <-- verbatim?
                     *  - xyz {{c}} xyz # <-- script subexpression?
                     */

                    if (!isString) {
                        return ((IConvertible)valStr)
                            .ToType(itemType, NumberFormatInfo.InvariantInfo);
                    }

                    Dsl["value"] = null;
                    IConvertible newValue
                        = RxStringListItemSubExpression.Replace(valStr,
                            m => {
                                var result = "";
                                try {
                                    var fn = compileFn(collection, m.Groups[1].Value);
                                    result = fn().ToString();
                                }
                                catch (Exception ex) {
                                    ModLoader.OnUnhandledException(ExceptionDispatchInfo.Capture(ex));
                                }

                                return result ?? "";
                            });

                    return newValue.ToType(itemType, NumberFormatInfo.InvariantInfo);
                }
                case YamlSequenceNode seq: {
                    throw new NotImplementedException(valNode.Start.ToString());
                }
                case YamlMappingNode map: {
                    if (map.Children.Count == 1)
                    {
                        var firstKv = map.First();
                        if (firstKv.Key is YamlScalarNode typeNode)
                        {
                            var typeStr = typeNode.Value;
                            if (typeStr == itemType.Name)
                            {
                                if (firstKv.Value is YamlMappingNode subMap)
                                    return ProcessObjectUpdate(itemType, initValue, subMap, compileFn, collection);
                                throw new NotImplementedException(firstKv.Value.Start.ToString());
                            }

                            // TODO: handle descendent types
                            throw new NotImplementedException(firstKv.Value.Start.ToString());
                        }
                        throw new NotImplementedException(firstKv.Key.Start.ToString());
                    }
                    return ProcessObjectUpdate(itemType, initValue, map, compileFn, collection);
                }
            }
        else
            switch (valNode)
            {
                case YamlScalarNode scalar: {
                    if (scalar.Value?.Trim().Equals("(delete)",StringComparison.OrdinalIgnoreCase) ?? false)
                        return itemType.IsClass ? null : Activator.CreateInstance(itemType);
                    throw new NotImplementedException(valNode.ToString());
                }
                case YamlSequenceNode seq: {
                    if (initValue is not IList list)
                        throw new NotImplementedException(valNode.Start.ToString());
                    ParseCollectionUpdate(itemType, ref list, seq, compileFn);
                    return list;
                }
                case YamlMappingNode map: {
                    if (initValue is not IList list)
                        return ProcessObjectUpdate(itemType, initValue, map, compileFn, collection);
                    ParseCollectionUpdate(itemType, ref list, map, compileFn);
                    return list;
                }
            }

        throw new NotImplementedException(valNode.Start.ToString());
    }
}
