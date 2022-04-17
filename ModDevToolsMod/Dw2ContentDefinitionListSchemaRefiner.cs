using System.Collections;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Json.Schema;
using Json.Schema.Generation;
using Json.Schema.Generation.Intents;

namespace ModDevToolsMod;

public class Dw2ContentDefinitionListSchemaRefiner : ISchemaRefiner {

  public Type RootType { get; }

  public bool ShouldRun(SchemaGeneratorContext context) {
    return context.Type.GetInterfaces()
      .Any(t => t.IsArray || t.IsGenericType
        && typeof(ICollection<>) == t.GetGenericTypeDefinition());
  }

  private static readonly Regex RxListIndex = new(@"^0|[1-9][0-9]*|\([^\)]+\)$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

  public Dw2ContentDefinitionListSchemaRefiner(Type rootType)
    => RootType = rootType;

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

    var isItemObj = itemCtx.Intents.OfType<TypeIntent>().FirstOrDefault()?.Type is SchemaValueType.Object;

    var defCtx = isItemObj ? SchemaGenerationContextCache.Get(typeof(Def<>).MakeGenericType(elemType), new(0), context.Configuration) : null;

    if (itemListCtx.Intents.Count <= 1) {
      itemListCtx.Intents.Clear();
      itemListCtx.Intents.Add(new TypeIntent(SchemaValueType.Array));
      itemListCtx.Intents.Add(new ItemsIntent(itemCtx));
    }

    SchemaGeneratorContext itemOrDeleteCtx = SchemaGenerationContextCache.Get(typeof(ItemOrDelete<>).MakeGenericType(elemType), new(0), context.Configuration);

    if (defCtx is not null && itemCtx.Intents.Count >= 1 && isItemObj
        && !itemCtx.Intents.Any(x => x is DynamicRefIntent or RefIntent)) {
      defCtx.Intents.Clear();
      foreach (var intent in itemCtx.Intents)
        defCtx.Intents.Add(intent);
      itemCtx.Intents.Clear();
      var itemRefUri = new Uri($"#/$defs/{elemType.Name}", UriKind.Relative);
      itemCtx.Intents.Add(new RefIntent(itemRefUri));
      var rootCtx = SchemaGenerationContextCache.Get(RootType, new(0), context.Configuration);
      if (rootCtx is null) throw new NotImplementedException();

      var rootDefs = rootCtx.Intents.OfType<DefsIntent>().FirstOrDefault();
      if (rootDefs is null) rootCtx.Intents.Add(rootDefs = new(new()));

      rootDefs.Definitions.Add(elemType.Name, defCtx);

      itemOrDeleteCtx.Intents.Clear();
      itemOrDeleteCtx.Intents.Add(new OneOfIntent(new ISchemaKeywordIntent[] {
        new RefIntent(itemRefUri)
      }, new ISchemaKeywordIntent[] {
        new TypeIntent(SchemaValueType.String),
        new PatternIntent(@"^\(delete\)$")
      }));
    }

    context.Intents.Add(new OneOfIntent(
      new ISchemaKeywordIntent[] {
        new TypeIntent(SchemaValueType.Array),
        new ItemsIntent(itemOrDeleteCtx)
      },
      new ISchemaKeywordIntent[] {
        new TypeIntent(SchemaValueType.Object),
        new PatternPropertiesIntent(new() {
          { RxListIndex, itemOrDeleteCtx }
        }),
        new RefIntent(new("./expression-language.json#/$defs/list-selection", UriKind.Relative))
      },
      new ISchemaKeywordIntent[] {
        new TypeIntent(SchemaValueType.Object),
        new PropertiesIntent(new() {
          { "$add", itemListCtx }
        })
      }
    ));
  }

}