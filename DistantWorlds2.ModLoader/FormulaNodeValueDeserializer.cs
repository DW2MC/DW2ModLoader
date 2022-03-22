using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.Utilities;

namespace DistantWorlds2.ModLoader;

public class FormulaScalarNodeDeserializer : INodeDeserializer
{
    private MathDslBase _mathDsl;

    public FormulaScalarNodeDeserializer(MathDslBase mathDsl)
        => _mathDsl = mathDsl;

    public bool Deserialize(IParser parser, Type expectedType, Func<IParser, Type, object?> nestedObjectDeserializer, out object? value)
    {
        var underlyingType = Nullable.GetUnderlyingType(expectedType) ?? expectedType;

        if (!underlyingType.IsPrimitive)
        {
            value = null;
            return false;
        }

        var typeCode = Type.GetTypeCode(underlyingType);
        switch (typeCode)
        {
            default: {
                value = null;
                return false;
            }
            case TypeCode.SByte:
            case TypeCode.Int16:
            case TypeCode.Int32:
            case TypeCode.Int64: {
                break;
            }

            case TypeCode.Byte:
            case TypeCode.UInt16:
            case TypeCode.UInt32:
            case TypeCode.UInt64: {

                break;
            }

            case TypeCode.Single:
            case TypeCode.Double: {
                break;
            }

            case TypeCode.Decimal: {
                break;
            }
        }

        if (!parser.Accept<Scalar>(out var scalar))
        {
            value = null;
            return false;
        }

        try
        {
            var f = _mathDsl.Parse(scalar.Value).Compile(true);
            value = ((IConvertible)f()).ToType(underlyingType, null);
        }
        catch
        {
            value = null;
            return false;
        }

        parser.MoveNext();
        return true;
    }
}
