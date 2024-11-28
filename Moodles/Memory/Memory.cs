using ECommons.EzHookManager;

namespace Moodles;
public unsafe partial class Memory : IDisposable
{
    public delegate nint AtkComponentIconText_LoadIconByIDDelegate(void* iconText, int iconId);
    // OLD -> public AtkComponentIconText_LoadIconByIDDelegate AtkComponentIconText_LoadIconByID = EzDelegate.Get<AtkComponentIconText_LoadIconByIDDelegate>("E8 ?? ?? ?? ?? 41 8D 47 2E");
    public AtkComponentIconText_LoadIconByIDDelegate AtkComponentIconText_LoadIconByID = EzDelegate.Get<AtkComponentIconText_LoadIconByIDDelegate>("E8 ?? ?? ?? ?? 41 8D 47 3D");

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

    internal delegate IntPtr SheApplier(IntPtr path, IntPtr target, IntPtr target2, float speed, byte a5, short a6, byte a7);
    [EzHook("E8 ?? ?? ?? ?? 48 8B D8 48 85 C0 74 27 B2 01", false)]
    internal EzHook<SheApplier> SheApplierHook;

    private IntPtr SheApplierDetour(IntPtr path, IntPtr target, IntPtr target2, float speed, byte a5, short a6, byte a7)
    {
        try
        {
            PluginLog.Information($"SheApplier {Marshal.PtrToStringUTF8(path)}, {target:X16}, {target2:X16}, {speed}, {a5}, {a6}, {a7}");
        }
        catch (Exception e)
        {
            e.Log();
        }
        return SheApplierHook.Original(path, target, target2, speed, a5, a6, a7);
    }

    internal void SpawnSHE(uint iconID, IntPtr target, IntPtr target2, float speed = -1.0f, byte a5 = 0, short a6 = 0, byte a7 = 0)
    {
        string smallPath = Utils.FindVFXPathByIconID(iconID);
        SpawnSHE(smallPath, target, target2, speed, a5, a6, a7);
    }

    internal void SpawnSHE(string path, IntPtr target, IntPtr target2, float speed = -1.0f, byte a5 = 0, short a6 = 0, byte a7 = 0)
    {
        string fullPath = Utils.GetVfxPath(path);
        fixed (byte* pPath = Encoding.UTF8.GetBytes(fullPath))
        {
            SheApplierHook.Original((IntPtr)pPath, target, target2, speed, a5, a6, a7);
        }
    }

    public void Dispose()
    {

    }
}
