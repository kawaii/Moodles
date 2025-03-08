using Dalamud.Plugin.Services;
using Moodles.Moodles.Services.Interfaces;
using Moodles.Moodles.StatusManaging.Interfaces;

namespace Moodles.Moodles.StatusManaging;

internal class MoodlesStatusManager : IMoodleStatusManager
{
    public bool IsActive { get; private set; }
    public bool IsEphemeral { get; private set; } = false;

    public ulong ContentID { get; private set; }     // The owners contentID if this is a pets status manager

    public int SkeletonID { get; private set; }      // The pets skeleton, is 0 if it is a player

    public string Name { get; private set; } = "";

    public ushort Homeworld { get; private set; }
    public string HomeworldName { get; private set; } = "";

    readonly IMoodlesServices Services;

    public MoodlesStatusManager(IMoodlesServices services, ulong contentID, int skeletonID, string name, ushort homeworld, bool active)
    {
        Services = services;

        ContentID = contentID;
        SkeletonID = skeletonID;
        IsActive = active;
        IsEphemeral = !IsActive;

        SetName(name);
        SetHomeworld(homeworld);
    }

    void SetName(string name)
    {
        Name = name;
    }

    void SetHomeworld(ushort homeworld)
    {
        Homeworld = homeworld;
        HomeworldName = Services.Sheets.GetWorldName(homeworld) ?? "...";
    }

    public void Update(IFramework framework)
    {
        
    }

    public void Clear(bool isIPC)
    {
        IsActive = false;
    }
}
