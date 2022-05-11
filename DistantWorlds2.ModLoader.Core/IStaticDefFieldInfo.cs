namespace DistantWorlds2.ModLoader;

interface IStaticDefFieldInfo : IDefFieldInfo {

  public Func<object> Get { get; }

  public Action<object> Set { get; }

}