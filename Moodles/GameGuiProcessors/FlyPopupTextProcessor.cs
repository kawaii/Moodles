using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Gui.FlyText;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Memory;
using Dalamud.Plugin.Services;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Moodles.Data;
using static Lumina.Data.Parsing.Uld.NodeData;

namespace Moodles.GameGuiProcessors;
public sealed unsafe class FlyPopupTextProcessor : IDisposable
{
    public Queue<FlyPopupTextData> Queue = [];
    public FlyPopupTextData CurrentElement = null;

    public FlyPopupTextProcessor()
    {
        Svc.Framework.Update += this.Framework_Update;
    }

    private void Framework_Update(IFramework framework)
    {
        ProcessPopupText();
        ProcessFlyText();
        if (CurrentElement != null) CurrentElement = null;
        while (Queue.TryDequeue(out var e))
        {
            var target = Svc.Objects.FirstOrDefault(x => x.ObjectId == e.Owner);
            if (target is PlayerCharacter pc)
            {
                PluginLog.Debug($"Processing {e.Status.Title} at {Utils.Frame} for {pc}...");
                CurrentElement = e;
                var isMine = e.Status.Applier == Player.NameWithWorld && e.IsAddition;
                FlyTextKind kind;
                if (e.Status.Type == StatusType.Negative)
                {
                    kind = e.IsAddition ? FlyTextKind.Debuff : FlyTextKind.DebuffFading;
                }
                else
                {
                    kind = e.IsAddition ? FlyTextKind.Buff : FlyTextKind.BuffFading;
                }
                P.Memory.BattleLog_AddToScreenLogWithScreenLogKindHook.Original(target.Address, isMine ? Player.Object.Address : target.Address, kind, 5, 0, 0, 0, 0, 0);
                break;
            }
            else
            {
                PluginLog.Debug($"Skipping {e.Status.Title} for {e.Owner:X8}, not found...");
            }
        }
    }

    void ProcessPopupText()
    {
        if (CurrentElement != null)
        {
            {
                if (TryGetAddonByName<AtkUnitBase>("_PopUpText", out var addon))
                {
                    for (int i = 1; i < addon->UldManager.NodeListCount; i++)
                    {
                        var candidate = addon->UldManager.NodeList[i];
                        if (IsCandidateValid(candidate))
                        {
                            var c = candidate->GetAsAtkComponentNode()->Component;
                            var sestr = new SeStringBuilder().AddText(CurrentElement.IsAddition ? "+ " : "- ").Append(Utils.ParseBBSeString(CurrentElement.Status.Title));
                            c->UldManager.NodeList[1]->GetAsAtkTextNode()->SetText(sestr.Encode());
                            c->UldManager.NodeList[2]->GetAsAtkImageNode()->LoadTexture(Svc.Texture.GetIconPath(CurrentElement.Status.AdjustedIconID), 1);
                            CurrentElement = null;
                            return;
                        }
                    }
                }
            }
        }
    }

    void ProcessFlyText()
    {
        if (CurrentElement != null)
        {
            {
                if (TryGetAddonByName<AtkUnitBase>("_FlyText", out var addon))
                {
                    for (int i = 1; i < addon->UldManager.NodeListCount; i++)
                    {
                        var candidate = addon->UldManager.NodeList[i];
                        if (IsCandidateValid(candidate))
                        {
                            var c = candidate->GetAsAtkComponentNode()->Component;
                            var sestr = new SeStringBuilder().AddText(CurrentElement.IsAddition ? "+ " : "- ").Append(Utils.ParseBBSeString(CurrentElement.Status.Title));
                            c->UldManager.NodeList[1]->GetAsAtkTextNode()->SetText(sestr.Encode());
                            c->UldManager.NodeList[2]->GetAsAtkImageNode()->LoadTexture(Svc.Texture.GetIconPath(CurrentElement.Status.AdjustedIconID), 1);
                            CurrentElement = null;
                            return;
                        }
                    }
                }
            }
        }
    }

    bool IsCandidateValid(AtkResNode* node)
    {
        if (!node->IsVisible) return false;
        var c = node->GetAsAtkComponentNode()->Component;
        if (c->UldManager.NodeListCount < 3 || c->UldManager.NodeListCount > 4) return false;
        if (c->UldManager.NodeList[1]->Type != NodeType.Text) return false;
        if (!c->UldManager.NodeList[1]->IsVisible) return false;
        if (c->UldManager.NodeList[2]->Type != NodeType.Image) return false;
        if (!c->UldManager.NodeList[2]->IsVisible) return false;
        var text = MemoryHelper.ReadSeString(&c->UldManager.NodeList[1]->GetAsAtkTextNode()->NodeText)?.ToString();
        if (text != "+ " && text != "- ") return false;
        return true;
    }

    public void Dispose()
    {
        Svc.Framework.Update -= this.Framework_Update;
    }
}
