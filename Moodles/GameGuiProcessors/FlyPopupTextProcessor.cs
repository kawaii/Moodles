using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Gui.FlyText;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Memory;
using Dalamud.Plugin.Services;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.GeneratedSheets;
using Moodles.Data;

namespace Moodles.GameGuiProcessors;
public sealed unsafe class FlyPopupTextProcessor : IDisposable
{
    private List<FlyPopupTextData> Queue = [];
    public FlyPopupTextData CurrentElement = null;
    public Dictionary<uint, IconStatusData> StatusData = [];

    public FlyPopupTextProcessor()
    {
        foreach(var x in Svc.Data.GetExcelSheet<Status>())
        {
            var baseData = new IconStatusData(x.RowId, x.Name.ExtractText(), 0);
            StatusData[x.Icon] = baseData;
            for (int i = 2; i <= x.MaxStacks; i++)
            {
                StatusData[(uint)(x.Icon + i - 1)] = baseData with { StackCount = (uint)i };
            }
        }
        Svc.Framework.Update += this.Framework_Update;
    }

    public void Enqueue(FlyPopupTextData data)
    {
        if (C.EnableFlyPopupText)
        {
            Queue.Add(data);
        }
    }

    private void Framework_Update(IFramework framework)
    {
        ProcessPopupText();
        ProcessFlyText();
        if (CurrentElement != null) CurrentElement = null;
        if(Queue.Count > C.FlyPopupTextLimit)
        {
            PluginLog.Warning($"FlyPopupTextProcessor Queue is too large! Trimming to {C.FlyPopupTextLimit} closest entities.");
            var n = Queue.RemoveAll(x => Svc.Objects.FirstOrDefault(z => z.OwnerId == x.Owner) is not IPlayerCharacter);
            if(n > 0) PluginLog.Information($"  Removed {n} non-player entities");
            Queue = Queue.OrderBy(x => Vector3.DistanceSquared(Player.Object.Position, Svc.Objects.First(z => z.OwnerId == x.Owner).Position)).Take(C.FlyPopupTextLimit).ToList();
        }
        while (Queue.TryDequeue(out var e))
        {
            IPlayerCharacter? target = null;
            for (int i = 0; i < Svc.Objects.Length; i++)
            {
                var cur = Svc.Objects[i];
                if (cur == null) continue;
                if (cur.EntityId != e.Owner) continue;
                if (cur is not IPlayerCharacter pChara) continue;

                target = pChara;
                break;
            }

            if (target != null)
            {
                PluginLog.Debug($"Processing {e.Status.Title} at {Utils.Frame} for {target}...");
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
                if (StatusData.TryGetValue((uint)e.Status.AdjustedIconID, out var data))
                {
                    P.Memory.BattleLog_AddToScreenLogWithScreenLogKindHook.Original(target.Address, isMine ? Player.Object.Address : target.Address, kind, 5, 0, 0, (int)data.StatusId, (int)data.StackCount, 0);
                }
                else
                {
                    PluginLog.Error($"[FlyPopupTextProcessor] Error retrieving data for icon {e.Status.IconID}, please report to developer.");
                }
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
                            var sestr = new SeStringBuilder().AddText(CurrentElement.IsAddition ? "+ " : "- ").Append(CurrentElement.Status.Title + " test");//.Append(Utils.ParseBBSeString(CurrentElement.Status.Title));
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
        if (!node->IsVisible()) return false;
        var c = node->GetAsAtkComponentNode()->Component;
        if (c->UldManager.NodeListCount < 3 || c->UldManager.NodeListCount > 4) return false;
        if (c->UldManager.NodeList[1]->Type != NodeType.Text) return false;
        if (!c->UldManager.NodeList[1]->IsVisible()) return false;
        if (c->UldManager.NodeList[2]->Type != NodeType.Image) return false;
        if (!c->UldManager.NodeList[2]->IsVisible()) return false;
        var text = MemoryHelper.ReadSeString(&c->UldManager.NodeList[1]->GetAsAtkTextNode()->NodeText)?.ExtractText();
        if (!text.StartsWith('-') && !text.StartsWith('+')) return false;
        if (StatusData.TryGetValue((uint)CurrentElement.Status.AdjustedIconID, out var data))
        {
            if (!text.Contains(data.Name)) return false;
        }
        else
        {
            return false;
        }
        return true;
    }

    public void Dispose()
    {
        Svc.Framework.Update -= this.Framework_Update;
    }
}
