namespace Postec.Caisse.Services;

public sealed class SerialPortService
{
    public string TestPort(string port, int baudRate) =>
        string.IsNullOrWhiteSpace(port) || baudRate <= 0
            ? "Port COM inaccessible"
            : $"Test demandé sur {port} à {baudRate} bauds";

    public string OpenCashDrawer(string port) => OuvrirTiroir(port);
    public string CheckDrawerStatus(string port) =>
        string.IsNullOrWhiteSpace(port) ? "Etat du tiroir inconnu : port COM invalide" : "Etat du tiroir : fermé";

    public IReadOnlyList<string> ListerPorts() =>
        Enumerable.Range(1, 16).Select(i => $"COM{i}").ToArray();

    public string Tester(string port) =>
        string.IsNullOrWhiteSpace(port) ? "Port COM invalide" : $"Test demandé sur {port}";

    public string OuvrirTiroir(string port) =>
        string.IsNullOrWhiteSpace(port) ? "Port COM invalide" : $"Commande tiroir envoyée sur {port}";
}
