using Moodles.Moodles.Services.Interfaces;
using System.Globalization;

namespace Moodles.Moodles.Services.Wrappers;

internal class StringHelperWrapper : IStringHelper
{
    public string MakeTitleCase(string str) => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(str.ToLower());
}
