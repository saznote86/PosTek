using Postec.Caisse.Services;
using Xunit;

namespace Postec.Caisse.Tests;

public sealed class ServicesTests
{
    [Fact]
    public void SaintsDuJour_ConnaitLe18Septembre()
    {
        var service = new SaintsDuJourService();
        var saints = service.GetSaints(new DateTime(2026, 9, 18));
        Assert.Contains("Ariane", saints);
        Assert.Contains("Nadia", saints);
    }

    [Fact]
    public void PortsSerie_ExposeUnJeuTactileDePorts()
    {
        var ports = new SerialPortService().ListerPorts();
        Assert.Contains("COM1", ports);
        Assert.Contains("COM16", ports);
    }
}
