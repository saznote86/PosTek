using System.Diagnostics;
using System.IO;

namespace Postec.Caisse.Services;

public sealed record SystemInfo(string Version, string Os, string Processus, string Memoire, string Disque);

public sealed class SystemInfoService
{
    public SystemInfo Lire()
    {
        var processus = Process.GetCurrentProcess();
        var disque = new DriveInfo(Path.GetPathRoot(AppContext.BaseDirectory) ?? "C:\\");
        return new SystemInfo(
            typeof(SystemInfoService).Assembly.GetName().Version?.ToString() ?? "1.0.0",
            Environment.OSVersion.VersionString,
            processus.Id.ToString(),
            $"{processus.WorkingSet64 / 1024 / 1024} Mo",
            $"{disque.AvailableFreeSpace / 1024 / 1024 / 1024} Go libres");
    }
}
