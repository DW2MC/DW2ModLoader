using System.Text.RegularExpressions;
using Json.Schema;
using Json.Schema.Generation;
using Json.Schema.Generation.Intents;

namespace ModDevToolsMod;

public class Dw2ContentDefinitionListSchemaRefiner : ISchemaRefiner {

  public Type RootType { get; }

  public Dw2ContentDefinitionSchemaRefiner ContentDefsRefiner { get; }

  public bool ShouldRun(SchemaGeneratorContext context) {
    return context.Type.GetInterfaces()
      .Any(t => t.IsArray || t.IsGenericType
        && typeof(ICollection<>) == t.GetGenericTypeDefinition());
  }

  private static readonly Regex RxListIndex = new(@"^0|[1-9][0-9]*|\([^\)]+\)$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

  public Dw2ContentDefinitionListSchemaRefiner(Type rootType, Dw2ContentDefinitionSchemaRefiner contentDefsRefiner) {
    RootType = rootType;
    ContentDefsRefiner = contentDefsRefiner;
  }

  public void Run(SchemaGeneratorContext context) {
    // oneOf [ array, $add, map incl (...) ]
    // ignored type name child, warning or error?

    context.Intents.Clear();

    var elemType = context.Type.HasElementType
      ? context.Type.GetElementType()!
      : context.Type.GetInterfaces()
        .First(t => t.IsGenericType
          && typeof(ICollection<>) == t.GetGenericTypeDefinition())
        .GetGenericArguments()[0]!;

    var itemCtx = SchemaGenerationContextCache.Get(elemType, new(0), context.Configuration);
    var itemListCtx = SchemaGenerationContextCache.Get(typeof(AddToList<>).MakeGenericType(elemType), new(0), context.Configuration);

    var isItemComplex = itemCtx.Intents.OfType<TypeIntent>().FirstOrDefault()?.Type is SchemaValueType.Object || elemType.IsEnum;

    var defCtx = isItemComplex ? SchemaGenerationContextCache.Get(typeof(Def<>).MakeGenericType(elemType), new(0), context.Configuration) : null;

    if (itemListCtx.Intents.Count <= 1) {
      itemListCtx.Intents.Clear();
      itemListCtx.Intents.Add(new TypeIntent(SchemaValueType.Array));
      itemListCtx.Intents.Add(new ItemsIntent(itemCtx));
    }

    var itemOrDeleteCtx = SchemaGenerationContextCache.Get(typeof(ItemOrDelete<>).MakeGenericType(elemType), new(0), context.Configuration);

    var itemRefUri = isItemComplex ? new Uri($"#/$defs/{elemType.FullName}", UriKind.Relative) : null;

    if (defCtx is not null && itemCtx.Intents.Count >= 1 && itemRefUri is not null
        && !itemCtx.Intents.Any(x => x is DynamicRefIntent or RefIntent)) {
      defCtx.Intents.Clear();
      if (!elemType.IsEnum) {
        defCtx.Intents.Add(new UnevaluatedPropertiesIntent(false));
        defCtx.Intents.Add(new AdditionalPropertiesIntent(false));
      }

      foreach (var intent in itemCtx.Intents)
        defCtx.Intents.Add(intent);

      ContentDefsRefiner.Definitions.Add(elemType.FullName, defCtx);

      itemCtx.Intents.Clear();
      itemCtx.Intents.Add(new RefIntent(itemRefUri));
    }

    if (itemOrDeleteCtx.Intents.Count < 1 && !itemOrDeleteCtx.Intents.OfType<AnyOfIntent>().Any()) {
#if DEBUG
      if (itemRefUri is not null && !ContentDefsRefiner.Definitions.ContainsKey(elemType.FullName))
        throw new NotImplementedException();
#endif

      var isItemSimple = !isItemComplex;
      var isString = isItemSimple && elemType == typeof(string);
      var typeCode = Type.GetTypeCode(elemType);
      var isBoolean = isItemSimple && typeCode is TypeCode.Boolean;
      var isInteger = isItemSimple && typeCode is >= TypeCode.SByte and <= TypeCode.UInt64;

      itemOrDeleteCtx.Intents.Clear();
      itemOrDeleteCtx.Intents.Add(
        new AnyOfIntent(
          itemRefUri is not null
            ? new ISchemaKeywordIntent[] {
              new RefIntent(itemRefUri)
            }
            : new ISchemaKeywordIntent[] {
              new TypeIntent(
                isBoolean
                  ? SchemaValueType.Boolean
                  : isString
                    ? SchemaValueType.String
                    : isInteger
                      ? SchemaValueType.Integer
                      : SchemaValueType.Number
              )
            },
          new ISchemaKeywordIntent[] {
            new TypeIntent(SchemaValueType.String),
            new PatternIntent(@"^\(delete\)$")
          }
        ));
    }

    context.Intents.Add(new AnyOfIntent(
      new ISchemaKeywordIntent[] {
        new TypeIntent(SchemaValueType.Array),
        new ItemsIntent(itemOrDeleteCtx),
        new UnevaluatedItemsIntent(false)
      },
      new ISchemaKeywordIntent[] {
        new AllOfIntent(
          new ISchemaKeywordIntent[] {
            new RefIntent(new("./expression-language.json#/$defs/list-selection", UriKind.Relative))
          },
          new ISchemaKeywordIntent[] {
            new CustomAdditionalPropertiesIntent(
              new JsonSchemaBuilder()
                .AnyOf(
                  itemRefUri is not null
                    ? new JsonSchemaBuilder().Ref(itemRefUri)
                    : itemCtx.Apply(),
                  new JsonSchemaBuilder()
                    .Enum("(delete)")
                )
                .Build()
            ),
            new UnevaluatedPropertiesIntent(false)
          }
        )
      },
      new ISchemaKeywordIntent[] {
        new TypeIntent(SchemaValueType.Object),
        new PropertiesIntent(new() {
          { "$add", itemListCtx }
        }),
        new AdditionalPropertiesIntent(false),
        new UnevaluatedPropertiesIntent(false)
      }
    ));
  }

}