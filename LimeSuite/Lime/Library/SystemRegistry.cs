using Microsoft.Win32;
using System;
using System.Runtime.Versioning;
using System.Security.Permissions;

/// <summary>
/// Windows System Registry access
/// </summary>
[SupportedOSPlatform("windows")]
public static class SystemRegistry
{
    private const string AutoRunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";


    /// <summary>
    /// Check if the given application is set for auto-run.
    /// </summary>
    /// <param name="name">Application name</param>
    /// <param name="executablePath">Application path expected in the autorun</param>
    /// <returns>true if the application is setup for auto-run</returns>
    public static bool GetAutoRun(string name, string executablePath = null)
    {
        using (RegistryKey key = Registry.CurrentUser.OpenSubKey(AutoRunKeyPath, false))
        {
            string value = key.GetValue(name) as string;
            if (!String.IsNullOrEmpty(executablePath)) return value == executablePath;
            return !String.IsNullOrEmpty(value);
        }
    }

    /// <summary>
    /// Setup (add/change or remove) an auto-run application in the system registry
    /// </summary>
    /// <param name="name">Application name</param>
    /// <param name="executablePath">Executable Path for program. Set to null or empty-string to remove the Autorun.</param>
    public static void SetAutoRun(string name, string executablePath)
    {
        using (RegistryKey key = Registry.CurrentUser.OpenSubKey(AutoRunKeyPath, true))
        {
            if (String.IsNullOrEmpty(executablePath))
            {
                if (key != null)  key.DeleteValue(name);
            }
            else
            {
                key.SetValue(name, executablePath);
            }
        }
    }

}
