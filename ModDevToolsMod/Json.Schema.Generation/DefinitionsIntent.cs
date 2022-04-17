using Json.Schema;
using Json.Schema.Generation;

namespace ModDevToolsMod;

/// <summary>
/// Provides intent to create an arbitrary definition scope.
/// </summary>
public class DefinitionsIntent : ISchemaKeywordIntent {

  /// <summary>
  /// The contexts that represent the definitions.
  /// </summary>
  public Dictionary<string, SchemaGeneratorContext> Definitions { get; }

  /// <summary>
  /// Creates a new <see cref="DefinitionsIntent"/> instance.
  /// </summary>
  /// <param name="definitions">The contexts.</param>
  public DefinitionsIntent(Dictionary<string, SchemaGeneratorContext> definitions) {
    Definitions = definitions;
  }

  /// <summary>
  /// Applies the keyword to the <see cref="JsonSchemaBuilder"/>.
  /// </summary>
  /// <param name="builder">The builder.</param>
  public void Apply(JsonSchemaBuilder builder) {
    builder.Add(new DefinitionsKeyword(Definitions.ToDictionary(
      p => p.Key,
      p => p.Value.Apply().Build())));
  }

  /// <summary>Determines whether the specified object is equal to the current object.</summary>
  /// <param name="obj">The object to compare with the current object.</param>
  /// <returns>true if the specified object  is equal to the current object; otherwise, false.</returns>
  public override bool Equals(object? obj)
    => !ReferenceEquals(null, obj);

  /// <summary>Serves as the default hash function.</summary>
  /// <returns>A hash code for the current object.</returns>
  public override int GetHashCode() {
    unchecked {
      var hashCode = GetType().GetHashCode();
      foreach (var property in Definitions) {
        hashCode = (hashCode * 397) ^ property.Key.GetHashCode();
        hashCode = (hashCode * 397) ^ property.Value.GetHashCode();
      }

      return hashCode;
    }
  }

}