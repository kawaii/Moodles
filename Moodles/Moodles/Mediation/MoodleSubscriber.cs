using Moodles.Moodles.Mediation.Interfaces;
using Moodles.Moodles.Services;

namespace Moodles.Moodles.Mediation;

internal abstract class MoodleSubscriber : IMoodleSubscriber
{
    public MoodleMediator Mediator { get; }

    protected MoodleSubscriber(MoodleMediator mediator)
    {
        Mediator = mediator;
    }

    protected void UnsubscribeAll()
    {
        PluginLog.LogVerbose($"Unsubscribing from all for {GetType().Name} ({this})");
        Mediator.UnsubscribeAll(this);
    }
}
