using System.Collections.Immutable;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using DistantWorlds.Types;
using DistantWorlds2.ModLoader;
using JetBrains.Annotations;
using Json.More;
using Json.Schema;
using Json.Schema.Generation;
using Json.Schema.Generation.Intents;

namespace ModDevToolsMod;

[PublicAPI]
public class Mod {

  //public const string JsonSchema2020r12 = "https://json-schema.org/draft/2020-12/schema";
  public const string JsonSchemaDraft7 = "https://json-schema.org/draft-07/schema";

  internal static readonly ImmutableHashSet<Type> DefTypes = ImmutableHashSet.CreateRange(new[] {
    typeof(OrbType),
    typeof(Resource),
    typeof(ComponentDefinition),
    typeof(Race),
    typeof(Artifact),
    typeof(PlanetaryFacilityDefinition),
    typeof(ColonyEventDefinition),
    typeof(ResearchProjectDefinition),
    typeof(TroopDefinition),
    typeof(CreatureType),
    typeof(Government),
    typeof(DesignTemplate),
    typeof(ShipHull),
    typeof(FleetTemplate),
    typeof(ArmyTemplate),
    typeof(GameEvent),
    typeof(LocationEffectGroupDefinition),
    typeof(CharacterAnimation),
    typeof(CharacterRoom),
    typeof(EmpirePolicy)
  });

  internal static readonly ImmutableHashSet<Type> PerEmpireDefs = ImmutableHashSet.CreateRange(new[] {
    typeof(EmpirePolicy)
  });

  internal static readonly ImmutableDictionary<string, string> DefIdFields = ImmutableDictionary.CreateRange(
    new KeyValuePair<string, string>[] {
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

  public static readonly SimpleDsl Dsl = new();

  [RegexPattern]
  public static readonly string TokenizerRegexPatternCaseSensitive
    = Dsl.Language.Tokenizer.RegexPattern.Replace("(?i)", "");

  [RegexPattern]
  public static readonly string ExpressionRegexPattern
    = $@"^(?:{TokenizerRegexPatternCaseSensitive})+$";

  [RegexPattern]
  public static readonly string ListSelectExpressionRegexPattern
    = $@"^0|[1-9][0-9]*|\((?:{TokenizerRegexPatternCaseSensitive})+\)$";

  private static readonly PatternIntent ExpressionPatternIntent = new(ExpressionRegexPattern);

  public static readonly Uri ExprLangRefUri = new Uri("./expression-language.json#", UriKind.Relative);

  public Mod() {
    var contentDefPatchSchema = new JsonSchemaBuilder()
      .Schema(JsonSchemaDraft7)
      .Id("https://dw2mc.github.io/DW2ModLoader/content-def-patch.json")
      .Type(SchemaValueType.Object)
      .AdditionalProperties(false)
      .UnevaluatedProperties(false)
      .MinProperties(1)
      .Properties(new ReadOnlyLinearDictionary<string, JsonSchema>(DefTypes.ToDictionary(
        type => type.Name,
        type => {
          if (PerEmpireDefs.Contains(type)) {
            return new JsonSchemaBuilder()
              .Type(SchemaValueType.Array)
              .MinItems(1)
              .Items(new JsonSchemaBuilder()
                .Type(SchemaValueType.Object)
                .Properties(
                  ("update", new JsonSchemaBuilder()
                    .AllOf(
                      new JsonSchemaBuilder()
                        .Properties(("$where",
                          new JsonSchemaBuilder()
                            .Ref(ExprLangRefUri)))
                        .MinProperties(1)
                        .AdditionalProperties(false)
                        .UnevaluatedProperties(false)
                        .Required("$where"),
                      new JsonSchemaBuilder()
                        .Ref($"./def-{type.Name}.json#")
                    ))
                ))
              .Build();
          }
          else {
            var idFieldName = DefIdFields[type.Name];
            var idField = type.GetField(idFieldName);
            var isStringIdField = idField.FieldType == typeof(string);
            var isIntegerIdField = Type.GetTypeCode(idField.FieldType) is >= TypeCode.SByte and <= TypeCode.UInt64;
            var addProps = new Dictionary<string, JsonSchema> {
              {
                idFieldName,
                isStringIdField
                  ? new JsonSchemaBuilder()
                    .Type(SchemaValueType.String)
                  : new JsonSchemaBuilder()
                    .OneOf(
                      new JsonSchemaBuilder()
                        .Type(isIntegerIdField ? SchemaValueType.Integer : SchemaValueType.Number),
                      new JsonSchemaBuilder()
                        .Ref(ExprLangRefUri))
              }, {
                $"${idFieldName}", new JsonSchemaBuilder()
                  .Type(SchemaValueType.String)
              }
            };
            var updateProps = new Dictionary<string, JsonSchema> {
              {
                idFieldName,
                isStringIdField
                  ? new JsonSchemaBuilder()
                    .AnyOf(
                      new JsonSchemaBuilder()
                        .Type(SchemaValueType.String),
                      new JsonSchemaBuilder()
                        .Ref(ExprLangRefUri))
                  : new JsonSchemaBuilder()
                    .OneOf(
                      new JsonSchemaBuilder()
                        .Type(isIntegerIdField ? SchemaValueType.Integer : SchemaValueType.Number),
                      new JsonSchemaBuilder()
                        .Ref(ExprLangRefUri))
              }, {
                $"${idFieldName}", new JsonSchemaBuilder()
                  .Type(SchemaValueType.String)
              }
            };
            var removeProps = new Dictionary<string, JsonSchema> {
              { idFieldName, new JsonSchemaBuilder().Type(SchemaValueType.Number).Build() },
              { $"${idFieldName}", new JsonSchemaBuilder().Ref(ExprLangRefUri).Build() }
            };
            return new JsonSchemaBuilder()
              .Type(SchemaValueType.Array)
              .MinItems(1)
              .AdditionalItems(false)
              .UnevaluatedItems(false)
              .Items(new JsonSchemaBuilder()
                .Type(SchemaValueType.Object)
                .MinProperties(1)
                .AdditionalProperties(false)
                .UnevaluatedProperties(false)
                .Properties(
                  ("state", new JsonSchemaBuilder()
                    .Type(SchemaValueType.Object)
                    .PatternProperties((new("^[A-Z_][0-9A-Za-z]+$", RegexOptions.CultureInvariant),
                        new JsonSchemaBuilder()
                          .Ref(ExprLangRefUri)
                      )
                    )),
                  ("add", new JsonSchemaBuilder()
                    .AllOf(
                      new JsonSchemaBuilder()
                        .Ref($"./def-{type.Name}.json#"),
                      new JsonSchemaBuilder()
                        .OneOf(
                          new JsonSchemaBuilder()
                            .Required(idFieldName),
                          new JsonSchemaBuilder()
                            .Required($"${idFieldName}")))
                  ),
                  ("update", new JsonSchemaBuilder()
                    .AllOf(
                      new JsonSchemaBuilder()
                        .Ref($"./def-{type.Name}.json#"),
                      new JsonSchemaBuilder()
                        .OneOf(
                          new JsonSchemaBuilder()
                            .Required(idFieldName),
                          new JsonSchemaBuilder()
                            .Required($"${idFieldName}")))
                    .Properties(updateProps)),
                  ("update-all", new JsonSchemaBuilder()
                    .AllOf(
                      new JsonSchemaBuilder()
                        .Ref($"./def-{type.Name}.json#"),
                      new JsonSchemaBuilder()
                        .Required("$where")
                    )),
                  ("remove", new JsonSchemaBuilder()
                    .Type(SchemaValueType.Object)
                    .Properties(removeProps)
                    .OneOf(
                      new JsonSchemaBuilder()
                        .Required(idFieldName),
                      new JsonSchemaBuilder()
                        .Required($"${idFieldName}")))
                ))
              .Build();
          }
        }).OrderBy(p => p.Key))).Build();

    var exprLangSchema = new JsonSchemaBuilder()
      .Schema(JsonSchemaDraft7)
      .Id("https://dw2mc.github.io/DW2ModLoader/expression-language.json")
      .Type(SchemaValueType.String)
      .Pattern(ExpressionRegexPattern)
      .Defs(new Dictionary<string, JsonSchema> {
        {
          "list-selection", new JsonSchemaBuilder()
            .PropertyNames(new JsonSchemaBuilder()
              .Pattern(ListSelectExpressionRegexPattern))
        }
      })
      .Build();

    Directory.CreateDirectory("tmp/schema");

    Console.WriteLine($"Generating content-def-patch schema");
    using (var contentDefPatchSchemaJson = contentDefPatchSchema.ToJsonDocument())
    using (var fs = File.Create("tmp/schema/content-def-patch.json"))
    using (var utf8JsonWriter = new Utf8JsonWriter(fs, new() { Indented = true }))
      contentDefPatchSchemaJson.WriteTo(utf8JsonWriter);

    Console.WriteLine($"Generating expression-language schema");
    using (var exprLangSchemaJson = exprLangSchema.ToJsonDocument())
    using (var fs = File.Create("tmp/schema/expression-language.json"))
    using (var utf8JsonWriter = new Utf8JsonWriter(fs, new() { Indented = true }))
      exprLangSchemaJson.WriteTo(utf8JsonWriter);

    // might do each type in parallel
    var lockObj = new object();
    var options = new ParallelOptions {
#if DEBUG
      MaxDegreeOfParallelism = 1
#endif
    };
    Parallel.ForEach(DefTypes, options, type => {
      try {
        var exprStrRefiner = new Dw2ContentDefinitionSchemaRefiner(type);
        var listRefiner = new Dw2ContentDefinitionListSchemaRefiner(type, exprStrRefiner);
        var typeSchemaBuilder = new JsonSchemaBuilder()
          .FromType(type, new() {
            Refiners = new() {
              exprStrRefiner,
              listRefiner,
            },
            PropertyOrder = PropertyOrder.AsDeclared
          });

        // I couldn't just let it do automatic optimization; recursion is involved so it'd crash
        // I couldn't get it to stop overwriting $defs, so I added definitions, now to merge them back to $defs

        var fixedTypeSchemaBuilder = new JsonSchemaBuilder();
        fixedTypeSchemaBuilder.Add(typeSchemaBuilder.Get<SchemaKeyword>()!);
        fixedTypeSchemaBuilder.Add(typeSchemaBuilder.Get<IdKeyword>()!);
        fixedTypeSchemaBuilder.Add(typeSchemaBuilder.Get<TypeKeyword>()!);
        fixedTypeSchemaBuilder.Add(typeSchemaBuilder.Get<PropertiesKeyword>()!);
        var defsKeyword1 = typeSchemaBuilder.Get<DefsKeyword>()!;
        var defsKeyword2 = typeSchemaBuilder.Get<DefinitionsKeyword>()!;
        fixedTypeSchemaBuilder.Add(new DefsKeyword(
          new ReadOnlyLinearDictionary<string, JsonSchema>(
            defsKeyword1.Definitions
              .Concat(defsKeyword2.Definitions)
              .OrderBy(kv => kv.Key)
          )));

        var typeSchema = fixedTypeSchemaBuilder.Build();

        var typeSchemaJson = typeSchema.ToJsonDocument();

        using (var fs = File.Create($"tmp/schema/def-{type.Name}.json"))
        using (var utf8JsonWriter = new Utf8JsonWriter(fs, new() { Indented = true }))
          typeSchemaJson.WriteTo(utf8JsonWriter);
      }
      catch (Exception ex) {
        lock (lockObj) {
          Console.WriteLine($"Exception while generating schema for {type.Name}");
          ModLoader.OnUnhandledException(ExceptionDispatchInfo.Capture(ex));
        }
      }
    });
  }

}