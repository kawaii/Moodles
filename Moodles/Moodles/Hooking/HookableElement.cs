using Moodles.Moodles.Hooking.Interfaces;
using Moodles.Moodles.Services;
using Moodles.Moodles.Services.Interfaces;

namespace Moodles.Moodles.Hooking;

internal abstract class HookableElement : IHookableElement
{
    protected readonly DalamudServices DalamudServices;
    protected readonly IMoodlesServices MoodlesServices;

    public HookableElement(DalamudServices dalamudServices, IMoodlesServices moodlesServices)
    {
        DalamudServices = dalamudServices;
        MoodlesServices = moodlesServices;

        DalamudServices.Hooking.InitializeFromAttributes(this);
    }

    public abstract void Init();
    protected abstract void OnDispose();

    public void Dispose()
    {

        OnDispose();
    }
}

