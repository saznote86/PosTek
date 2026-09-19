namespace Postec.Caisse.Services;

public sealed class PrinterService
{
    public IReadOnlyList<string> GetInstalledPrinters() => ListerImprimantes();
    public string GetDefaultPrinter() =>
        Microsoft.Win32.Registry.GetValue(
            @"HKEY_CURRENT_USER\Software\Microsoft\Windows NT\CurrentVersion\Windows",
            "Device", "")?.ToString()?.Split(',')[0] ?? "";

    public IReadOnlyList<string> ListerImprimantes()
    {
        try
        {
            using var cle = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows NT\CurrentVersion\Devices");
            return cle is null
                ? Array.Empty<string>()
                : cle.GetValueNames().OrderBy(n => n).ToList();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    public bool ImprimanteDisponible(string? nom) =>
        !string.IsNullOrWhiteSpace(nom) && ListerImprimantes().Contains(nom, StringComparer.OrdinalIgnoreCase);
}
