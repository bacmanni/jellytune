namespace JellyTune.Gnome.Helpers;

using System;
using System.Runtime.InteropServices;

public static class SystemChecker
{
    private static readonly Version RequiredAdwVersion = new Version(1, 8, 0);
    private static readonly Version RequiredGtkVersion = new Version(4, 10, 0);

    // Native P/Invoke bindings safely encapsulated
    [DllImport("libadwaita-1.so.0", EntryPoint = "adw_get_major_version")]
    private static extern uint adw_get_major_version();
    [DllImport("libadwaita-1.so.0", EntryPoint = "adw_get_minor_version")]
    private static extern uint adw_get_minor_version();
    [DllImport("libadwaita-1.so.0", EntryPoint = "adw_get_micro_version")]
    private static extern uint adw_get_micro_version();

    [DllImport("libgtk-4.so.1", EntryPoint = "gtk_get_major_version")]
    private static extern uint gtk_get_major_version();
    [DllImport("libgtk-4.so.1", EntryPoint = "gtk_get_minor_version")]
    private static extern uint gtk_get_minor_version();
    [DllImport("libgtk-4.so.1", EntryPoint = "gtk_get_micro_version")]
    private static extern uint gtk_get_micro_version();

    public static bool CheckRequirements(out string errorMessage)
    {
        errorMessage = string.Empty;

        // Check Libadwaita
        if (!TryGetVersion(adw_get_major_version, adw_get_minor_version, adw_get_micro_version, out var adwVersion))
        {
            errorMessage = BuildError("Libadwaita (libadwaita-1.so)", RequiredAdwVersion, "Not Installed");
            return false;
        }
        if (adwVersion < RequiredAdwVersion)
        {
            errorMessage = BuildError("Libadwaita", RequiredAdwVersion, adwVersion.ToString());
            return false;
        }

        // Check GTK4
        if (!TryGetVersion(gtk_get_major_version, gtk_get_minor_version, gtk_get_micro_version, out var gtkVersion))
        {
            errorMessage = BuildError("GTK4 (libgtk-4.so)", RequiredGtkVersion, "Not Installed");
            return false;
        }
        if (gtkVersion < RequiredGtkVersion)
        {
            errorMessage = BuildError("GTK4", RequiredGtkVersion, gtkVersion.ToString());
            return false;
        }

        return true;
    }

    private delegate uint VersionFunc();

    private static bool TryGetVersion(VersionFunc major, VersionFunc minor, VersionFunc micro, out Version version)
    {
        version = new Version(0, 0, 0);
        try
        {
            version = new Version((int)major(), (int)minor(), (int)micro());
            return true;
        }
        catch
        {
            return false; // Library not found or entry point missing
        }
    }

    private static string BuildError(string component, Version required, string detected)
    {
        return $"System requirement check failed for: {component}\n\n" +
               $"• Required Version: {required}\n" +
               $"• Detected Version: {detected}\n\n" +
               $"Please update your system packages or use a supported environment.";
    }
}