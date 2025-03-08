using System;
using Dalamud.Configuration;

namespace Moodles;

[Serializable]
internal class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 2;


}
