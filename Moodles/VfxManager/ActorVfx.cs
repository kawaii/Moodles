using Dalamud.Game.ClientState.Objects.Types;

namespace Moodles.VfxManager;
public unsafe class ActorVfx : BaseVfx
{
    public ActorVfx(GameObject caster, GameObject target, string path) : this(caster.Address, target.Address, path) { }

    public ActorVfx(IntPtr caster, IntPtr target, string path) : base(path)
    {
        Vfx = (VfxStruct*)P.Memory.ActorVfxCreate(path, caster, target, -1, (char)0, 0, (char)0);
    }

    public override void Remove()
    {
        P.Memory.ActorVfxRemove((nint)Vfx, (char)1);
    }
}