namespace Moodles.Moodles.Services.Interfaces;

internal interface IMoodlesServices
{
    Configuration Configuration { get; }

    IStringHelper StringHelper { get; }
    ISheets Sheets { get; }
}
