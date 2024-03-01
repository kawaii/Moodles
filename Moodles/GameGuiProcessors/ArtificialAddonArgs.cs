using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moodles.GameGuiProcessors;
public unsafe class ArtificialAddonArgs : AddonArgs
{
    public ArtificialAddonArgs(AtkUnitBase* addon)
    {
        this.Addon = (nint)addon;
    }

    public override AddonArgsType Type => AddonArgsType.RequestedUpdate;

}
