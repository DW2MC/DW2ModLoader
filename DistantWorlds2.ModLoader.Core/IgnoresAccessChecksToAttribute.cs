// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Allows the current assembly to access the internal types of a specified assembly that are ordinarily invisible.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class IgnoresAccessChecksToAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IgnoresAccessChecksToAttribute" /> class with the name of the specified assembly.
        /// </summary>
        /// <param name="assemblyName">The name of the specified assembly.</param>
        public IgnoresAccessChecksToAttribute(string assemblyName)
            => AssemblyName = assemblyName;

        /// <summary>
        /// Gets the name of the specified assembly whose access checks against the current assembly are ignored .
        /// </summary>
        /// <value>A string that represents the name of the specified assembly.</value>
        public string AssemblyName { get; }
    }
}
