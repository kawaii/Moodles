using Moodles.Moodles.Mediation.Interfaces;
using System;

namespace Moodles.Moodles.Services.Interfaces;

internal interface IMoodlesServices : IDisposable
{
    Configuration Configuration { get; }

    IStringHelper StringHelper { get; }
    ISheets Sheets { get; }
    IMoodlesMediator Mediator { get; }
    IMoodlesCache MoodlesCache { get; }
    IMoodleValidator MoodleValidator { get; }
    IMoodlesTargetManager TargetManager { get; }
}
