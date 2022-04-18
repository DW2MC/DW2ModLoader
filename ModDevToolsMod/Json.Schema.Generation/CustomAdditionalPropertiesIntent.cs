using Json.Schema;
using Json.Schema.Generation;

namespace ModDevToolsMod;

/// <summary>
/// Provides intent to create an `additionalProperties` keyword.
/// </summary>
public class CustomAdditionalPropertiesIntent : ISchemaKeywordIntent {

  /// <summary>
  /// The context that represents the inner requirements.
  /// </summary>
  public JsonSchema Context { get; }

  /// <summary>
  /// Creates a new <see cref="AdditionalPropertiesIntent"/> instance.
  /// </summary>
  /// <param name="context">The context.</param>
  public CustomAdditionalPropertiesIntent(JsonSchema context)
    => Context = context;

  /// <summary>
  /// Applies the keyword to the <see cref="JsonSchemaBuilder"/>.
  /// </summary>
  /// <param name="builder">The builder.</param>
  public void Apply(JsonSchemaBuilder builder) {
    builder.AdditionalProperties(Context);
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
      var hashCode = typeof(AdditionalPropertiesIntent).GetHashCode();
      hashCode = (hashCode * 397) ^ Context.GetHashCode();
      return hashCode;
    }
  }

}