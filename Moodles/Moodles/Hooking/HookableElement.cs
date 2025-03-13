using Moodles.Moodles.Hooking.Interfaces;
using Moodles.Moodles.Mediation;
using Moodles.Moodles.MoodleUsers.Interfaces;
using Moodles.Moodles.Services;
using Moodles.Moodles.Services.Interfaces;

namespace Moodles.Moodles.Hooking;

internal abstract class HookableElement : MoodleSubscriber, IHookableElement
{
    protected readonly DalamudServices DalamudServices;
    protected readonly IMoodlesServices MoodlesServices;
    protected readonly IUserList UserList;

    public HookableElement(DalamudServices dalamudServices, IUserList userList, IMoodlesServices moodlesServices) : base(moodlesServices.Mediator)
    {
        DalamudServices = dalamudServices;
        UserList = userList;
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

