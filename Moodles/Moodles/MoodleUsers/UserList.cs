using Moodles.Moodles.MoodleUsers.Interfaces;
using Moodles.Moodles.StatusManaging.Interfaces;
using System.Linq;

namespace Moodles.Moodles.MoodleUsers;

internal class UserList : IUserList
{
    public const int UserArraySize = 100;

    public IMoodleUser?[] Users { get; } = new IMoodleUser[UserArraySize];
    public IMoodleUser? LocalPlayer => Users[0];

    public IMoodlePet? GetPet(nint pet)
    {
        if (pet == nint.Zero) return null;

        for (int i = 0; i < UserArraySize; i++)
        {
            IMoodleUser? user = Users[i];
            if (user == null) continue;

            IMoodlePet? pPet = user.GetPet(pet);
            if (pPet == null) continue;

            return pPet;
        }

        return null;
    }

    public IMoodleUser? GetUser(nint user, bool petMeansOwner = true)
    {
        if (user == nint.Zero) return null;

        for (int i = 0; i < UserArraySize; i++)
        {
            IMoodleUser? pUser = Users[i];
            if (pUser == null) continue;
            if (pUser.Address == user) return pUser;

            if (petMeansOwner)
            {
                if (pUser.GetPet(user) == null) continue;

                return pUser;
            }
        }

        return null;
    }

    public IMoodleUser? GetUserFromContentID(ulong contentID)
    {
        if (contentID == 0) return null;

        for (int i = 0; i < UserArraySize; i++)
        {
            IMoodleUser? pUser = Users[i];
            if (pUser == null) continue;
            if (pUser.ContentID != contentID) continue;

            return pUser;
        }

        return null;
    }

    public IMoodleUser? GetUserFromOwnerID(uint ownerID)
    {
        if (ownerID == 0) return null;

        for (int i = 0; i < UserArraySize; i++)
        {
            IMoodleUser? pUser = Users[i];
            if (pUser == null) continue;
            if (pUser.ShortObjectID != ownerID) continue;

            return pUser;
        }
        return null;
    }

    public IMoodleHolder? GetHolder(ulong contentId, int skeletonId)
    {
        if (contentId == 0) return null;

        for (int i = 0; i < UserArraySize; i++)
        {
            IMoodleUser? pUser = Users[i];
            if (pUser == null) continue;
            if (pUser.StatusManager.ContentID == contentId && pUser.StatusManager.SkeletonID == skeletonId) return pUser;
            
            int petCount = pUser.MoodlePets.Count;

            for (int f = 0; f < petCount; f++)
            {
                IMoodlePet pet = pUser.MoodlePets[f];
                if (pet.StatusManager.ContentID != contentId) continue;
                if (pet.StatusManager.SkeletonID != skeletonId) continue;

                return pet;
            }
        }

        return null;
    }

    public IMoodleHolder? GetHolder(IWorldMoodle worldMoodle)
    {
        for (int i = 0; i < UserArraySize; i++)
        {
            IMoodleUser? pUser = Users[i];
            if (pUser == null) continue;
            if (pUser.StatusManager.WorldMoodles.Contains(worldMoodle)) return pUser;

            int petCount = pUser.MoodlePets.Count;

            for (int f = 0; f < petCount; f++)
            {
                IMoodlePet pet = pUser.MoodlePets[f];
                if (!pet.StatusManager.WorldMoodles.Contains(worldMoodle)) continue;

                return pet;
            }
        }

        return null;
    }
}
