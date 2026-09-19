using System.Collections.Generic;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace Postec.Caisse.Views.Controls;

public sealed class AutomationContentControl : ContentControl
{
    protected override AutomationPeer OnCreateAutomationPeer() =>
        new AutomationContentControlAutomationPeer(this);

    public void RafraichirAutomationPeer()
    {
        if (UIElementAutomationPeer.CreatePeerForElement(this)
            is AutomationContentControlAutomationPeer peer)
            peer.RafraichirContenu();
    }
}

internal sealed class AutomationContentControlAutomationPeer : FrameworkElementAutomationPeer
{
    public AutomationContentControlAutomationPeer(AutomationContentControl owner)
        : base(owner)
    {
    }

    protected override string GetClassNameCore() => nameof(AutomationContentControl);

    protected override string GetNameCore() =>
        AutomationProperties.GetName(Owner) ?? base.GetNameCore();

    protected override string GetAutomationIdCore() =>
        AutomationProperties.GetAutomationId(Owner);

    protected override AutomationControlType GetAutomationControlTypeCore() =>
        AutomationControlType.Group;

    protected override bool IsControlElementCore() => true;

    protected override bool IsContentElementCore() => true;

    protected override List<AutomationPeer>? GetChildrenCore()
    {
        var owner = (AutomationContentControl)Owner;
        if (owner.Content is not UIElement contenu)
            return null;

        var peer = CreatePeerForElement(contenu);
        return peer is null ? null : new List<AutomationPeer> { peer };
    }

    public void RafraichirContenu()
    {
        RaiseAutomationEvent(AutomationEvents.StructureChanged);
    }
}
