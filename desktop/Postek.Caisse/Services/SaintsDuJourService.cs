using System.Globalization;
using System.Text.Json;

namespace Postec.Caisse.Services;

public sealed class SaintsDuJourService
{
    private readonly Dictionary<string, string[]> _saints;

    public SaintsDuJourService()
    {
        _saints = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["09-18"] = new[] { "Ariane", "Sonia", "Vera", "Nadege", "Nadia" },
            ["01-01"] = new[] { "Marie" },
            ["12-25"] = new[] { "Noel" },
        };
    }

    public IReadOnlyList<string> GetSaints(DateTime date)
    {
        var cle = date.ToString("MM-dd", CultureInfo.InvariantCulture);
        return _saints.TryGetValue(cle, out var saints) ? saints : Array.Empty<string>();
    }

    public string Formater(DateTime date)
    {
        var saints = GetSaints(date);
        return saints.Count == 0 ? "Aucun saint du jour" : $"On fete les {string.Join(", ", saints)}";
    }
}
