using YamlDotNet.Serialization;

namespace DistantWorlds2.ModLoader;

public class HackyTypeInspectorWrapper : ITypeInspector
{
    private readonly ITypeInspector _backingTypeInspector;
    private readonly VariableMathDslBase _dsl;
    private readonly Func<object> _contextFn;
    private readonly Action _ctxPopFn;
    public HackyTypeInspectorWrapper(ITypeInspector backingTypeInspector, VariableMathDslBase dsl, Func<object> contextFn, Action ctxPopFn)
    {
        _backingTypeInspector = backingTypeInspector;
        _dsl = dsl;
        _contextFn = contextFn;
        _ctxPopFn = ctxPopFn;
    }

    public IEnumerable<IPropertyDescriptor> GetProperties(Type type, object? container)
        => _backingTypeInspector.GetProperties(type, container);

    public IPropertyDescriptor GetProperty(Type type, object? container, string name, bool ignoreUnmatched)
    {
        // maybe this mechanism can break if a same-type is nested? wish container was populated here, but alas is null
        var ctx = _contextFn();
        while (type != ctx.GetType())
        {
            _ctxPopFn();
            ctx = _contextFn();
        }

        var pd = _backingTypeInspector.GetProperty(type, container, name, ignoreUnmatched);

        var t = pd.TypeOverride ?? pd.Type;

        if (!t.IsPrimitive) return pd;

        switch (Type.GetTypeCode(t))
        {
            default: {
                _dsl.Value = double.NaN;
                break;
            }
            case TypeCode.SByte:
            case TypeCode.Byte:
            case TypeCode.Int16:
            case TypeCode.UInt16:
            case TypeCode.Int32:
            case TypeCode.UInt32:
            case TypeCode.Int64:
            case TypeCode.UInt64:
            case TypeCode.Single:
            case TypeCode.Double:
            case TypeCode.Decimal: {
                var v = (IConvertible?)pd.Read(ctx).Value;
                _dsl.Value = v?.ToDouble(null) ?? double.NaN;
                break;
            }
        }

        return pd;
    }
}
