namespace Moodles.Moodles.MoodleUsers.Interfaces;

internal interface IUserList
{
    IMoodleUser?[] Users { get; }
    IMoodleUser? LocalPlayer { get; }

    IMoodlePet? GetPet(nint pet);
    IMoodleUser? GetUser(nint user);

    IMoodleUser? GetUserFromContentID(ulong contentID);
    IMoodleUser? GetUserFromOwnerID(uint ownerID);
}
