using Moodles.Moodles.Mediation.Interfaces;
using Moodles.Moodles.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Moodles.Moodles.Mediation;

// Shamelessly stolen concept from Mare...
// It just works so well :nootspank:
internal class MoodleMediator : IMoodlesMediator
{
    readonly ConcurrentDictionary<Type, HashSet<SubscriberAction>> _subscriberDict = new ConcurrentDictionary<Type, HashSet<SubscriberAction>>();
    readonly ConcurrentDictionary<Type, MethodInfo?> _genericExecuteMethods = new ConcurrentDictionary<Type, MethodInfo?>();

    public void Send<T>(T message) where T : MessageBase
    {
        PluginLog.LogVerbose($"Message Send: '{message.ToString()}'");
        ExecuteMessage(message);
    }

    public void Subscribe<T>(IMoodleSubscriber subscriber, Action<T> action) where T : MessageBase
    {
        _ = _subscriberDict.TryAdd(typeof(T), []);

        if (!_subscriberDict[typeof(T)].Add(new(subscriber, action)))
        {
            throw new InvalidOperationException("Already subscribed");
        }
    }

    public void Unsubscribe<T>(IMoodleSubscriber subscriber) where T : MessageBase
    {
        if (_subscriberDict.ContainsKey(typeof(T)))
        {
            _subscriberDict[typeof(T)].RemoveWhere(p => p.Subscriber == subscriber);
        }
    }

    public void UnsubscribeAll(IMoodleSubscriber subscriber)
    {
        foreach (Type kvp in _subscriberDict.Select(k => k.Key))
        {
            int unSubbed = _subscriberDict[kvp]?.RemoveWhere(p => p.Subscriber == subscriber) ?? 0;
            if (unSubbed > 0)
            {
                PluginLog.LogVerbose($"{subscriber.GetType().Name} unsubscribed from {kvp.Name}");
            }
        }
    }

    void ExecuteMessage(MessageBase message)
    {
        if (!_subscriberDict.TryGetValue(message.GetType(), out HashSet<SubscriberAction>? subscribers)) return;
        if (subscribers == null) return;
        if (subscribers.Count == 0) return;

        SubscriberAction[] subscribersCopy = subscribers.ToArray();

        Type msgType = message.GetType();
        if (!_genericExecuteMethods.TryGetValue(msgType, out MethodInfo? methodInfo))
        {
            _genericExecuteMethods[msgType] = methodInfo = GetType()
                 .GetMethod(nameof(ExecuteReflected), BindingFlags.NonPublic | BindingFlags.Instance)?
                 .MakeGenericMethod(msgType);
        }

        if (methodInfo != null)
        {
            methodInfo!.Invoke(this, [subscribersCopy, message]);
        }
        else
        {
            PluginLog.LogWarning($"Method info was null. This shouldn't be possible");
        }
    }

    void ExecuteReflected<T>(SubscriberAction[] subscribers, T message) where T : MessageBase
    {
        foreach(SubscriberAction subscriber in subscribers)
        {
            try
            {
                ((Action<T>)subscriber.Action).Invoke(message);
            }
            catch(Exception e)
            {
                PluginLog.LogException(e);
            }
        }
    }

    sealed class SubscriberAction
    {
        public SubscriberAction(IMoodleSubscriber subscriber, object action)
        {
            Subscriber = subscriber;
            Action = action;
        }

        public object Action { get; }
        public IMoodleSubscriber Subscriber { get; }
    }
}