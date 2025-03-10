﻿using Moodles.Moodles.Services.Data;
using Moodles.Moodles.StatusManaging;
using Moodles.Moodles.StatusManaging.Interfaces;

namespace Moodles.Moodles.Mediation;

internal record DatabaseDirtyMessage(IMoodlesDatabase Database) : MessageBase;
internal record DatabaseAddedStatusManagerMessage(IMoodlesDatabase Database, IMoodleStatusManager StatusManager) : MessageBase;
internal record DatabaseAddedMoodleMessage(IMoodlesDatabase Database, IMoodle Moodle) : MessageBase;
internal record DatabaseRemovedStatusManagerMessage(IMoodlesDatabase Database, IMoodleStatusManager StatusManager) : MessageBase;
internal record DatabaseRemovedMoodleMessage(IMoodlesDatabase Database, IMoodle Moodle) : MessageBase;
internal record StatusManagerDirtyMessage(IMoodleStatusManager StatusManager) : MessageBase;
internal record StatusManagerClearedMessage(IMoodleStatusManager StatusManager) : MessageBase;
internal record MoodleChangedMessage(IMoodle Moodle) : MessageBase;
internal record MoodleAppliedMessage(IMoodle Moodle, WorldMoodle WorldMoodle, IMoodleStatusManager StatusManager) : MessageBase;
internal record MoodleRemovedMessage(WorldMoodle WorldMoodle, MoodleRemoveReason RemoveReason, IMoodleStatusManager StatusManager) : MessageBase;
internal record MoodleStackChangedMessage(WorldMoodle WorldMoodle) : MessageBase;