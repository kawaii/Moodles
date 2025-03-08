using System;

namespace Moodles.Moodles.Hooking.Interfaces;

internal interface IHookableElement : IDisposable
{
    void Init();
}
