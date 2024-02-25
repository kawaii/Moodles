using Dalamud.Game.ClientState.Objects.Types;

namespace Moodles.VfxManager
{
    public static unsafe class VfxSpawn
    {
        private enum SpawnType
        {
            None,
            Ground,
            Self,
            Target
        }

        public static BaseVfx Vfx { get; private set; }
        public static bool Active => Vfx != null;
        public static long RemoveAt = long.MaxValue;

        public static void SpawnOn(GameObject playerObject, string path, bool canLoop = false)
        {
            if (playerObject == null) return;

            if(Active)
            {
                Remove();
            }
            RemoveAt = Environment.TickCount64 + 2000;
            Vfx = new ActorVfx(playerObject, playerObject, path);
        }

        public static void Tick()
        {
            if(Active && Environment.TickCount64 > RemoveAt)
            {
                PluginLog.Information($"Vfx auto-removed");
                Remove();
                RemoveAt = long.MaxValue;
            }
        }

        public static void Remove()
        {
            if (Vfx != null)
            {
                Vfx?.Remove(); // this also calls InteropRemoved()
                Vfx = null;
            }
        }
    }
}