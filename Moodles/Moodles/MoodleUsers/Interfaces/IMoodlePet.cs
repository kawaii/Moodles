using Moodles.Moodles.Services.Interfaces;

namespace Moodles.Moodles.MoodleUsers.Interfaces;

internal interface IMoodlePet : IMoodleHolder
{
    IMoodleUser Owner { get; }

    int SkeletonID { get; }
    ulong ObjectID { get; }
    ushort Index { get; }
    string Name { get; }

    IPetSheetData? PetData { get; }
}
