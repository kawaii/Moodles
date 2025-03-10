using Dalamud.Plugin.Services;
using Moodles.Moodles.Mediation;
using Moodles.Moodles.Services.Interfaces;
using Moodles.Moodles.Updating.Interfaces;

namespace Moodles.Moodles.SaveHandling;

internal class SaveHandler : MoodleSubscriber, IUpdatable
{
    const double SaveInterval = 3;

    public bool Enabled { get; set; } = true;

    double saveTimer = 0;

    bool savable = false;

    readonly IMoodlesServices Services;

    public SaveHandler(IMoodlesServices moodlesServices) : base(moodlesServices.Mediator)
    {
        Services = moodlesServices;

        Mediator.Subscribe<DatabaseAddedMoodleMessage>(this, DatabaseAddedMoodle);
        Mediator.Subscribe<DatabaseRemovedMoodleMessage>(this, DatabaseRemoveMoodleMessage);
        Mediator.Subscribe<MoodleChangedMessage>(this, OnMoodleChanged);
    }

    public void Update(IFramework framework)
    {
        if (saveTimer < SaveInterval)
        {
            saveTimer += framework.UpdateDelta.TotalSeconds;
            return;
        }

        if (!savable) return;

        savable = false;

        saveTimer -= SaveInterval;

        ForceSave();
    }

    public void ForceSave()
    {
        Services.Configuration.Save();
    }

    void DatabaseAddedMoodle(DatabaseAddedMoodleMessage message)
    {
        savable = true;
    }

    void DatabaseRemoveMoodleMessage(DatabaseRemovedMoodleMessage message)
    {
        savable = true;
    }

    void OnMoodleChanged(MoodleChangedMessage message)
    {
        savable = true;
    }
}
