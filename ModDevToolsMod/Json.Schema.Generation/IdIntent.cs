using Json.Schema;
using Json.Schema.Generation;

namespace ModDevToolsMod;

/// <summary>
/// Provides intent to create a `id` keyword.
/// </summary>
public class IdIntent : ISchemaKeywordIntent {

  public string Reference { get; }

  /// <summary>
  /// Creates a new instance of the <see cref="IdIntent"/> class.
  /// </summary>
  public IdIntent(string reference)
    => Reference = reference;

  /// <summary>
  /// Applies the keyword to the <see cref="JsonSchemaBuilder"/>.
  /// </summary>
  /// <param name="builder">The builder.</param>
  public void Apply(JsonSchemaBuilder builder)
    => builder.Id(Reference);

  /// <summary>Determines whether the specified object is equal to the current object.</summary>
  /// <param name="obj">The object to compare with the current object.</param>
  /// <returns>true if the specified object  is equal to the current object; otherwise, false.</returns>
  public override bool Equals(object? obj)
    => !ReferenceEquals(null, obj);

  /// <summary>Serves as the default hash function.</summary>
  /// <returns>A hash code for the current object.</returns>
  public override int GetHashCode() {
    unchecked {
      return typeof(IdIntent).GetHashCode();
    }
  }

}