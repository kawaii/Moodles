using OtterGui.Log;

namespace Moodles.OtterGuiHandlers;
#pragma warning disable CS8618 // Failing the constructors try-catch will fail Main() initialization anyways.
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
#pragma warning restore CS8618 // Failing the constructors try-catch will fail Main() initialization anyways.
