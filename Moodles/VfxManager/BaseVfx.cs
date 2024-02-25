using Dalamud.Game.ClientState.Objects.Types;

namespace Moodles.VfxManager;

[StructLayout(LayoutKind.Explicit)]
public unsafe struct VfxStruct
{
    [FieldOffset(0x38)] public byte Flags;
    [FieldOffset(0x50)] public Vector3 Position;
    [FieldOffset(0x60)] public Quat Rotation;
    [FieldOffset(0x70)] public Vector3 Scale;

    [FieldOffset(0x128)] public int ActorCaster;
    [FieldOffset(0x130)] public int ActorTarget;

    [FieldOffset(0x1B8)] public int StaticCaster;
    [FieldOffset(0x1C0)] public int StaticTarget;
}

public abstract unsafe class BaseVfx
{
    public VfxStruct* Vfx;
    public string Path;

    public BaseVfx(string path)
    {
        Path = path;
    }

    public abstract void Remove();

    public void Update()
    {
        if (Vfx == null) return;
        Vfx->Flags |= 0x2;
    }

    public void UpdatePosition(Vector3 position)
    {
        if (Vfx == null) return;
        Vfx->Position = new Vector3
        {
            X = position.X,
            Y = position.Y,
            Z = position.Z
        };
    }

    public void UpdatePosition(GameObject actor)
    {
        if (Vfx == null) return;
        Vfx->Position = actor.Position;
    }

    public void UpdateScale(Vector3 scale)
    {
        if (Vfx == null) return;
        Vfx->Scale = new Vector3
        {
            X = scale.X,
            Y = scale.Y,
            Z = scale.Z
        };
    }

    public void UpdateRotation(Vector3 rotation)
    {
        if (Vfx == null) return;

        var q = Quaternion.CreateFromYawPitchRoll(rotation.X, rotation.Y, rotation.Z);
        Vfx->Rotation = new Quat
        {
            X = q.X,
            Y = q.Y,
            Z = q.Z,
            W = q.W
        };
    }
}