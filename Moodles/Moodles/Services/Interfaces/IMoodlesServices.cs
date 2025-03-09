using Moodles.Moodles.Mediation.Interfaces;

namespace Moodles.Moodles.Services.Interfaces;

internal interface IMoodlesServices
{
    Configuration Configuration { get; }

    IStringHelper StringHelper { get; }
    ISheets Sheets { get; }
    IMoodlesMediator Mediator { get; }
}
