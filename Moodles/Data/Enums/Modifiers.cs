using System.Runtime.CompilerServices;

namespace Moodles.Data;

/// <summary>
///     How a moodle can modify its behavior to use special interactions. <para />
///     This allows for future proofing of additions by not tamping with the 
///     MemoryPack serialization and only adding on new strings and numbers at the,
///     without it feeling too messy.
/// </summary>
/// <remarks> 
///     This could be turned to a list if desired, but checking a list during processor 
///     updates could be most costly than a bitwise check. Best alternative would be a HashSet.
/// </remarks>
[Flags]
public enum Modifiers : uint // use uint to allow for futureproof options.
{
    None                = 0,
    CanDispel           = 1u << 0, // Can be dispelled.
    StacksIncrease      = 1u << 1, // Stackable moodles, when reapplied, can increase their stack count.
    StacksRollOver      = 1u << 2, // When a stack reaches its cap, it starts over and counts up again.
    PersistExpireTime   = 1u << 3, // When reapplied, the expire time remains the same.
    StacksMoveToChain   = 1u << 4, // When a ChainStatus trigger occurs, the current stacks are is carried over.
    StacksCarryToChain  = 1u << 5, // When the stacks increase and hit max, remaining stacks carry over.
    PersistAfterTrigger = 1u << 6, // When a ChainStatus trigger occurs, the original moodle remains.
    // Ideas: Persist original after chain trigger, ext.. 
}

// What must occur for a chained status to trigger.
// Could be expanded upon to be caused by many things.
public enum ChainTrigger
{
    Dispel = 0,
    HitMaxStacks = 1,
    TimerExpired = 2,
}

// Bitwise operation help for Status Modifiers.
public static class ModifierExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Has(this Modifiers value, Modifiers flag)
        => (value & flag) == flag;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasAny(this Modifiers value, Modifiers flags)
        => (value & flags) != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Set(ref this Modifiers value, Modifiers flag, bool enabled)
    {
        if (enabled) value |= flag;
        else value &= ~flag;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Clear(ref this Modifiers value, Modifiers flag)
        => value &= ~flag;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Toggle(ref this Modifiers value, Modifiers flag)
        => value ^= flag;
}


