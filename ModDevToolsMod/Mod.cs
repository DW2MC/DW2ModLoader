using System.Collections;
using System.Collections.Immutable;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using DistantWorlds.Types;
using DistantWorlds2;
using DistantWorlds2.ModLoader;
using Humanizer;
using JetBrains.Annotations;
using Json.More;
using Json.Schema;
using Json.Schema.Generation;
using Json.Schema.Generation.Intents;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization.TypeInspectors;
using Encoding = System.Text.Encoding;

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

  private static readonly ImmutableDictionary<string, Func<object>> StaticDefs = ImmutableDictionary.CreateRange(
    new KeyValuePair<string, Func<object>>[] {
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
    new KeyValuePair<string, Func<Galaxy, object>>[] {
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

  public static readonly SimpleDsl Dsl = new();

  [RegexPattern]
  public static readonly string TokenizerRegexPatternCaseSensitive
    = Dsl.Language.Tokenizer.RegexPattern.Replace("(?i)", "");

  [RegexPattern]
  public static readonly string ExpressionRegexPattern
    = $@"^(?:{TokenizerRegexPatternCaseSensitive})+$";

  [RegexPattern]
  public static readonly string ListSelectExpressionRegexPattern
    = $@"^(?:0|[1-9][0-9]*|\((?:{TokenizerRegexPatternCaseSensitive})+\))$";

  private static readonly PatternIntent ExpressionPatternIntent = new(ExpressionRegexPattern);

  public static readonly Uri ExprLangRefUri = new("./expression-language.json#", UriKind.Relative);

  public Mod(DWGame game) {
    var orderedDefTypes = DefTypes.OrderBy(t => t.Name).ToImmutableArray();
    var contentDefPatchSchema = new JsonSchemaBuilder()
      .Schema(JsonSchemaDraft7)
      .Id("https://dw2mc.github.io/DW2ModLoader/content-def-patch.json")
      .Title($"The content definition document context")
      .Description($"The content definition document context.\nSupported root contexts: {string.Join(", ", orderedDefTypes.Select(t => t.Name))}\nSee https://github.com/DW2MC/DW2ModLoader/wiki/Content-definition-document")
      .Type(SchemaValueType.Object)
      .AdditionalProperties(false)
      .UnevaluatedProperties(false)
      .MinProperties(1)
      .Properties(new ReadOnlyLinearDictionary<string, JsonSchema>(orderedDefTypes.ToDictionary(
        type => type.Name,
        type => {
          if (PerEmpireDefs.Contains(type)) {
            return new JsonSchemaBuilder()
              .Title($"{type.FullName} context")
              .Description(
                $"The {type.FullName} content definition root context.\nAvailable instructions: state, update\nSee https://github.com/DW2MC/DW2ModLoader/wiki/{type.FullName}#Content-definition-root-context")
              .Type(SchemaValueType.Array)
              .MinItems(1)
              .Items(new JsonSchemaBuilder()
                .Type(SchemaValueType.Object)
                .Properties(
                  ("state", new JsonSchemaBuilder()
                    .Title("Create or modify variables in a globally shared state.")
                    .Description("See https://github.com/DW2MC/DW2ModLoader/wiki/YAML-content-patch-syntax-reference#state")
                    .Type(SchemaValueType.Object)
                    .PatternProperties((new("^[A-Z_][0-9A-Za-z]+$", RegexOptions.CultureInvariant),
                        new JsonSchemaBuilder()
                          .Ref(ExprLangRefUri)
                      )
                    )),
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

          var idFieldName = DefIdFields[type.Name];
          var idField = type.GetField(idFieldName);
          var isStringIdField = idField.FieldType == typeof(string);
          var isIntegerIdField = Type.GetTypeCode(idField.FieldType) is >= TypeCode.SByte and <= TypeCode.UInt64;
          return new JsonSchemaBuilder()
            .Title($"{type.FullName} context")
            .Description(
              $"The {type.FullName} content definition root context.\nAvailable instructions: state, add, template, remove, remove-all, update, update-all\nSee https://github.com/DW2MC/DW2ModLoader/wiki/{type.FullName}#Content-definition-root-context")
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
                  .Title("Create or modify variables in a globally shared state.")
                  .Description("See https://github.com/DW2MC/DW2ModLoader/wiki/YAML-content-patch-syntax-reference#state")
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
                      .Title($"Add one {type.Name} definition.")
                      .Description($"See https://github.com/DW2MC/DW2ModLoader/wiki/YAML-content-patch-syntax-reference#add and https://github.com/DW2MC/DW2ModLoader/wiki/{type.FullName}")
                      .OneOf(
                        new JsonSchemaBuilder()
                          .Required(idFieldName),
                        new JsonSchemaBuilder()
                          .Required($"${idFieldName}")))
                ),
                ("template", new JsonSchemaBuilder()
                  .AllOf(
                    new JsonSchemaBuilder()
                      .Title($"Clone one {type.Name} definition and change it's identity.")
                      .Description($"See https://github.com/DW2MC/DW2ModLoader/wiki/YAML-content-patch-syntax-reference#template and https://github.com/DW2MC/DW2ModLoader/wiki/{type.FullName}")
                      .OneOf(
                        new JsonSchemaBuilder()
                          .Required(idFieldName, $"${idFieldName}")))
                ),
                ("update",
                  new JsonSchemaBuilder()
                    .AllOf(
                      new JsonSchemaBuilder()
                        .Ref($"./def-{type.Name}.json#"),
                      new JsonSchemaBuilder()
                        .Title($"Update one {type.Name} definition.")
                        .Description($"See https://github.com/DW2MC/DW2ModLoader/wiki/YAML-content-patch-syntax-reference#update and https://github.com/DW2MC/DW2ModLoader/wiki/{type.FullName}")
                        .OneOf(
                          new JsonSchemaBuilder()
                            .Required(idFieldName),
                          new JsonSchemaBuilder()
                            .Required($"${idFieldName}")))
                    .Properties(new Dictionary<string, JsonSchema> {
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
                        $"${idFieldName}",
                        new JsonSchemaBuilder()
                          .Type(SchemaValueType.String)
                      }
                    })),
                ("update-all", new JsonSchemaBuilder()
                  .AllOf(
                    new JsonSchemaBuilder()
                      .Ref($"./def-{type.Name}.json#"),
                    new JsonSchemaBuilder()
                      .Title($"Update a matching selection of {type.Name} definitions.")
                      .Description($"See https://github.com/DW2MC/DW2ModLoader/wiki/YAML-content-patch-syntax-reference#update-all and https://github.com/DW2MC/DW2ModLoader/wiki/{type.FullName}")
                      .Required("$where")
                  )),
                ("remove", new JsonSchemaBuilder()
                  .Type(SchemaValueType.Object)
                  .Title($"Remove one {type.Name} definition.")
                  .Description($"See https://github.com/DW2MC/DW2ModLoader/wiki/YAML-content-patch-syntax-reference#remove and https://github.com/DW2MC/DW2ModLoader/wiki/{type.FullName}")
                  .Properties(new Dictionary<string, JsonSchema> {
                    { idFieldName, new JsonSchemaBuilder().Type(SchemaValueType.Number).Build() },
                    { $"${idFieldName}", new JsonSchemaBuilder().Ref(ExprLangRefUri).Build() }
                  })
                  .OneOf(
                    new JsonSchemaBuilder()
                      .Required(idFieldName),
                    new JsonSchemaBuilder()
                      .Required($"${idFieldName}"))),
                ("remove-all", new JsonSchemaBuilder()
                  .Type(SchemaValueType.Object)
                  .Title($"Remove a matching selection of {type.Name} definitions.")
                  .Description($"See https://github.com/DW2MC/DW2ModLoader/wiki/YAML-content-patch-syntax-reference#remove-all and https://github.com/DW2MC/DW2ModLoader/wiki/{type.FullName}")
                  .Properties(("$where",
                    new JsonSchemaBuilder()
                      .Ref(ExprLangRefUri)))
                  .Required("$where"))
              ))
            .Build();
        }))).Build();

    var exprLangSchema = new JsonSchemaBuilder()
      .Schema(JsonSchemaDraft7)
      .Id("https://dw2mc.github.io/DW2ModLoader/expression-language.json")
      .Type(SchemaValueType.String)
      .Pattern(ExpressionRegexPattern)
      .Title("An expression.")
      .Description("See https://github.com/DW2MC/DW2ModLoader/wiki/Expression-language-syntax-reference")
      .Defs(new Dictionary<string, JsonSchema> {
        {
          "list-selection", new JsonSchemaBuilder()
            .Title("A list selection expression.")
            .Description("See https://github.com/DW2MC/DW2ModLoader/wiki/Expression-language-syntax-reference")
            .PropertyNames(new JsonSchemaBuilder()
              .Pattern(ListSelectExpressionRegexPattern))
        }
      })
      .Build();

    Directory.CreateDirectory("tmp/schema");
    Directory.CreateDirectory("tmp/ref-yml");

    Console.WriteLine("Generating content-def-patch schema");
    using (var contentDefPatchSchemaJson = contentDefPatchSchema.ToJsonDocument())
    using (var fs = File.Create("tmp/schema/content-def-patch.json"))
    using (var utf8JsonWriter = new Utf8JsonWriter(fs, new() { Indented = true }))
      contentDefPatchSchemaJson.WriteTo(utf8JsonWriter);

    Console.WriteLine("Generating expression-language schema");
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
        fixedTypeSchemaBuilder.Add(typeSchemaBuilder.Get<TitleKeyword>()!);
        fixedTypeSchemaBuilder.Add(typeSchemaBuilder.Get<DescriptionKeyword>()!);
        fixedTypeSchemaBuilder.Add(typeSchemaBuilder.Get<AdditionalPropertiesKeyword>() ?? new AdditionalPropertiesKeyword(false));
        fixedTypeSchemaBuilder.Add(typeSchemaBuilder.Get<UnevaluatedItemsKeyword>() ?? new UnevaluatedItemsKeyword(false));
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

      var szrB1 = new SerializerBuilder()
        .WithTypeInspector(inner => new ReadableAndWritablePropertiesTypeInspector(inner), loc => loc.OnBottom())
        .WithNamingConvention(NullNamingConvention.Instance)
        .WithTypeInspector(i => new RemoveExtraneousMembersInspector(i))
        .WithTypeInspector(i => new DefRefInspector(i, type))
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.Preserve);

      var szrB2 = new SerializerBuilder()
        .WithTypeInspector(inner => new ReadableAndWritablePropertiesTypeInspector(inner), loc => loc.OnBottom())
        .WithNamingConvention(NullNamingConvention.Instance)
        .WithTypeInspector(i => new RemoveExtraneousMembersInspector(i))
        .WithTypeInspector(i => new DefRefInspector(i, type))
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults | DefaultValuesHandling.OmitNull | DefaultValuesHandling.OmitEmptyCollections);

      if (StaticDefs.TryGetValue(type.Name, out var getDefs)) {
        var defs = (IEnumerable)getDefs();
        var szr1 = szrB1.Build();
        var szr2 = szrB2.Build();

        using (var fs = File.Create($"tmp/ref-yml/{type.Name}.yml")) {
          using var tw = new StreamWriter(fs, Encoding.UTF8, 65536);
          var em = new YamlDotNet.Core.Emitter(tw);
          em.Emit(new StreamStart());
          var e = defs.GetEnumerator();
          if (e.MoveNext()) {
            var obj = e.Current;
            em.Emit(new DocumentStart());
            em.Emit(new MappingStart(null, null, false, MappingStyle.Block));
            em.Emit(new Scalar(type.Name));
            szr1.Serialize(new StreamAndDocumentSkipperEmitter(em), obj);
            em.Emit(new MappingEnd());
            em.Emit(new DocumentEnd(true));
            tw.Flush();
            while (e.MoveNext()) {
              obj = e.Current;
              em.Emit(new DocumentStart());
              em.Emit(new MappingStart(null, null, false, MappingStyle.Block));
              em.Emit(new Scalar(type.Name));
              szr2.Serialize(new StreamAndDocumentSkipperEmitter(em), obj);
              em.Emit(new MappingEnd());
              em.Emit(new DocumentEnd(true));
              tw.Flush();
            }
          }
          em.Emit(new StreamEnd());
        }
      }
      else if (InstanceDefs.TryGetValue(type.Name, out var getInstDefs)) {
        var defs = (IEnumerable)getInstDefs(game.Galaxy);

        var szr1 = szrB1.Build();
        var szr2 = szrB2.Build();

        using (var fs = File.Create($"tmp/ref-yml/{type.Name}.yml")) {
          using var tw = new StreamWriter(fs, Encoding.UTF8, 65536);
          var em = new YamlDotNet.Core.Emitter(tw);
          em.Emit(new StreamStart());
          var e = defs.GetEnumerator();
          if (e.MoveNext()) {
            var obj = e.Current;
            em.Emit(new DocumentStart());
            em.Emit(new MappingStart(null, null, false, MappingStyle.Block));
            em.Emit(new Scalar(type.Name));
            szr1.Serialize(new StreamAndDocumentSkipperEmitter(em), obj);
            em.Emit(new MappingEnd());
            em.Emit(new DocumentEnd(true));
            tw.Flush();
            while (e.MoveNext()) {
              obj = e.Current;
              em.Emit(new DocumentStart());
              em.Emit(new MappingStart(null, null, false, MappingStyle.Block));
              em.Emit(new Scalar(type.Name));
              szr2.Serialize(new StreamAndDocumentSkipperEmitter(em), obj);
              em.Emit(new MappingEnd());
              em.Emit(new DocumentEnd(true));
              tw.Flush();
            }
          }
          em.Emit(new StreamEnd());
        }
      }
    });
  }

  public static string GetFriendlyName(Type type) {
    Type? elemType;
    if (type.IsArray && type.HasElementType) {
      elemType = type.GetElementType()!;
      return $"{GetFriendlyName(elemType).Pluralize()} collection";
    }

    elemType = type.GetInterfaces().FirstOrDefault(i => i.IsConstructedGenericType && i.GetGenericTypeDefinition() == typeof(IList<>))
      ?.GetGenericArguments()[0];
    if (elemType is not null)
      return $"{GetFriendlyName(elemType).Pluralize()} collection";

    if (type.IsEnum)
      return type.Name;

    return Type.GetTypeCode(type) switch {
      TypeCode.Boolean => "boolean",
      TypeCode.Char => "16-bit character value",
      TypeCode.DateTime => "date and time",
      TypeCode.Single => "floating point number",
      TypeCode.Double => "double-precision floating point number",
      TypeCode.String => "text string",
      TypeCode.Byte => "byte",
      TypeCode.SByte => "signed byte",
      TypeCode.Int16 => "16-bit integer",
      TypeCode.UInt16 => "unsigned 16-bit integer",
      TypeCode.Int32 => "32-bit integer",
      TypeCode.UInt32 => "unsigned 32-bit integer",
      TypeCode.Int64 => "64-bit integer",
      TypeCode.UInt64 => "unsigned 32-bit integer",
      _ => type == typeof(NumberExpression)
        ? "decimal number or expression"
        : type == typeof(IntegerExpression)
          ? "integer number or expression"
          : type.Name
    };
  }

  public static string GetFriendlyDescription(Type type) {
    Type? elemType = null;
    if (type.IsArray && type.HasElementType)
      elemType = type.GetElementType()!;
    elemType ??= type.GetInterfaces().FirstOrDefault(i => i.IsConstructedGenericType && i.GetGenericTypeDefinition() == typeof(IList<>))
      ?.GetGenericArguments()[0];
    if (elemType is not null)
      return Type.GetTypeCode(elemType) switch {
        TypeCode.Boolean => "A collection of boolean values.",
        TypeCode.Char => "A collection of UTF-16 characters.",
        TypeCode.DateTime => "A collection of date and time values.",
        TypeCode.Single => "A collection of single precision floating point values.",
        TypeCode.Double => "A collection of double precision floating point values.",
        TypeCode.String => "A collection of texts.",
        TypeCode.SByte => "A collection of signed 8-bit integers.",
        TypeCode.Byte => "A collection of unsigned 8-bit integers.",
        TypeCode.Int16 => "A collection of signed 16-bit integers.",
        TypeCode.UInt16 => "A collection of unsigned 16-bit integers.",
        TypeCode.Int32 => "A collection of signed 32-bit integers.",
        TypeCode.UInt32 => "A collection of unsigned 32-bit integers.",
        TypeCode.Int64 => "A collection of signed 64-bit integers.",
        TypeCode.UInt64 => "A collection of unsigned 64-bit integers.",
        _ => type == typeof(NumberExpression)
          ? "A collection of decimal numbers.\nSee https://github.com/DW2MC/DW2ModLoader/wiki/YAML-content-patch-syntax-reference#Collections"
          : type == typeof(IntegerExpression)
            ? "A collection of integer numbers.\nSee https://github.com/DW2MC/DW2ModLoader/wiki/YAML-content-patch-syntax-reference#Collections"
            : elemType.FullName is null
              ? $"A collection of unknown things.\nSee https://github.com/DW2MC/DW2ModLoader/wiki/YAML-content-patch-syntax-reference#Collections and https://github.com/DW2MC/DW2ModLoader/wiki/{type.Assembly.GetName().Name}#Token-0x{type.MetadataToken:X8}"
              : $"See https://github.com/DW2MC/DW2ModLoader/wiki/YAML-content-patch-syntax-reference#Collections and https://github.com/DW2MC/DW2ModLoader/wiki/{elemType.FullName}"
      };

    if (type.IsEnum)
      return
        $"Some text (enum) mapped to some {GetFriendlyName(Enum.GetUnderlyingType(type))}\nSee https://github.com/DW2MC/DW2ModLoader/wiki/YAML-content-patch-syntax-reference#Enums and https://github.com/DW2MC/DW2ModLoader/wiki/{type.FullName}";

    return Type.GetTypeCode(type) switch {
      TypeCode.Boolean => "A boolean value.\nSee https://github.com/DW2MC/DW2ModLoader/wiki/Expression-language-syntax-reference#booleans",
      TypeCode.Char => "A UTF-16 character value.\nSee https://github.com/DW2MC/DW2ModLoader/wiki/Expression-language-syntax-reference#numbers",
      TypeCode.DateTime => "A date and time value.\nSee https://github.com/DW2MC/DW2ModLoader/wiki/Expression-language-syntax-reference#strings",
      TypeCode.Single => "A single precision floating point value.\nSee https://github.com/DW2MC/DW2ModLoader/wiki/Expression-language-syntax-reference#numbers",
      TypeCode.Double => "A double precision floating point value.\nSee https://github.com/DW2MC/DW2ModLoader/wiki/Expression-language-syntax-reference#numbers",
      TypeCode.String => "Some text.\nSee https://github.com/DW2MC/DW2ModLoader/wiki/Expression-language-syntax-reference#strings",
      TypeCode.SByte => "A signed 8-bit integer.\nSee https://github.com/DW2MC/DW2ModLoader/wiki/Expression-language-syntax-reference#numbers",
      TypeCode.Byte => "A unsigned 8-bit integer.\nSee https://github.com/DW2MC/DW2ModLoader/wiki/Expression-language-syntax-reference#numbers",
      TypeCode.Int16 => "A signed 16-bit integer.\nSee https://github.com/DW2MC/DW2ModLoader/wiki/Expression-language-syntax-reference#numbers",
      TypeCode.UInt16 => "A unsigned 16-bit integer.\nSee https://github.com/DW2MC/DW2ModLoader/wiki/Expression-language-syntax-reference#numbers",
      TypeCode.Int32 => "A signed 32-bit integer.\nSee https://github.com/DW2MC/DW2ModLoader/wiki/Expression-language-syntax-reference#numbers",
      TypeCode.UInt32 => "A unsigned 32-bit integer.\nSee https://github.com/DW2MC/DW2ModLoader/wiki/Expression-language-syntax-reference#numbers",
      TypeCode.Int64 => "A signed 64-bit integer.\nSee https://github.com/DW2MC/DW2ModLoader/wiki/Expression-language-syntax-reference#numbers",
      TypeCode.UInt64 => "A unsigned 64-bit integer.\nSee https://github.com/DW2MC/DW2ModLoader/wiki/Expression-language-syntax-reference#numbers",
      _ => type == typeof(NumberExpression)
        ? "A decimal number or an expression.\nSee https://github.com/DW2MC/DW2ModLoader/wiki/Expression-language-syntax-reference#numbers"
        : type == typeof(IntegerExpression)
          ? "An integer number or an expression.\nSee https://github.com/DW2MC/DW2ModLoader/wiki/Expression-language-syntax-reference#numbers"
          : type.FullName is null
            ? $"Some unknown thing.\nSee https://github.com/DW2MC/DW2ModLoader/wiki/{type.Assembly.GetName().Name}#Token-0x{type.MetadataToken:X8}"
            : $"See https://github.com/DW2MC/DW2ModLoader/wiki/{type.FullName}"
    };
  }

}