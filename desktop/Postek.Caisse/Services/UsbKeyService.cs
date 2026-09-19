using System.IO;

namespace Postec.Caisse.Services;

public sealed class UsbKeyService
{
    public IReadOnlyList<string> GetUsbDrives() => ListerLecteurs();
    public void WritePasswordToUsb(string lecteur, string secret) => Ecrire(lecteur, secret);
    public void DeletePasswordFromUsb(string lecteur) => Effacer(lecteur);

    public IReadOnlyList<string> ListerLecteurs() =>
        DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Removable).Select(d => d.Name).ToList();

    public void Ecrire(string lecteur, string secret)
    {
        if (string.IsNullOrWhiteSpace(lecteur) || string.IsNullOrEmpty(secret))
            throw new ArgumentException("Lecteur et secret obligatoires.");
        var chemin = Path.Combine(lecteur, "POSTEC.key");
        File.WriteAllText(chemin, Convert.ToBase64String(
            System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(secret))));
    }

    public void Effacer(string lecteur)
    {
        var chemin = Path.Combine(lecteur, "POSTEC.key");
        if (File.Exists(chemin)) File.Delete(chemin);
    }
}
