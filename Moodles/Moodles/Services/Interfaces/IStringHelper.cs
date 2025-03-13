using Dalamud.Game.Text.SeStringHandling;
using Moodles.Moodles.MoodleUsers.Interfaces;

namespace Moodles.Moodles.Services.Interfaces;

internal interface IStringHelper
{
    string MakeTitleCase(string str);

    string GetVfxPath(string path);

    SeString AddCasterText(IMoodleUser user, SeString originalText);
}
