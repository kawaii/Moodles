using Dalamud.Plugin.Services;
using Moodles.Moodles.MoodleUsers.Interfaces;

namespace Moodles.Moodles.StatusManaging.Interfaces;

internal interface IMoodleStatusManager
{
    public bool IsActive { get;  }
    public bool IsEphemeral { get; }

    ulong ContentID { get; }

    int SkeletonID { get; } // Player skeleton is 0

    string Name { get; }
    ushort Homeworld { get; }
    string HomeworldName { get; }

    void Update(IFramework framework);
    void UpdateEntry(IMoodleUser user);
    void UpdateEntry(IMoodlePet pet);

    void SetIdentifier(ulong contentID, int skeletonID, bool removeEphemeralStatus = false);

    void Clear(bool isIPC);
}
