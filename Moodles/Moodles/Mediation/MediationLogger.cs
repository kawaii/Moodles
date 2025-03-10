using ECommons.Logging;
using Moodles.Moodles.Mediation.Interfaces;
using System;

namespace Moodles.Moodles.Mediation;

internal class MediationLogger : MoodleSubscriber, IDisposable
{
    public MediationLogger(IMoodlesMediator mediator) : base(mediator)
    {
        mediator.Subscribe<MessageBase>(this, Log);
    }

    void Log(MessageBase mBase)
    {
        PluginLog.LogVerbose($"Received message from: {mBase.GetType()}");
    }

    public void Dispose()
    {
        Mediator.Unsubscribe<MessageBase>(this);
    }
}
