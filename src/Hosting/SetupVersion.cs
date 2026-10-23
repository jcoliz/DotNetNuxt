using System;
using System.Reflection;

namespace DotNetNuxt.Hosting;

public static partial class VersionHelpers
{
    /// <summary>
    /// Get app version from a specific assembly.
    /// </summary>
    /// <param name="assembly"></param>
    /// <returns>Informational version, or "unknown"</returns>
    public static string GetVersion(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        // Get app version from assembly attribute
        return assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? "unknown";
    }
}
