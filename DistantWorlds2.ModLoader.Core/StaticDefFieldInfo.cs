namespace DistantWorlds2.ModLoader;

public class StaticDefFieldInfo : IStaticDefFieldInfo {

  public string FieldName { get; }

  public Func<object> Get { get; }

  public Action<object> Set { get; }

  public StaticDefFieldInfo(string fieldName, Func<object> get, Action<object> set) {
    FieldName = fieldName;
    Get = get;
    Set = set;
  }

}