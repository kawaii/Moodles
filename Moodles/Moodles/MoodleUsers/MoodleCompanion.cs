using FFXIVClientStructs.FFXIV.Client.Game.Character;
using Moodles.Moodles.MoodleUsers.Interfaces;
using Moodles.Moodles.Services.Interfaces;
using Moodles.Moodles.StatusManaging.Interfaces;

namespace Moodles.Moodles.MoodleUsers;

internal unsafe sealed class MoodleCompanion : BaseMoodlesPet, IMoodleCompanion
{
    public Companion* Companion { get => (Companion*)Address; }

    public MoodleCompanion(Companion* companion, IMoodleUser owner, IMoodlesDatabase database, IMoodlesServices moodleServices) : base(&companion->Character, owner, database, moodleServices, false) { }
}
