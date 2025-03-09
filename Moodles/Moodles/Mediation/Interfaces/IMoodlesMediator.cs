using System;

namespace Moodles.Moodles.Mediation.Interfaces;

internal interface IMoodlesMediator
{
    void Send<T>(T message) where T : MessageBase;
    void Subscribe<T>(IMoodleSubscriber subscriber, Action<T> action) where T : MessageBase;
    void Unsubscribe<T>(IMoodleSubscriber subscriber) where T : MessageBase;
    void UnsubscribeAll(IMoodleSubscriber subscriber);
}
