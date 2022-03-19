using System.Runtime.Remoting;
using System.Text;

namespace DistantWorlds2.ModLoader;

public class TeeTextWriter : TextWriter
{
    private readonly TextWriter _target1;
    private readonly TextWriter _target2;
    public TeeTextWriter(TextWriter target1, TextWriter target2)
    {
        _target1 = target1;
        _target2 = target2;
    }

    public override void Close()
    {
        _target1.Close();
        _target2.Close();
    }

    protected override void Dispose(bool disposing)
    {
        _target1.Dispose();
        _target2.Dispose();
    }

    public override void Flush()
    {
        _target1.Flush();
        _target2.Flush();
    }

    public override void Write(char value)
    {
        _target1.Write(value);
        _target2.Write(value);
    }

    public override void Write(char[] buffer)
    {
        _target1.Write(buffer);
        _target2.Write(buffer);
    }

    public override void Write(char[] buffer, int index, int count)
    {
        _target1.Write(buffer, index, count);
        _target2.Write(buffer, index, count);
    }

    public override void Write(bool value)
    {
        _target1.Write(value);
        _target2.Write(value);
    }

    public override void Write(int value)
    {
        _target1.Write(value);
        _target2.Write(value);
    }

    public override void Write(uint value)
    {
        _target1.Write(value);
        _target2.Write(value);
    }

    public override void Write(long value)
    {
        _target1.Write(value);
        _target2.Write(value);
    }

    public override void Write(ulong value)
    {
        _target1.Write(value);
        _target2.Write(value);
    }

    public override void Write(float value)
    {
        _target1.Write(value);
        _target2.Write(value);
    }

    public override void Write(double value)
    {
        _target1.Write(value);
        _target2.Write(value);
    }

    public override void Write(decimal value)
    {
        _target1.Write(value);
        _target2.Write(value);
    }

    public override void Write(string value)
    {
        _target1.Write(value);
        _target2.Write(value);
    }

    public override void Write(object value)
    {
        _target1.Write(value);
        _target2.Write(value);
    }

    public override void Write(string format, object arg0)
    {
        _target1.Write(format, arg0);
        _target2.Write(format, arg0);
    }

    public override void Write(string format, object arg0, object arg1)
    {
        _target1.Write(format, arg0, arg1);
        _target2.Write(format, arg0, arg1);
    }

    public override void Write(string format, object arg0, object arg1, object arg2)
    {
        _target1.Write(format, arg0, arg1, arg2);
        _target2.Write(format, arg0, arg1, arg2);
    }

    public override void Write(string format, params object[] arg)
    {
        _target1.Write(format, arg);
        _target2.Write(format, arg);
    }

    public override void WriteLine()
    {
        _target1.WriteLine();
        _target2.WriteLine();
    }

    public override void WriteLine(char value)
    {
        _target1.WriteLine(value);
        _target2.WriteLine(value);
    }

    public override void WriteLine(char[] buffer)
    {
        _target1.WriteLine(buffer);
        _target2.WriteLine(buffer);
    }

    public override void WriteLine(char[] buffer, int index, int count)
    {
        _target1.WriteLine(buffer, index, count);
        _target2.WriteLine(buffer, index, count);
    }

    public override void WriteLine(bool value)
    {
        _target1.WriteLine(value);
        _target2.WriteLine(value);
    }

    public override void WriteLine(int value)
    {
        _target1.WriteLine(value);
        _target2.WriteLine(value);
    }

    public override void WriteLine(uint value)
    {
        _target1.WriteLine(value);
        _target2.WriteLine(value);
    }

    public override void WriteLine(long value)
    {
        _target1.WriteLine(value);
        _target2.WriteLine(value);
    }

    public override void WriteLine(ulong value)
    {
        _target1.WriteLine(value);
        _target2.WriteLine(value);
    }

    public override void WriteLine(float value)
    {
        _target1.WriteLine(value);
        _target2.WriteLine(value);
    }

    public override void WriteLine(double value)
    {
        _target1.WriteLine(value);
        _target2.WriteLine(value);
    }

    public override void WriteLine(decimal value)
    {
        _target1.WriteLine(value);
        _target2.WriteLine(value);
    }

    public override void WriteLine(string value)
    {
        _target1.WriteLine(value);
        _target2.WriteLine(value);
    }

    public override void WriteLine(object value)
    {
        _target1.WriteLine(value);
        _target2.WriteLine(value);
    }

    public override void WriteLine(string format, object arg0)
    {
        _target1.WriteLine(format, arg0);
        _target2.WriteLine(format, arg0);
    }

    public override void WriteLine(string format, object arg0, object arg1)
    {
        _target1.WriteLine(format, arg0, arg1);
        _target2.WriteLine(format, arg0, arg1);
    }

    public override void WriteLine(string format, object arg0, object arg1, object arg2)
    {
        _target1.WriteLine(format, arg0, arg1, arg2);
        _target2.WriteLine(format, arg0, arg1, arg2);
    }

    public override void WriteLine(string format, params object[] arg)
    {
        _target1.WriteLine(format, arg);
        _target2.WriteLine(format, arg);
    }

    public override Task WriteAsync(char value)
        => Task.WhenAll(
            _target1.WriteAsync(value),
            _target2.WriteAsync(value)
        );

    public override Task WriteAsync(string value)
        => Task.WhenAll(
            _target1.WriteAsync(value),
            _target2.WriteAsync(value)
        );

    public override Task WriteAsync(char[] buffer, int index, int count)
        => Task.WhenAll(
            _target1.WriteAsync(buffer, index, count),
            _target2.WriteAsync(buffer, index, count)
        );

    public override Task WriteLineAsync(char value)
        => Task.WhenAll(
            _target1.WriteLineAsync(value),
            _target2.WriteLineAsync(value)
        );
    public override Task WriteLineAsync(string value)
        => Task.WhenAll(
            _target1.WriteLineAsync(value),
            _target2.WriteLineAsync(value)
        );
    public override Task WriteLineAsync(char[] buffer, int index, int count)
        => Task.WhenAll(
            _target1.WriteLineAsync(buffer, index, count),
            _target2.WriteLineAsync(buffer, index, count)
        );
    public override Task WriteLineAsync()
        => Task.WhenAll(
            _target1.WriteLineAsync(),
            _target2.WriteLineAsync()
        );
    public override Task FlushAsync()
        => Task.WhenAll(
            _target1.FlushAsync(),
            _target2.FlushAsync()
        );
    public override IFormatProvider FormatProvider => _target1.FormatProvider;
    public override Encoding Encoding => _target1.Encoding;

    public override string NewLine
    {
        get => _target1.NewLine;
        set => _target1.NewLine = value;
    }

    public override object? InitializeLifetimeService()
        => _target1.InitializeLifetimeService();
    public override ObjRef CreateObjRef(Type requestedType)
        => _target1.CreateObjRef(requestedType);
    public override string ToString()
        => $"[TeeTextWriter {_target1} {_target2}]";
}
