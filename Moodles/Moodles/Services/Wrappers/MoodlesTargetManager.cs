using Dalamud.Game.ClientState.Objects.Types;
using Moodles.Moodles.MoodleUsers.Interfaces;
using Moodles.Moodles.Services.Interfaces;

namespace Moodles.Moodles.Services.Wrappers;

internal class MoodlesTargetManager : IMoodlesTargetManager
{
    readonly DalamudServices DalamudServices;
    readonly IUserList UserList;

    public MoodlesTargetManager(DalamudServices dalamudServices, IUserList userList)
    {
        DalamudServices = dalamudServices;
        UserList = userList;
    }

    public IMoodleHolder? Target => GetMoodleHolder(DalamudServices.TargetManager.Target ?? DalamudServices.TargetManager.SoftTarget);
    public IMoodleHolder? FocusTarget => GetMoodleHolder(DalamudServices.TargetManager.FocusTarget);
    public IMoodleHolder? MouseOverTarget => GetMoodleHolder(DalamudServices.TargetManager.MouseOverTarget);
    public IMoodleHolder? PreviousTarget => GetMoodleHolder(DalamudServices.TargetManager.PreviousTarget);
    public IMoodleHolder? GPoseTarget => GetMoodleHolder(DalamudServices.TargetManager.GPoseTarget);
    public IMoodleHolder? MouseOverNameplateTarget => GetMoodleHolder(DalamudServices.TargetManager.MouseOverNameplateTarget);

    IMoodleHolder? GetMoodleHolder(IGameObject? target)
    {
        if (target == null) return null;

        return GetMoodleHolder(target.Address);
    }

    IMoodleHolder? GetMoodleHolder(nint address)
    {
        IMoodleUser? playerTarget = UserList.GetUser(address, false);
        if (playerTarget != null)
        {
            return playerTarget;
        }

        return UserList.GetPet(address);
    }
}
