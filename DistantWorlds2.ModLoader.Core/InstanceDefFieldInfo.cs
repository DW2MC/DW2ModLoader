namespace DistantWorlds2.ModLoader;

public class InstanceDefFieldInfo<T> : IDefFieldInfo {

  public string FieldName { get; }

  public Func<T, object> Get { get; }

  public Action<T, object> Set { get; }

  public InstanceDefFieldInfo(string fieldName, Func<T, object> get, Action<T, object> set) {
    FieldName = fieldName;
    Get = get;
    Set = set;
  }

}