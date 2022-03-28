using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Cysharp.Text;
using JetBrains.Annotations;

namespace DistantWorlds2.ModLoader;

[PublicAPI]
public static class StringUtils
{
    private static readonly Regex EscapeSequencesRegex = new(
        LanguageHelpers.Rx(@"\\[abfnrtv?""'\\]|\\[0-3]?[0-7]{1,2}|\\x[0-9a-fA-F]{1,4}|\\u[0-9a-fA-F]{4}|\\U[0-9a-fA-F]{8}|."),
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
    public static string Unescape(string s)
    {

        var mc = EscapeSequencesRegex.Matches(s, 0);
        var sb = ZString.CreateStringBuilder();
        try
        {
            sb.TryGrow(16 * ((s.Length + 15) / 16));
            foreach (Match m in mc)
            {
                if (m.Length == 1)
                    sb.Append(m.Value);
                else
                    switch (m.Value[1])
                    {
                        // @formatter:off
                        case >= '0' and <= '7': sb.Append((char)Convert.ToInt32(m.Value.Substring(1), 8)); break;
                        case 'u' or 'x': sb.Append((char)Convert.ToInt32(m.Value.Substring(2), 16)); break;
                        case 'U': sb.AppendFromUtf32(Convert.ToInt32(m.Value.Substring(2), 16)); break;
                        case 'a': sb.Append('\a'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'v': sb.Append('\v'); break;
                        default: sb.Append(s, m.Index, m.Length); break;
                        // @formatter:on
                    }
            }

            return sb.ToString();
        }
        finally
        {
            sb.Dispose();
        }
    }
    public static void AppendFromUtf32(ref this Utf16ValueStringBuilder sb, int utf32)
    {
        if (utf32 is < 0 or > 0x10FFFF or >= 0xD800 and <= 0xDFFF)
            throw new ArgumentOutOfRangeException(nameof(utf32));

        if (utf32 >= 0x10000)
        {
            utf32 -= 0x10000;
            sb.Append((char)(utf32 / 0x400 + 0xD800));
            sb.Append((char)(utf32 % 0x400 + 0xDC00));
            return;
        }

        sb.Append((char)utf32);
    }

    public static string ToHexString(this Span<byte> data)
        => ToHexString((ReadOnlySpan<byte>)data);
    public static string ToHexString(this byte[] data)
        => ToHexString((ReadOnlySpan<byte>)data);
    public static unsafe string ToHexString(this ReadOnlySpan<byte> data)
    {
        var dataLen = data.Length;
        var strLen = data.Length * 2;
        var newStr = new string('\0', strLen);
        fixed (char* pStr = newStr)
        {
            Span<char> chars = new(pStr, strLen);
            var ints = MemoryMarshal.Cast<char, int>(chars);
            for (var i = 0; i < dataLen; ++i)
            {
                var b = data[i];
                var nibLo = b >> 4;
                var isDigLo = (nibLo - 10) >> 31;
                var chLo = 55 + nibLo + (isDigLo & -7);
                var nibHi = b & 0xF;
                var isDigHi = (nibHi - 10) >> 31;
                var chHi = 55 + nibHi + (isDigHi & -7);
                ints[i] = (chHi << 16) | chLo;
            }
        }
        return newStr;
    }
}
