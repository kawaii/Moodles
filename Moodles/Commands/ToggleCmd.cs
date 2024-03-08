using ECommons.GameHelpers;
using Moodles.Data;

namespace Moodles.Commands
{
    internal static class ToggleCmd
    {
        private static readonly IEnumerable<MyStatus> Statuses = C.SavedStatuses;
        private static readonly IEnumerable<Preset> Presets = C.SavedPresets;
        public static void Process(string _, string arguments)
        {
            var args = arguments.ToLower().Split(' ');
            ProcessMoodleCommand(args);
        }

        private static void ProcessMoodleCommand(string[] commandArgs)
        {
            if (commandArgs.Count() != 2) throw new ArgumentOutOfRangeException(nameof(commandArgs));

            var action = commandArgs[0];

            if (Guid.TryParse(commandArgs[1], out Guid guid))
            {
                switch (action)
                {
                    case "add":
                        ApplyMoodle(guid);
                        break;
                    case "remove":
                        ApplyMoodle(guid, true);
                        break;
                    default:
                        break;
                }
            }
            else
            {
                Notify.Error("Not a valid guid for moodle");
            }
        }

        private static void ApplyMoodle(Guid guid, bool cancel = false)
        {
            var match = Statuses.SingleOrDefault(x => x.GUID == guid);
            if (match == null)
            {
                ApplyPreset(guid, cancel);
            }

            if (cancel)
            {
                Utils.GetMyStatusManager(Player.NameWithWorld).Cancel(match);
            }
            else
            {
                Utils.GetMyStatusManager(Player.NameWithWorld).AddOrUpdate(match.PrepareToApply(match.Persistent ? PrepareOptions.Persistent : PrepareOptions.NoOption));
            }
        }

        private static void ApplyPreset(Guid guid, bool cancel = false)
        {
            var match = Presets.SingleOrDefault(x => x.GUID == guid);
            if (match == null)
            {
                Notify.Error("Could not find moodle");
            }

            if (cancel)
            {
                Utils.GetMyStatusManager(Player.NameWithWorld).RemovePreset(match);
            }
            else
            {
                Utils.GetMyStatusManager(Player.NameWithWorld).ApplyPreset(match);
            }
        }
    }
}
