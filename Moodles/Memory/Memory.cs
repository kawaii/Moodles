using ECommons.EzHookManager;

namespace Moodles;
public unsafe partial class Memory : IDisposable
{
    public delegate nint AtkComponentIconText_LoadIconByIDDelegate(void* iconText, int iconId);
    // OLD -> public AtkComponentIconText_LoadIconByIDDelegate AtkComponentIconText_LoadIconByID = EzDelegate.Get<AtkComponentIconText_LoadIconByIDDelegate>("E8 ?? ?? ?? ?? 41 8D 47 2E");
    public AtkComponentIconText_LoadIconByIDDelegate AtkComponentIconText_LoadIconByID = EzDelegate.Get<AtkComponentIconText_LoadIconByIDDelegate>("E8 ?? ?? ?? ?? 41 8D 47 3C");

    public Memory()
    {
        EzSignatureHelper.Initialize(this);
    }

    private delegate void AtkComponentIconText_ReceiveEvent(nint a1, short a2, nint a3, nint a4, nint a5);
    [EzHook("44 0F B7 C2 4D 8B D1")]
    private EzHook<AtkComponentIconText_ReceiveEvent> AtkComponentIconText_ReceiveEventHook;

    private void AtkComponentIconText_ReceiveEventDetour(nint a1, short a2, nint a3, nint a4, nint a5)
    {
        try
        {
            //PluginLog.Debug($"{a1:X16}, {a2}, {a3:X16}, {a4:X16}, {a5:X16}");
            if(a2 == 6)
            {
                P.CommonProcessor.HoveringOver = a1;
            }
            if(a2 == 7)
            {
                P.CommonProcessor.HoveringOver = 0;
            }
            if(a2 == 9 && P.CommonProcessor.WasRightMousePressed)
            {
                P.CommonProcessor.CancelRequests.Add(a1);
                P.CommonProcessor.HoveringOver = 0;
            }
        }
        catch(Exception e)
        {
            e.Log();
        }
        AtkComponentIconText_ReceiveEventHook.Original(a1, a2, a3, a4, a5);
    }

    internal delegate nint ApplyStatusHitEffect(StatusHitEffectKind kind, nint target, nint target2, float speed, byte a5, short a6, byte a7);
    [EzHook("40 53 55 56 57 48 81 EC ?? ?? ?? ?? 0F 29 B4 24 ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 ?? ?? ?? ?? 48 8B 05", false)]
    internal EzHook<ApplyStatusHitEffect> ApplyStatusHitEffectHook;

    private nint ApplyStatusHitEffectDetour(StatusHitEffectKind kind, nint target, nint target2, float speed, byte a5, short a6, byte a7)
    {
        try
        {
            PluginLog.Information($"ApplyStatusHitEffectDetour {kind}, {target:X16}, {target2:X16}, {speed}, {a5}, {a6}, {a7}");
        }
        catch(Exception e)
        {
            e.Log();
        }
        return ApplyStatusHitEffectHook.Original(kind, target, target2, speed, a5, a6, a7);
    }

    public void Dispose()
    {

    }
}
