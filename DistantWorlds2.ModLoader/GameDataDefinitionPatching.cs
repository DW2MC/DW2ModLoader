using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Serialization;
using CppNet;
using DistantWorlds.Types;
using HarmonyLib;
using JetBrains.Annotations;
using SharpDX.Text;
using Xenko.Core.IO;
using Xenko.Core.Serialization;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization.NodeDeserializers;
using YamlDotNet.Serialization.ObjectFactories;
using VirtualFileSystem = Xenko.Core.IO.VirtualFileSystem;

namespace DistantWorlds2.ModLoader;

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

    public static void ApplyStaticDataPatches(ModManager mm, string dataPath)
    {
        var absPath = new Uri(Path.Combine(Environment.CurrentDirectory, dataPath)).LocalPath;
        foreach (var dataFilePath in Directory.EnumerateFiles(absPath, "*.yml", SearchOption.AllDirectories))
        {
            if (dataFilePath is null) continue;
            using var s = File.Open(dataFilePath, FileMode.Open, FileAccess.Read);
            var ys = GameDataUtils.LoadYaml(s);
            foreach (var yd in ys)
            {
                var yr = yd.RootNode;

                if (yr is YamlMappingNode ymr)
                {
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

                        if (!StaticDefs.TryGetValue(typeStr, out var getDefs))
                        {
                            Console.Error.WriteLine($"Can't find static defs for {typeStr} @ {keyScalar.Start}");
                            continue;
                        }
                        
                        DefIdFields.TryGetValue(typeStr, out var idFieldName);

                        var defs = getDefs();
                        
                        if (typeof(IndexedList<>).MakeGenericType(type).IsInstanceOfType(defs))
                            GameDataUtils.PatchIndexedData(type, defs, valueSeq, idFieldName);
                        else
                            GameDataUtils.PatchData(type, defs, valueSeq, idFieldName);
                    }
                }
                else
                {
                    Console.Error.WriteLine($"Unsupported root node type @ {yr.Start}");
                }
            }
        }
    }
}
