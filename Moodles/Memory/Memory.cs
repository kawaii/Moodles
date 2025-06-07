using Dalamud.Utility;
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

    // Handles the detour of when we hover over an icon in our positive, negative, or special status icons.
    private void AtkComponentIconText_ReceiveEventDetour(nint a1, short a2, nint a3, nint a4, nint a5)
    {
        try
        {
            //PluginLog.Debug($"{a1:X16}, {a2}, {a3:X16}, {a4:X16}, {a5:X16}");
            if (a2 == 6)
            {
                P.CommonProcessor.HoveringOver = a1;
            }
            if (a2 == 7)
            {
                P.CommonProcessor.HoveringOver = 0;
            }
            if (a2 == 9 && P.CommonProcessor.WasRightMousePressed)
            {
                // Append the address to the cancelRequests for StatusManager updater in Tick Scheduler
                P.CommonProcessor.CancelRequests.Add(a1);
                P.CommonProcessor.HoveringOver = 0;
            }
        }
        catch (Exception e)
        {
            e.Log();
        }
        AtkComponentIconText_ReceiveEventHook.Original(a1, a2, a3, a4, a5);
    }

    internal delegate IntPtr SheApplier(string path, IntPtr target, IntPtr target2, float speed, char a5, UInt16 a6, char a7);
    [EzHook("E8 ?? ?? ?? ?? 48 8B D8 48 85 C0 74 27 B2 01", false)]
    internal EzHook<SheApplier> SheApplierHook;

    private IntPtr SheApplierDetour(string path, IntPtr target, IntPtr target2, float speed, char a5, UInt16 a6, char a7)
    {
        try
        {
            PluginLog.Information($"SheApplier {path}, {target:X16}, {target2:X16}, {speed}, {a5}, {a6}, {a7}");
        }
        catch (Exception e)
        {
            e.Log();
        }
        return SheApplierHook.Original(path, target, target2, speed, a5, a6, a7);
    }

    internal void SpawnSHE(uint iconID, IntPtr target, IntPtr target2, float speed = -1.0f, char a5 = char.MinValue, UInt16 a6 = 0, char a7 = char.MinValue)
    {
        try
        {
            string smallPath = Utils.FindVFXPathByIconID(iconID);
            if (smallPath.IsNullOrWhitespace())
            {
                PluginLog.Information($"Path for IconID: {iconID} is empty");
                return;
            }
            PluginLog.Verbose($"Path for IconID: {iconID} is: {smallPath}");
            SpawnSHE(smallPath, target, target2, speed, a5, a6, a7);
        }
        catch (Exception e)
        {
            e.Log();
        }
    }

    internal void SpawnSHE(string path, IntPtr target, IntPtr target2, float speed = -1.0f, char a5 = char.MinValue, UInt16 a6 = 0, char a7 = char.MinValue)
    {
        try
        {
            if (path.IsNullOrWhitespace())
            {
                PluginLog.Information($"Path for SHE is empty");
                return;
            }

            string fullPath = Utils.GetVfxPath(path);

            PluginLog.Verbose($"Path for SHE is: {fullPath}");

            SheApplierHook.Original(fullPath, target, target2, speed, a5, a6, a7);
        }
        catch (Exception e)
        {
            e.Log();
        }
    }

    public void Dispose()
    {

    }
}
