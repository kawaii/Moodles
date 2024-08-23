using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Moodles.GameGuiProcessors;
public unsafe class ArtificialAddonArgs : AddonArgs
{
    public ArtificialAddonArgs(AtkUnitBase* addon)
    {
        Addon = (nint)addon;
    }

    public override AddonArgsType Type => AddonArgsType.RequestedUpdate;

}
