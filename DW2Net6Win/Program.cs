using System;
using System.IO;
using System.Reflection;

public static class Program
{
    private static readonly Version V6 = new(6, 0, 0, 0);
    private static readonly string BaseDir = AppContext.BaseDirectory;

    static Program()
        => AppDomain.CurrentDomain.AssemblyResolve += (_, eventArgs) => {
            var an = new AssemblyName(eventArgs.Name);
            var name = an.Name!;

            var isSystem = name.StartsWith("System.");

            if (isSystem)
            {
                var v = an.Version;
                if (v is not null && v.CompareTo(V6) >= 0)
                    return null;

                an.Version = V6;
                return Assembly.Load(an);
            }

            var dll = name + ".dll";

            var p = Path.Combine(BaseDir, dll);

            if (File.Exists(dll))
                return Assembly.LoadFile(p);

            return null;
        };

    public static int Main(string[] args)
        => Launcher.Run(args);
}
