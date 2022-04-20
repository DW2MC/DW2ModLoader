using System.Globalization;
using System.Text;

namespace DistantWorlds2.ModLoader;

public class TeeTextWriter : TextWriter
{
    private TextWriter? _target1;
    private TextWriter? _target2;
    public readonly bool ForceFlush = true;
    public TeeTextWriter(TextWriter? target1, TextWriter? target2)
    {
        _target1 = target1;
        _target2 = target2;
    }

    public override void Close()
    {
        try { _target1?.Close(); }
        catch { _target1 = null; }
        try { _target2?.Close(); }
        catch { _target2 = null; }

    }

    protected override void Dispose(bool disposing)
    {
        try { _target1?.Dispose(); }
        catch { _target1 = null; }
        try { _target2?.Dispose(); }
        catch { _target2 = null; }
    }

    public override void Flush()
    {
        try { _target1?.Flush(); }
        catch { _target1 = null; }
        try { _target2?.Flush(); }
        catch { _target2 = null; }
    }

    public override void Write(char value)
    {
        try { _target1?.Write(value); }
        catch { _target1 = null; }
        try { _target2?.Write(value); }
        catch { _target2 = null; }
        if (ForceFlush) Flush();
    }

    public override void Write(char[] buffer)
    {
        try { _target1?.Write(buffer); }
        catch { _target1 = null; }
        try { _target2?.Write(buffer); }
        catch { _target2 = null; }
        if (ForceFlush) Flush();
    }

    public override void Write(char[] buffer, int index, int count)
    {
        try { _target1?.Write(buffer, index, count); }
        catch { _target1 = null; }
        try { _target2?.Write(buffer, index, count); }
        catch { _target2 = null; }
        if (ForceFlush) Flush();
    }

    public override void Write(bool value)
    {
        try { _target1?.Write(value); }
        catch { _target1 = null; }
        try { _target2?.Write(value); }
        catch { _target2 = null; }
        if (ForceFlush) Flush();
    }

    public override void Write(int value)
    {
        try { _target1?.Write(value); }
        catch { _target1 = null; }
        try { _target2?.Write(value); }
        catch { _target2 = null; }
        if (ForceFlush) Flush();
    }

    public override void Write(uint value)
    {
        try { _target1?.Write(value); }
        catch { _target1 = null; }
        try { _target2?.Write(value); }
        catch { _target2 = null; }
        if (ForceFlush) Flush();
    }

    public override void Write(long value)
    {
        try { _target1?.Write(value); }
        catch { _target1 = null; }
        try { _target2?.Write(value); }
        catch { _target2 = null; }
        if (ForceFlush) Flush();
    }

    public override void Write(ulong value)
    {
        try { _target1?.Write(value); }
        catch { _target1 = null; }
        try { _target2?.Write(value); }
        catch { _target2 = null; }
        if (ForceFlush) Flush();
    }

    public override void Write(float value)
    {
        try { _target1?.Write(value); }
        catch { _target1 = null; }
        try { _target2?.Write(value); }
        catch { _target2 = null; }
        if (ForceFlush) Flush();
    }

    public override void Write(double value)
    {
        try { _target1?.Write(value); }
        catch { _target1 = null; }
        try { _target2?.Write(value); }
        catch { _target2 = null; }
        if (ForceFlush) Flush();
    }

    public override void Write(decimal value)
    {
        try { _target1?.Write(value); }
        catch { _target1 = null; }
        try { _target2?.Write(value); }
        catch { _target2 = null; }
        if (ForceFlush) Flush();
    }

    public override void Write(string value)
    {
        try { _target1?.Write(value); }
        catch { _target1 = null; }
        try { _target2?.Write(value); }
        catch { _target2 = null; }
        if (ForceFlush) Flush();
    }

    public override void Write(object value)
    {
        try { _target1?.Write(value); }
        catch { _target1 = null; }
        try { _target2?.Write(value); }
        catch { _target2 = null; }
        if (ForceFlush) Flush();
    }

    public override void Write(string format, object arg0)
    {
        try { _target1?.Write(format, arg0); }
        catch { _target1 = null; }
        try { _target2?.Write(format, arg0); }
        catch { _target2 = null; }
        if (ForceFlush) Flush();
    }

    public override void Write(string format, object arg0, object arg1)
    {
        try { _target1?.Write(format, arg0, arg1); }
        catch { _target1 = null; }
        try { _target2?.Write(format, arg0, arg1); }
        catch { _target2 = null; }
        if (ForceFlush) Flush();
    }

    public override void Write(string format, object arg0, object arg1, object arg2)
    {
        try { _target1?.Write(format, arg0, arg1, arg2); }
        catch { _target1 = null; }
        try { _target2?.Write(format, arg0, arg1, arg2); }
        catch { _target2 = null; }
        if (ForceFlush) Flush();
    }

    public override void Write(string format, params object[] arg)
    {
        try { _target1?.Write(format, arg); }
        catch { _target1 = null; }
        try { _target2?.Write(format, arg); }
        catch { _target2 = null; }
        if (ForceFlush) Flush();
    }

    public override void WriteLine()
    {
        try { _target1?.WriteLine(); }
        catch { _target1 = null; }
        try { _target2?.WriteLine(); }
        catch { _target2 = null; }
        if (ForceFlush) Flush();
    }

    public override void WriteLine(char value)
    {
        try { _target1?.WriteLine(value); }
        catch { _target1 = null; }
        try { _target2?.WriteLine(value); }
        catch { _target2 = null; }
        if (ForceFlush) Flush();
    }

    public override void WriteLine(char[] buffer)
    {
        try { _target1?.WriteLine(buffer); }
        catch { _target1 = null; }
        try { _target2?.WriteLine(buffer); }
        catch { _target2 = null; }
        if (ForceFlush) Flush();
    }

    public override void WriteLine(char[] buffer, int index, int count)
    {
        try { _target1?.WriteLine(buffer, index, count); }
        catch { _target1 = null; }
        try { _target2?.WriteLine(buffer, index, count); }
        catch { _target2 = null; }
        if (ForceFlush) Flush();
    }

    public override void WriteLine(bool value)
    {
        try { _target1?.WriteLine(value); }
        catch { _target1 = null; }
        try { _target2?.WriteLine(value); }
        catch { _target2 = null; }
        if (ForceFlush) Flush();
    }

    public override void WriteLine(int value)
    {
        try { _target1?.WriteLine(value); }
        catch { _target1 = null; }
        try { _target2?.WriteLine(value); }
        catch { _target2 = null; }
        if (ForceFlush) Flush();
    }

    public override void WriteLine(uint value)
    {
        try { _target1?.WriteLine(value); }
        catch { _target1 = null; }
        try { _target2?.WriteLine(value); }
        catch { _target2 = null; }
        if (ForceFlush) Flush();
    }

    public override void WriteLine(long value)
    {
        try { _target1?.WriteLine(value); }
        catch { _target1 = null; }
        try { _target2?.WriteLine(value); }
        catch { _target2 = null; }
        if (ForceFlush) Flush();
    }

    public override void WriteLine(ulong value)
    {
        try { _target1?.WriteLine(value); }
        catch { _target1 = null; }
        try { _target2?.WriteLine(value); }
        catch { _target2 = null; }
        if (ForceFlush) Flush();
    }

    public override void WriteLine(float value)
    {
        try { _target1?.WriteLine(value); }
        catch { _target1 = null; }
        try { _target2?.WriteLine(value); }
        catch { _target2 = null; }
        if (ForceFlush) Flush();
    }

    public override void WriteLine(double value)
    {
        try { _target1?.WriteLine(value); }
        catch { _target1 = null; }
        try { _target2?.WriteLine(value); }
        catch { _target2 = null; }
        if (ForceFlush) Flush();
    }

    public override void WriteLine(decimal value)
    {
        try { _target1?.WriteLine(value); }
        catch { _target1 = null; }
        try { _target2?.WriteLine(value); }
        catch { _target2 = null; }
        if (ForceFlush) Flush();
    }

    public override void WriteLine(string value)
    {
        try
        {
            _target1?.WriteLine(value);
        }
        catch (IOException)
        {
            _target1 = null;
        }
        try { _target2?.WriteLine(value); }
        catch { _target2 = null; }
        if (ForceFlush) Flush();
    }

    public override void WriteLine(object value)
    {
        try { _target1?.WriteLine(value); }
        catch { _target1 = null; }
        try { _target2?.WriteLine(value); }
        catch { _target2 = null; }
        if (ForceFlush) Flush();
    }

    public override void WriteLine(string format, object arg0)
    {
        try { _target1?.WriteLine(format, arg0); }
        catch { _target1 = null; }
        try { _target2?.WriteLine(format, arg0); }
        catch { _target2 = null; }
        if (ForceFlush) Flush();
    }

    public override void WriteLine(string format, object arg0, object arg1)
    {
        try { _target1?.WriteLine(format, arg0, arg1); }
        catch { _target1 = null; }
        try { _target2?.WriteLine(format, arg0, arg1); }
        catch { _target2 = null; }
        if (ForceFlush) Flush();
    }

    public override void WriteLine(string format, object arg0, object arg1, object arg2)
    {
        try { _target1?.WriteLine(format, arg0, arg1, arg2); }
        catch { _target1 = null; }
        try { _target2?.WriteLine(format, arg0, arg1, arg2); }
        catch { _target2 = null; }
        if (ForceFlush) Flush();
    }

    public override void WriteLine(string format, params object[] arg)
    {
        try { _target1?.WriteLine(format, arg); }
        catch { _target1 = null; }
        try { _target2?.WriteLine(format, arg); }
        catch { _target2 = null; }
        if (ForceFlush) Flush();
    }

    private async Task TryAsync(Func<Task?> f, Action fail)
    {
#pragma warning disable CS8602
        try { await f(); }
        catch { fail(); }
#pragma warning restore CS8602
    }

    public override Task WriteAsync(char value)
    {
        var task = Task.WhenAll(
            TryAsync(() => _target1?.WriteAsync(value), () => _target1 = null),
            TryAsync(() => _target2?.WriteAsync(value), () => _target1 = null)
        );
        return ForceFlush ? task.ContinueWith(_ => FlushAsync()) : task;
    }

    public override Task WriteAsync(string value)
    {
        var task = Task.WhenAll(
            TryAsync(() => _target1?.WriteAsync(value), () => _target1 = null),
            TryAsync(() => _target2?.WriteAsync(value), () => _target1 = null)
        );
        return ForceFlush ? task.ContinueWith(_ => FlushAsync()) : task;
    }

    public override Task WriteAsync(char[] buffer, int index, int count)
    {
        var task = Task.WhenAll(
            TryAsync(() => _target1?.WriteAsync(buffer, index, count), () => _target1 = null),
            TryAsync(() => _target2?.WriteAsync(buffer, index, count), () => _target1 = null)
        );
        return ForceFlush ? task.ContinueWith(_ => FlushAsync()) : task;
    }

    public override Task WriteLineAsync(char value)
    {
        var task = Task.WhenAll(
            TryAsync(() => _target1?.WriteLineAsync(value), () => _target1 = null),
            TryAsync(() => _target2?.WriteLineAsync(value), () => _target1 = null)
        );
        return ForceFlush ? task.ContinueWith(_ => FlushAsync()) : task;
    }

    public override Task WriteLineAsync(string value)
    {
        var task = Task.WhenAll(
            TryAsync(() => _target1?.WriteLineAsync(value), () => _target1 = null),
            TryAsync(() => _target2?.WriteLineAsync(value), () => _target1 = null)
        );
        return ForceFlush ? task.ContinueWith(_ => FlushAsync()) : task;
    }

    public override Task WriteLineAsync(char[] buffer, int index, int count)
    {
        var task = Task.WhenAll(
            TryAsync(() => _target1?.WriteLineAsync(buffer, index, count), () => _target1 = null),
            TryAsync(() => _target2?.WriteLineAsync(buffer, index, count), () => _target1 = null)
        );
        return ForceFlush ? task.ContinueWith(_ => FlushAsync()) : task;
    }

    public override Task WriteLineAsync()
    {
        var task = Task.WhenAll(
            TryAsync(() => _target1?.WriteLineAsync(), () => _target1 = null),
            TryAsync(() => _target2?.WriteLineAsync(), () => _target1 = null)
        );
        return ForceFlush ? task.ContinueWith(_ => FlushAsync()) : task;
    }

    public override Task FlushAsync()
    {
        var task = Task.WhenAll(
            TryAsync(() => _target1?.FlushAsync(), () => _target1 = null),
            TryAsync(() => _target2?.FlushAsync(), () => _target1 = null)
        );
        return ForceFlush ? task.ContinueWith(_ => FlushAsync()) : task;
    }

    public override IFormatProvider FormatProvider
        => _target1?.FormatProvider
            ?? _target2?.FormatProvider
            ?? CultureInfo.InvariantCulture;

    public override Encoding Encoding
        => _target1?.Encoding
            ?? _target2?.Encoding
            ?? Encoding.UTF8;

    public override string NewLine
    {
        get => _target1?.NewLine ?? _target2?.NewLine ?? Environment.NewLine;
        set {
            if (_target1 is not null)
                _target1.NewLine = value;
            if (_target2 is not null)
                _target2.NewLine = value;
        }
    }

    public override string ToString()
        => $"[TeeTextWriter {_target1} {_target2}]";
}
