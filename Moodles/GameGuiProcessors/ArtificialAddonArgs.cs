using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System.Reflection;

namespace Moodles.GameGuiProcessors;
public unsafe class ArtificialAddonArgs : AddonArgs
{
    public ArtificialAddonArgs(AtkUnitBase* addon)
    {
        // This is a terrible horrible awful hack! (Does it work? Nobody knows!)
        var prop = typeof(AddonArgs).GetProperty("Addon", BindingFlags.Instance | BindingFlags.NonPublic);
        prop?.SetValue(Addon, (nint)addon);
    }

    public override AddonArgsType Type => AddonArgsType.RequestedUpdate;

}
