using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game;
using Moodles.Moodles.Services.Interfaces;
using System.Collections.Generic;
using System.Globalization;
using Moodles.Moodles.MoodleUsers.Interfaces;
using Dalamud.Game.Text.SeStringHandling.Payloads;

namespace Moodles.Moodles.Services.Wrappers;

internal class StringHelperWrapper : IStringHelper
{
    readonly DalamudServices DalamudServices;

    public StringHelperWrapper(DalamudServices dalamudServices)
    {
        DalamudServices = dalamudServices;
    }

    public string MakeTitleCase(string str) => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(str.ToLower());

    public string GetVfxPath(string path)
    {
        string outcome = string.IsNullOrEmpty(path) ? "vfx/common/eff/dk05th_stup0t.avfx" : $"vfx/common/eff/{path}.avfx";

        //if (!outcome.EndsWith('\0')) outcome += '\0';

        return outcome;
    }


    // From: https://github.com/perchbirdd/DamageInfoPlugin/blob/44fc1abfb4ad1d7adbdcaca6a85d253346fe5210/DamageInfoPlugin/DamageInfoPlugin.cs#L822
    public SeString AddCasterText(IMoodleUser user, SeString originalText)
    {
        SeString name = user.Name;
        var newPayloads = new List<Payload>();

        if (name.Payloads.Count == 0) return originalText;

        switch (DalamudServices.ClientState.ClientLanguage)
        {
            case ClientLanguage.Japanese:
                newPayloads.AddRange(name.Payloads);
                newPayloads.Add(new TextPayload("から"));
                break;
            case ClientLanguage.English:
                newPayloads.Add(new TextPayload("from "));
                newPayloads.AddRange(name.Payloads);
                break;
            case ClientLanguage.German:
                newPayloads.Add(new TextPayload("von "));
                newPayloads.AddRange(name.Payloads);
                break;
            case ClientLanguage.French:
                newPayloads.Add(new TextPayload("de "));
                newPayloads.AddRange(name.Payloads);
                break;
            default:
                newPayloads.Add(new TextPayload(">"));
                newPayloads.AddRange(name.Payloads);
                break;
        }

        if (originalText.Payloads.Count > 0)
        {
            newPayloads.AddRange(originalText.Payloads);
        }

        return new SeString(newPayloads);
    }
}
