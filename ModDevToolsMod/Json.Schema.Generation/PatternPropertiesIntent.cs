using System.Text.RegularExpressions;
using Json.Schema;
using Json.Schema.Generation;

namespace ModDevToolsMod;

/// <summary>
/// Provides intent to create an `patternedProperties` keyword.
/// </summary>
public class PatternPropertiesIntent : ISchemaKeywordIntent, IContextContainer {

  /// <summary>
  /// The contexts that represent the properties.
  /// </summary>
  public Dictionary<Regex, SchemaGeneratorContext> PatternProperties { get; }

  /// <summary>
  /// Creates a new <see cref="PatternPropertiesIntent"/> instance.
  /// </summary>
  /// <param name="patternProperties">The contexts.</param>
  public PatternPropertiesIntent(Dictionary<Regex, SchemaGeneratorContext> patternProperties)
    => PatternProperties = patternProperties;

  /// <summary>
  /// Gets the contexts.
  /// </summary>
  /// <returns>
  ///	The <see cref="SchemaGeneratorContext"/>s contained by this object.
  /// </returns>
  public IEnumerable<SchemaGeneratorContext> GetContexts()
    => PatternProperties.Values;

  /// <summary>
  /// Replaces one context with another.
  /// </summary>
  /// <param name="hashCode">The hashcode of the context to replace.</param>
  /// <param name="newContext">The new context.</param>
  public void Replace(int hashCode, SchemaGeneratorContext newContext) {
    foreach (var property in PatternProperties.ToList()) {
      var hc = property.Value.GetHashCode();
      if (hc == hashCode)
        PatternProperties[property.Key] = newContext;
    }
  }

  /// <summary>
  /// Applies the keyword to the <see cref="JsonSchemaBuilder"/>.
  /// </summary>
  /// <param name="builder">The builder.</param>
  public void Apply(JsonSchemaBuilder builder) {
    builder.PatternProperties(PatternProperties.ToDictionary(p => p.Key, p => p.Value.Apply().Build()));
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
      foreach (var property in PatternProperties) {
        hashCode = (hashCode * 397) ^ property.Key.GetHashCode();
        hashCode = (hashCode * 397) ^ property.Value.GetHashCode();
      }

      return hashCode;
    }
  }

}
