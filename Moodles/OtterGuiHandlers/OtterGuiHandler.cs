using OtterGui;
using OtterGui.Filesystem;
using OtterGui.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moodles.OtterGuiHandlers;
public sealed class OtterGuiHandler : IDisposable
{
    public MoodleFileSystem MoodleFileSystem;
    public PresetFileSystem PresetFileSystem;
    public Logger Logger;
    public AutomationList AutomationList;
    public OtterGuiHandler()
    {
        try
        {
            Logger = new();
            MoodleFileSystem = new(this);
            PresetFileSystem = new(this);
            AutomationList = new();
        }
        catch(Exception ex)
        {
            ex.Log();
        }
    }

    public void Dispose()
    {
        Safe(() => MoodleFileSystem?.Save());
        Safe(() => PresetFileSystem?.Save());
    }
}
