using Dalamud.Game.ClientState.Objects.SubKinds;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using Lumina.Excel.Sheets;
using Moodles.Data;
using System.Text.RegularExpressions;

namespace Moodles.Commands;

public static class MoodleCommandProcessor
{
    private const string CUSTOM_TAG = "[custom]";
    private static string lastCommandPart = string.Empty;
    private static List<string> matchedArguments = [];
    private static int customCounter = 0;

    public static void Process(string _, string arguments)
    {
        arguments += " ";
        ClearLast();
        PrepareArguments(ref arguments);
        var args = arguments.ToLower().Split(' ');
        try
        {
            if(arguments.Length == 0) ThrowArgumentException();
            ProcessMoodleCommand(args);
        }
        catch(MoodleChatException moodleChatException)
        {
            if(C.DisplayCommandFeedback)
            {
                Svc.Chat.PrintError(moodleChatException.Message);
            }
        }
    }

    private static void PrepareArguments(ref string arguments)
    {
        foreach(Match match in Regex.Matches(arguments, "(\".+?\")"))
        {
            var matchString = match.Value;
            matchedArguments.Add(matchString.Replace("\"", ""));
            arguments = arguments.Replace(matchString, CUSTOM_TAG);
        }
    }

    private static void ClearLast()
    {
        lastCommandPart = string.Empty;
        matchedArguments.Clear();
        customCounter = 0;
    }

    // Commands should look like this:
    // /moodle apply|remove|toggle self|target|"Firstname Lastname"|"Firstname Lastname@world" moodle|preset|automation "moodleName"|"presetName"|"automationName"|"GUID"|all
    // /moodle help

    private static void ProcessMoodleCommand(string[] commandArgs)
    {
        var moodleState = ParseMoodleState(commandArgs);

        if(moodleState == MoodleState.INVALID)
        {
            throw new MoodleChatException($"'{lastCommandPart}' is invalid syntax. Use: apply|remove|toggle|help");
        }
        else if(moodleState == MoodleState.Help)
        {
            HandleHelp();
            return;
        }

        var targetState = ParseTargetState(commandArgs);

        if(targetState == TargetState.INVALID)
        {
            throw new MoodleChatException($"'{lastCommandPart}' is invalid syntax. Use: self|target|\"Firstname Lastname\"|\"Firstname Lastname@world\"");
        }
        else if(targetState == TargetState.Custom)
        {
            customCounter++;
        }

        var moodleType = ParseMoodleType(commandArgs);

        if(moodleType == MoodleType.INVALID)
        {
            throw new MoodleChatException($"'{lastCommandPart}' is invalid syntax. Use: moodle|preset|automation");
        }

        var moodleNameType = ParseMoodleNameType(commandArgs);

        if(moodleNameType == MoodleNameType.INVALID)
        {
            throw new MoodleChatException($"'{lastCommandPart}' is invalid syntax. Use: \"GUID\"|\"ELEMENT NAME\"|\"automationName\"|all");
        }

        customCounter = 0;

        MoveCommand(moodleState, targetState, moodleType, moodleNameType);
    }

    private static void MoveCommand(MoodleState moodleState, TargetState targetState, MoodleType moodleType, MoodleNameType moodleNameType)
    {
        switch(moodleType)
        {
            case MoodleType.Moodle:
                HandleAsMoodle(targetState, moodleState, moodleNameType); break;
            case MoodleType.Preset:
                HandleAsPreset(targetState, moodleState, moodleNameType); break;
            case MoodleType.Automation:
                HandleAsAutomation(targetState, moodleState, moodleNameType); break;
            case MoodleType.INVALID:
            default:
                break;
        }
    }

    private static unsafe void HandleAsMoodle(TargetState targetState, MoodleState moodleState, MoodleNameType moodleNameType)
    {
        var sm = GetStatusManager(targetState);
        var myStatuses = GetMyStatus(moodleNameType);

        foreach (var myStatus in myStatuses)
        {

            if (moodleState == MoodleState.Toggle)
            {
                if (sm.ContainsStatus(myStatus))
                {
                    moodleState = MoodleState.Remove;
                }
                else
                {
                    moodleState = MoodleState.Apply;
                }
            }

            if (moodleState == MoodleState.Apply)
            {
                if (IPC.GSpeakPlayerCache.ContainsKey((nint)sm.Owner))
                {
                    myStatus.SendGSpeakMessage((nint)sm.Owner);
                }
                else if (IPC.SundouleiaPlayerCache.ContainsKey((nint)sm.Owner))
                {
                    myStatus.SendSundouleiaMessage((nint)sm.Owner);
                }
                else
                {
                    sm.AddOrUpdate(myStatus.PrepareToApply(myStatus.Persistent ? PrepareOptions.Persistent : PrepareOptions.NoOption), UpdateSource.StatusTuple);
                }
            }
            else if (moodleState == MoodleState.Remove)
            {
                if (IPC.GSpeakPlayerCache.ContainsKey((nint)sm.Owner))
                {
                    var newStatus = myStatus.JSONClone();
                    newStatus.ExpiresAt = 0;
                    newStatus.SendGSpeakMessage((nint)sm.Owner);
                }
                else if (IPC.SundouleiaPlayerCache.ContainsKey((nint)sm.Owner))
                {
                    var newStatus = myStatus.JSONClone();
                    newStatus.ExpiresAt = 0;
                    newStatus.SendSundouleiaMessage((nint)sm.Owner);
                }
                else
                {
                    sm.Cancel(myStatus);
                }
            }
        }
    }

    private static void HandleAsPreset(TargetState targetState, MoodleState moodleState, MoodleNameType moodleNameType)
    {
        var statusManager = GetStatusManager(targetState);
        var myPresets = GetMyPreset(moodleNameType);

        foreach (var myPreset in myPresets)
        {
            if (moodleState == MoodleState.Toggle)
            {
                if (statusManager.ContainsPreset(myPreset))
                {
                    moodleState = MoodleState.Remove;
                }
                else
                {
                    moodleState = MoodleState.Apply;
                }
            }

            if (moodleState == MoodleState.Apply)
            {
                statusManager.ApplyPreset(myPreset);
            }
            else if (moodleState == MoodleState.Remove)
            {
                statusManager.RemovePreset(myPreset);
            }
        }
    }

    private static unsafe void HandleAsAutomation(TargetState targetState, MoodleState moodleState, MoodleNameType moodleNameType)
    {
        if(moodleNameType == MoodleNameType.GUID)
        {
            throw new MoodleChatException("GUID is an invalid parameter type for automation.");
        }

        Character* chara = null!;

        if(targetState == TargetState.Self)
        {
            chara = LocalPlayer.Character;
        }
        else if(targetState == TargetState.Target)
        {
            if(Svc.Targets.Target is IPlayerCharacter)
            {
                chara = (Character*)Svc.Targets.Target.Address;
            }
            else
            {
                if(Svc.Targets.Target is null)
                {
                    throw new MoodleChatException("No target selected.");
                }
                else
                {
                    throw new MoodleChatException("Target is not a valid player.");
                }
            }
        }
        else if(targetState == TargetState.Custom)
        {
            chara = PlayerFromString(GetCustomString());
        }

        if(chara == null)
        {
            throw new MoodleChatException("An error occured whilst obtaining the selected target.");
        }

        var customString = GetCustomString();
        AutomationProfile selectedProfile = null!;

        var hasWorld = customString.Split('@').Length == 2 || targetState != TargetState.Custom;

        foreach(var profile in C.AutomationProfiles)
        {
            if(profile.Name == customString)
            {
                selectedProfile = profile;
                break;
            }
        }

        if(selectedProfile == null)
        {
            throw new MoodleChatException($"Automation with the name '{customString}' does not exist.");
        }

        if(moodleState == MoodleState.Toggle)
        {
            var nameIsCorrect = selectedProfile.Character == chara->NameString;
            var worldIsCorrect = true;

            if(hasWorld)
            {
                worldIsCorrect = selectedProfile.World == chara->HomeWorld;
            }

            if(nameIsCorrect && worldIsCorrect)
            {
                moodleState = MoodleState.Remove;
            }
            else
            {
                moodleState = MoodleState.Apply;
            }
        }

        if(moodleState == MoodleState.Apply)
        {
            selectedProfile.Character = chara->NameString;
            if (hasWorld)
            {
                selectedProfile.World = chara->HomeWorld;
            }
            else
            {
                selectedProfile.World = 0;
            }
        }
        else if(moodleState == MoodleState.Remove)
        {
            selectedProfile.Character = string.Empty;
            selectedProfile.World = 0;
        }
    }

    private static void HandleHelp()
    {
        Svc.Chat.Print(
            "Moodles Help: \n" +
            "\n" +
            "A Moodles command is build as followed:\n" +
            "    /moodle [action] [target selector] [element type] [element name]\n" +
            "\n" +
            "Example command: /moodle apply self moodle \"moodlename\"\n" +
            "Or: /moodle toggle \"Firstname Lastname@Homeworldname\" automation \"automationname\"\n"  +
            "\n" +
            "[action]\n" +
            "    apply\n" +
            "        Applies the specified element to the specified target selector.\n" +
            "    remove\n" +
            "        Removes the specified element to the specified target selector.\n" +
            "    toggle\n" +
            "        Toggles the specified element to the specified target selector.\n" +
            "\n" +
            "[target selector]\n" +
            "    self\n" +
            "        Selects yourself as the designated target.\n" +
            "    target\n" +
            "        Selects your target as the designated target.\n" +
            "    \"Firstname Lastname\"\n" +
            "        Selects your specified character as the designated target.\n" +
            "    \"Firstname Lastname@Homeworld\"\n" +
            "        Selects your specified character with the given homeworld as the designated target.\n" +
            "\n" +
            "[element type]\n" +
            "    moodle\n" +
            "        Specifies that this command applies to Moodles.\n" +
            "    preset\n" +
            "        Specifies that this command applies to Presets.\n" +
            "    automation\n" +
            "        Specifies that this command applies to Automation.\n" +
            "\n" +
            "[element name]\n" +
            "    \"GUID\"\n" +
            "        The GUID of the element you want target.\n" +
            "    \"ELEMENT NAME\"\n" +
            "        The EXACT name of the element you want to target.\n" +
            "    \"all\"\n" +
            "        Every element of the selected type.\n");
    }

    private static Preset[] GetMyPreset(MoodleNameType moodleNameType)
    {
        if (moodleNameType == MoodleNameType.All)
        {
            return C.SavedPresets.ToArray();
        }

        var cString = GetCustomString();
        var match = C.SavedPresets.SingleOrDefault(x => PresetMatch(x, moodleNameType, cString));

        if(match == null)
        {
            if(moodleNameType == MoodleNameType.Name)
            {
                throw new MoodleChatException($"Preset with the name '{cString}' could not be found.");
            }
            else
            {
                throw new MoodleChatException($"Preset with the GUID '{cString}' could not be found.");
            }
        }

        return [match];
    }

    private static bool PresetMatch(Preset preset, MoodleNameType moodleNameType, string customString)
    {
        if(moodleNameType == MoodleNameType.GUID)
        {
            return preset.GUID == Guid.Parse(customString);
        }
        else
        {
            if(P.OtterGuiHandler.PresetFileSystem.FindLeaf(preset, out var l))
            {
                if(l != null)
                {
                    return l.FullName() == customString;
                }
            }
        }

        return false;
    }

    private static MyStatus[] GetMyStatus(MoodleNameType moodleNameType)
    {
        if (moodleNameType == MoodleNameType.All)
        {
            return C.SavedStatuses.ToArray();
        }

        var cString = GetCustomString();
        var match = C.SavedStatuses.SingleOrDefault(x => StatusMatch(x, moodleNameType, cString));

        if(match == null)
        {
            if(moodleNameType == MoodleNameType.Name)
            {
                throw new MoodleChatException($"Moodle with the name '{cString}' could not be found.");
            }
            else
            {
                throw new MoodleChatException($"Moodle with the GUID '{cString}' could not be found.");
            }
        }

        return [match];
    }

    private static bool StatusMatch(MyStatus myStatus, MoodleNameType moodleNameType, string customString)
    {
        if(moodleNameType == MoodleNameType.GUID)
        {
            return myStatus.GUID == Guid.Parse(customString);
        }
        else
        {
            if(P.OtterGuiHandler.MoodleFileSystem.FindLeaf(myStatus, out var l))
            {
                if(l != null)
                {
                    return l.FullName() == customString;
                }
            }
        }

        return false;
    }

    private static unsafe MyStatusManager GetStatusManager(TargetState targetState)
    {
        MyStatusManager statusManager = null!;

        if(targetState == TargetState.Self)
        {
            statusManager = Utils.GetMyStatusManager(LocalPlayer.NameWithWorld);
        }
        else if(targetState == TargetState.Target)
        {
            if(Svc.Targets.Target is IPlayerCharacter)
            {
                statusManager = Utils.GetMyStatusManager(((Character*)Svc.Targets.Target.Address)->GetNameWithWorld());
            }
            else
            {
                if(Svc.Targets.Target == null)
                {
                    throw new MoodleChatException("No target selected.");
                }
                else
                {
                    throw new MoodleChatException("Target is not a valid player.");
                }
            }
        }
        else if(targetState == TargetState.Custom)
        {
            Character* chara = PlayerFromString(GetCustomString());
            if(chara == null)
            {
                statusManager = Utils.GetMyStatusManager(chara->GetNameWithWorld());
            }
        }

        return statusManager;
    }

    private static unsafe Character* PlayerFromString(string playerString)
    {
        var splitString = playerString.Split('@');
        var hasWorld = false;

        if(splitString.Length == 2)
        {
            hasWorld = true;
        }

        var userName = splitString[0];
        var homeworld = -1;

        if(hasWorld)
        {
            foreach(var world in Svc.Data.GetExcelSheet<World>())
            {
                if(world.Name == splitString[1])
                {
                    homeworld = (int)world.RowId;
                    break;
                }
            }
        }

        var chara = (Character*)CharacterManager.Instance()->LookupBattleCharaByName(userName, true, (short)homeworld);
        if(chara == null)
        {
            throw new MoodleChatException($"Specified Target Selector '{playerString}' could not be found.");
        }

        return chara;
    }

    private static string GetCustomString(bool applyCounter = true)
    {
        var customString = matchedArguments[customCounter];
        if(applyCounter) customCounter++;
        return customString;
    }

    private static MoodleState ParseMoodleState(string[] commandArgs) => GetCommandPart(commandArgs, 0) switch
    {
        "apply" => MoodleState.Apply,
        "remove" => MoodleState.Remove,
        "toggle" => MoodleState.Toggle,
        "help" => MoodleState.Help,
        _ => MoodleState.INVALID
    };

    private static TargetState ParseTargetState(string[] commandArgs) => GetCommandPart(commandArgs, 1) switch
    {
        "self" => TargetState.Self,
        "target" => TargetState.Target,
        CUSTOM_TAG => TargetState.Custom,
        _ => TargetState.INVALID
    };

    private static MoodleType ParseMoodleType(string[] commandArgs) => GetCommandPart(commandArgs, 2) switch
    {
        "moodle" => MoodleType.Moodle,
        "preset" => MoodleType.Preset,
        "automation" => MoodleType.Automation,
        _ => MoodleType.INVALID
    };

    private static MoodleNameType ParseMoodleNameType(string[] commandArgs)
    {
        var commandString = GetCommandPart(commandArgs, 3);

        if (commandString == "all")
        {
            return MoodleNameType.All;
        }
        else if (commandString != CUSTOM_TAG)
        {
            return MoodleNameType.INVALID;
        }

        var customString = GetCustomString(false);
        if(Guid.TryParse(customString, out _))
        {
            return MoodleNameType.GUID;
        }
        else
        {
            return MoodleNameType.Name;
        }
    }

    private static void ThrowArgumentException() => throw new MoodleChatException("Missing arguments. Use \"/moodle help\" for more information.");

    private static string GetCommandPart(string[] commandArgs, int location)
    {
        if(commandArgs.Length <= location) ThrowArgumentException();
        return lastCommandPart = commandArgs[location];
    }

    private enum MoodleState
    {
        INVALID,
        Apply,
        Remove,
        Toggle,
        Help,
        Settings
    }

    private enum TargetState
    {
        INVALID,
        Self,
        Target,
        Custom
    }

    private enum MoodleType
    {
        INVALID,
        Moodle,
        Preset,
        Automation
    }

    private enum MoodleNameType
    {
        INVALID,
        Name,
        GUID,
        All
    }

    private class MoodleChatException : Exception
    {
        public MoodleChatException(string message) : base(message) { }
    }
}
