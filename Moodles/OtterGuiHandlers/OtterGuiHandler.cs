using OtterGui.Log;

namespace Moodles.OtterGuiHandlers;
public sealed class OtterGuiHandler : IDisposable
{
    public MoodleFileSystem MoodleFileSystem;
    public PresetFileSystem PresetFileSystem;
    public Logger Logger;
    public AutomationList AutomationList;
    public Whitelist Whitelist;
    public OtterGuiHandler()
    {
        try
        {
            Logger = new();
            MoodleFileSystem = new(this);
            PresetFileSystem = new(this);
            AutomationList = new();
            Whitelist = new();
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
