using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using System.Diagnostics.CodeAnalysis;

namespace Moodles;

// Monitors the lifetime of rendered characters to know when they are valid or not.
// Can switch to BattleChara if we run into issues, but should be fine for now.
public unsafe class CharaWatcher : IDisposable
{
    internal Hook<Character.Delegates.OnInitialize> OnCharaInitializeHook;
    internal Hook<Character.Delegates.Dtor> OnCharaDestroyHook;
    internal Hook<Character.Delegates.Terminate> OnCharaTerminateHook;

    public CharaWatcher()
    {
        OnCharaInitializeHook = Svc.Hook.HookFromAddress<Character.Delegates.OnInitialize>((nint)Character.StaticVirtualTablePointer->OnInitialize, InitCharacter);
        OnCharaTerminateHook = Svc.Hook.HookFromAddress<Character.Delegates.Terminate>((nint)Character.StaticVirtualTablePointer->Terminate, TerminateCharacter);
        OnCharaDestroyHook = Svc.Hook.HookFromAddress<Character.Delegates.Dtor>((nint)Character.StaticVirtualTablePointer->Dtor, DestroyCharacter);

        OnCharaInitializeHook.Enable();
        OnCharaTerminateHook.Enable();
        OnCharaDestroyHook.Enable();

        CollectInitialData();
    }

    // Do not hold Character* as it will access invalid memory after destruction. Cast to it instead.
    public static HashSet<nint> Rendered { get; private set; } = new();

    /// <summary>
    ///     The targeted player character. Null if not a player, or no target.
    /// </summary>
    public static Character* PlayerTarget => Svc.Targets.Target is IPlayerCharacter t ? AsCharacter(t.Address) : null;

    public static bool LocalPlayerRendered => Rendered.Contains(LocalPlayer.Address);

    public void Dispose()
    {
        OnCharaInitializeHook?.Dispose();
        OnCharaTerminateHook?.Dispose();
        OnCharaDestroyHook?.Dispose();
        // Clear out tracked pointers.
        Rendered.Clear();
    }

    private void CollectInitialData()
    {
        var objects = GameObjectManager.Instance();
        // Standard Actor Handling.
        for (var i = 0; i < 200; i++)
        {
            GameObject* obj = objects->Objects.IndexSorted[i];
            if (obj is null)
                continue;

            // Only process characters.
            if (!obj->IsCharacter())
                continue;

            if (obj->GetObjectKind() is not (ObjectKind.Pc))
            {
                PluginLog.Verbose($"[CharaWatcher] Skipping found character of object kind {obj->GetObjectKind()} at index {i}");
                continue;
            }

            AddToWatcher((Character*)obj);
        }

        // Dont need to care about GPose actors, but if we do we can scan from
        // 201 to GameObjectManager.Instance()->Objects.IndexSorted.Length
    }

    /// <summary>
    ///     Obtain the Character* of a rendered address, or null if not present.
    ///     (For people too lazy to cast I guess.)
    /// </summary>
    public static unsafe Character* AsCharacter(nint address)
    {
        return Rendered.Contains(address) ? (Character*)address : null;
    }

    public static bool TryGetFirst(Func<Character, bool> predicate, [NotNullWhen(true)] out nint charaAddr)
    {
        foreach (Character* addr in Rendered)
        {
            if (predicate(*addr))
            {
                charaAddr = (nint)addr;
                return true;
            }
        }
        charaAddr = nint.Zero;
        return false;
    }

    public static unsafe bool TryGetFirstUnsafe(Func<Character, bool> predicate, [NotNullWhen(true)] out Character* character)
    {
        foreach (Character* addr in Rendered)
        {
            if (predicate(*addr))
            {
                character = addr;
                return true;
            }
        }
        character = null;
        return false;
    }

    /// <summary>
    ///     Obtain a Character* if rendered, returning false otherwise.
    /// </summary>
    public static unsafe bool TryGetValue(nint address, [NotNullWhen(true)] out Character* character)
    {
        if (Rendered.Contains(address))
        {
            character = (Character*)address;
            return true;
        }
        character = null;
        return false;
    }

    private unsafe void InitCharacter(Character* character)
    {
        try { OnCharaInitializeHook!.OriginalDisposeSafe(character); }
        catch (Exception e) { e.Log(); }
        Svc.Framework.Run(() => AddToWatcher(character));
    }

    private unsafe void TerminateCharacter(Character* character)
    {
        RemoveCharacter(character);
        try { OnCharaTerminateHook!.OriginalDisposeSafe(character); }
        catch (Exception e) { e.Log(); }
    }

    private unsafe GameObject* DestroyCharacter(Character* character, byte freeMemory)
    {
        RemoveCharacter(character);
        try { return OnCharaDestroyHook!.OriginalDisposeSafe(character, freeMemory); }
        catch (Exception e) { e.Log(); return null; }
    }

    private void AddToWatcher(Character* chara)
    {
        if (chara is null
            || chara->ObjectIndex < 0 || chara->ObjectIndex >= 200
            || chara->IsCharacter() == false
            || chara->GetObjectKind() is not ObjectKind.Pc)
            return;

        if (Rendered.Add((nint)chara))
        {
            var charaNameWorld = chara->GetNameWithWorld();
            PluginLog.Verbose($"Added rendered character: Rendered: {(nint)chara:X} - {charaNameWorld}");
            // If the player had an associated StatusManager, assign their Character* to it.
            if (C.StatusManagers.TryGetValue(charaNameWorld, out var sm))
            {
                PluginLog.Verbose($"Assigning {charaNameWorld} to SM. [SM Owner Null: {sm.Owner is null} | Chara Null: {chara is null}]");
                sm.Owner = chara;
            }
        }
    }

    private void RemoveCharacter(Character* chara)
    {
        if (Rendered.Contains((nint)chara))
        {
            if (Rendered.Remove((nint)chara))
            {
                var charaNameWorld = chara->GetNameWithWorld();
                PluginLog.Verbose($"Removed rendered character: Rendered: {(nint)chara:X} - {charaNameWorld}");
                // If the removed character was a player with a status manager, handle Owner cleanup.
                if (C.StatusManagers.TryGetValue(charaNameWorld, out var sm))
                {
                    if (sm.OwnerValid)
                    {
                        sm.Owner = null;
                    }

                    // If Ephemeral, remove their status manager and any SeenPlayers entry.
                    if (sm.Ephemeral)
                    {
                        C.StatusManagers.Remove(charaNameWorld);
                        P.SeenPlayers.RemoveAll(x => x.Name == charaNameWorld);
                        PluginLog.Debug($"Removing ephemeral status manager for {charaNameWorld}");
                    }
                }
            }
        }
    }
}
