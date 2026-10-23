using System;
using GrimDynasty.TaskItems;

// Own observation fixtures for two separate bounded Actor constructor segments.
// Preferred-image values are numeric identity tokens, never native pointers.
// No files, native calls, construction prerequisites or old suites are run.
class ActorConstructorPrefixTests
{
    static int assertions, groups;
    static readonly int[] strings = { 0x208, 0x228, 0x250, 0x270, 0x290, 0x2B0,
        0x2D0, 0x2F0, 0x388, 0x3D8, 0x440, 0x4E8 };
    static void Need(bool value, string why) { assertions++; if (!value) throw new Exception(why); }
    static bool In(int at, int first, int last) { return at >= first && at <= last; }
    // Independent union of final changed-byte intervals from the saved body;
    // this does not reproduce the implementation's store order or helper calls.
    static bool Written(int at)
    {
        if (In(at, 0, 7) || In(at, 0x30, 0x3F) || In(at, 0x1E8, 0x200) ||
            In(at, 0x310, 0x387) || In(at, 0x3D0, 0x3D3) || In(at, 0x3F8, 0x400) ||
            In(at, 0x404, 0x410) || In(at, 0x414, 0x418) || In(at, 0x420, 0x427) ||
            In(at, 0x438, 0x43F) || In(at, 0x510, 0x527)) return true;
        for (int i = 0; i < strings.Length; i++)
            if (at == strings[i] || In(at, strings[i] + 0x10, strings[i] + 0x1F)) return true;
        return false;
    }
    static bool EntryWritten(int at)
    {
        return In(at, 0, 8) || In(at, 0x18, 0x27) || In(at, 0x30, 0x3F) ||
            In(at, 0x78, 0x8F) || at == 0xA8 || In(at, 0xB8, 0xC7) ||
            In(at, 0xD8, 0xDF) || In(at, 0x140, 0x143);
    }
    static byte?[] Filled(byte value)
    {
        var bytes = new byte?[0x530];
        for (int i = 0; i < bytes.Length; i++) bytes[i] = value;
        return bytes;
    }
    static bool Equal(byte?[] left, byte?[] right)
    {
        if (left.Length != right.Length) return false;
        for (int i = 0; i < left.Length; i++) if (left[i] != right[i]) return false;
        return true;
    }
    static bool PreservedOutside(ActorConstructorPrefixState state, byte?[] before)
    {
        for (int i = 0; i < before.Length; i++)
            if (!Written(i) && state.ReadByte(i) != before[i]) return false;
        return true;
    }
    static bool RangeIs(ActorConstructorPrefixState state, int first, int last, byte value)
    {
        for (int i = first; i <= last; i++) if (state.ReadByte(i) != value) return false;
        return true;
    }
    static bool ArgumentRejected(Action action)
    {
        try { action(); } catch (ArgumentException) { return true; }
        return false;
    }
    static void Run(string name, Action body)
    {
        int before = assertions;
        body(); groups++;
        Console.WriteLine("PASS group " + groups + " " + name + ": " + (assertions - before) + " assertions");
    }

    static void UnknownObservation()
    {
        var state = new ActorConstructorPrefixState();
        byte?[] before = state.CopyObservation(); bool unknown = true;
        for (int i = 0; i < before.Length; i++) unknown &= !before[i].HasValue;
        Need(before.Length == 0x530 && ActorConstructorPrefixState.CapturedExtent == 0x530 && unknown,
            "new bounded storage starts entirely unknown");
        var result = ActorConstructorPrefix.ApplyAfterNormalBaseReturn(state);
        Need(result.Status == "KnownPrefix" && result.Reason == null && Object.ReferenceEquals(result.State, state),
            "normal local completion retains the supplied state");
        Need(result.StoreInstructions == 71 && result.MayHaveEffects && !result.IsCompleteConstructor &&
            result.ResumeInstructionRva == 0x25C55U, "71 stores stop at the next unmodeled instruction");
        int known = 0; bool exactMask = true;
        for (int i = 0; i < 0x530; i++)
        {
            bool has = state.ReadByte(i).HasValue;
            if (has) known++;
            exactMask &= has == Written(i);
        }
        Need(exactMask && known == 444, "exactly 444 changed bytes become known; every untouched byte stays unknown");
        Need(state.ReadLittleEndian(0, 8) == 0x18033A0D8UL && state.ReadLittleEndian(0x30, 8) == 0x18033A098UL &&
            state.ReadLittleEndian(0x38, 8) == 0x18033F078UL, "three preferred-image identity tokens retain all 64 bits");
        Need(RangeIs(state, 0x1E8, 0x200, 0) && RangeIs(state, 0x310, 0x387, 0) &&
            RangeIs(state, 0x510, 0x527, 0), "the three zero regions include their complete qwords and final flag");
        Need(!state.ReadLittleEndian(0x1E0, 8).HasValue && !state.ReadLittleEndian(0x3B0, 4).HasValue &&
            !state.ReadLittleEndian(0x528, 4).HasValue, "later constructor fields are not manufactured as zero");
    }

    static void MixedInputAndNarrowWrites()
    {
        byte?[] seed = Filled(0xA5);
        for (int i = 0; i < seed.Length; i += 7) seed[i] = null;
        seed[0x3D0] = 0x12; seed[0x3D1] = null; seed[0x401] = null; seed[0x402] = 0x77;
        var state = new ActorConstructorPrefixState(seed);
        Need(state.ReadByte(0x3D0) == 0x12 && !state.ReadByte(0x3D1).HasValue && state.ReadByte(0x402) == 0x77,
            "fixture mixes nonzero and unknown input within and beside narrow writes");
        ActorConstructorPrefix.ApplyAfterNormalBaseReturn(state);
        Need(state.ReadLittleEndian(0x3D0, 4) == 0xFFFFFFFFUL && state.ReadLittleEndian(0x404, 4) == 0x3F4CCCCDUL &&
            state.ReadLittleEndian(0x414, 4) == 0x40B00000UL, "raw dword constants match the saved instruction bits");
        Need(state.ReadLittleEndian(0x408, 8) == 0xAC44UL && state.ReadLittleEndian(0x40C, 4) == 0UL,
            "AC44 is a full qword store, clearing the upper dword");
        Need(state.ReadByte(0x400) == 0 && state.ReadByte(0x410) == 0 && state.ReadByte(0x418) == 0 &&
            state.ReadLittleEndian(0x3F8, 8) == 0UL && state.ReadLittleEndian(0x420, 8) == 0UL &&
            state.ReadLittleEndian(0x438, 8) == 0UL, "byte flags and separate qword fields have the observed widths");
        Need(PreservedOutside(state, seed), "all unobserved neighbors preserve their exact known or unknown input");
        Need(!state.ReadLittleEndian(0x400, 8).HasValue && state.ReadByte(0x402) == 0x77,
            "writing one byte does not normalize the enclosing partly unknown qword");
    }

    static void TwelveInlineStrings()
    {
        byte?[] seed = Filled(0xD3); var state = new ActorConstructorPrefixState(seed);
        ActorConstructorPrefix.ApplyAfterNormalBaseReturn(state);
        bool first = true, metadata = true, tails = true, partialQword = true;
        for (int i = 0; i < strings.Length; i++)
        {
            int at = strings[i];
            first &= state.ReadByte(at) == 0;
            metadata &= state.ReadLittleEndian(at + 0x10, 8) == 0UL && state.ReadLittleEndian(at + 0x18, 8) == 15UL;
            tails &= RangeIs(state, at + 1, at + 15, 0xD3);
            partialQword &= state.ReadLittleEndian(at, 8) == 0xD3D3D3D3D3D3D300UL;
        }
        Need(strings.Length == 12 && first, "all twelve inline payload first bytes are zero");
        Need(metadata, "all twelve length and capacity qwords become zero and fifteen");
        Need(tails, "all 180 remaining inline payload bytes retain their previous bits");
        Need(partialQword, "a first-byte store must not become a full payload qword clear");
        Need(PreservedOutside(state, seed), "string initialization preserves every gap and unrelated field");
    }

    static void StopBeforeDependencies()
    {
        var state = new ActorConstructorPrefixState(Filled(0x5A));
        var result = ActorConstructorPrefix.ApplyAfterNormalBaseReturn(state);
        Need(ActorConstructorPrefix.ActorEntryRva == 0x259A0U && ActorConstructorPrefix.RequiredPriorCallRva == 0x7D020U,
            "the explicit entry contract requires the preceding base-like call to have returned normally");
        Need(result.ResumeInstructionRva == 0x25C55U && ActorConstructorPrefix.PendingCallSiteRva == 0x25C5FU &&
            ActorConstructorPrefix.PendingCallTargetRva == 0x22CE0U && !result.IsCompleteConstructor,
            "the prefix stops before preparation and invocation of the next helper");
        Need(ActorConstructorPrefix.PendingThisOffset == 0x250 && ActorConstructorPrefix.PendingSourcePreferredIdentity == 0x180307E61UL &&
            ActorConstructorPrefix.PendingLength == 0UL, "pending argument metadata is explicit and does not perform a call");
        Need(RangeIs(state, 0x1E0, 0x1E7, 0x5A) && RangeIs(state, 0x3B0, 0x3CF, 0x5A) &&
            state.ReadByte(0x248) == 0x5A && state.ReadByte(0x4C0) == 0x5A,
            "later allocated-object identity, scalar fields and flags stay untouched");
        Need(RangeIs(state, 0x464, 0x473, 0x5A) && RangeIs(state, 0x49C, 0x4A7, 0x5A) &&
            RangeIs(state, 0x4E0, 0x4E7, 0x5A) && RangeIs(state, 0x528, 0x52F, 0x5A),
            "constant-load destinations, global-derived fields and final stores are not guessed");
        Need(RangeIs(state, 8, 0x2F, 0x5A) && RangeIs(state, 0x40, 0x1DF, 0x5A),
            "the model preserves previously constructed base fields outside its three identity-token stores");
    }

    static void IdentityRepetitionAndIsolation()
    {
        byte?[] seed = Filled(0x6C); byte?[] seedBefore = (byte?[])seed.Clone();
        var first = new ActorConstructorPrefixState(seed); var second = new ActorConstructorPrefixState(seed); var alias = first;
        var initialCopy = first.CopyObservation();
        var result = ActorConstructorPrefix.ApplyAfterNormalBaseReturn(first);
        Need(Object.ReferenceEquals(result.State, alias) && alias.ReadLittleEndian(0, 8) == 0x18033A0D8UL,
            "the existing state alias observes writes without replacement");
        Need(Equal(second.CopyObservation(), seedBefore) && Equal(seed, seedBefore) && Equal(initialCopy, seedBefore),
            "a second state, input array and earlier output snapshot remain independent");
        byte?[] once = first.CopyObservation(); var again = ActorConstructorPrefix.ApplyAfterNormalBaseReturn(first);
        Need(again.Status == "KnownPrefix" && again.StoreInstructions == 71 && Object.ReferenceEquals(again.State, first) &&
            Equal(first.CopyObservation(), once), "repeating the local segment repeats 71 stores without accumulating counts or clearing more bytes");
        seed[0x10] = 0; seed[0x208] = 0;
        Need(first.ReadByte(0x10) == 0x6C && second.ReadByte(0x10) == 0x6C && second.ReadByte(0x208) == 0x6C,
            "later input-array mutation does not alias either owned observation");
        var secondResult = ActorConstructorPrefix.ApplyAfterNormalBaseReturn(second);
        Need(Object.ReferenceEquals(secondResult.State, second) && !Object.ReferenceEquals(first, second) &&
            Equal(first.CopyObservation(), second.CopyObservation()), "independent equal input states converge without sharing identity");
    }

    static void OwnGuardsAndOutputCopies()
    {
        var absent = ActorConstructorPrefix.ApplyAfterNormalBaseReturn(null);
        Need(absent.Status == "Unsupported" && absent.Reason == "UnknownActorFieldStorage" && absent.State == null,
            "null own storage is explicitly unsupported");
        Need(absent.StoreInstructions == 0 && !absent.MayHaveEffects && !absent.IsCompleteConstructor &&
            absent.ResumeInstructionRva == 0x259C1U, "null guard has no observed prefix effects");
        Need(ArgumentRejected(delegate { new ActorConstructorPrefixState(null); }) &&
            ArgumentRejected(delegate { new ActorConstructorPrefixState(new byte?[0x52F]); }),
            "observation construction rejects missing or incorrectly bounded input");
        var state = new ActorConstructorPrefixState(Filled(0x81));
        byte?[] copy = state.CopyObservation(); byte?[] other = state.CopyObservation();
        copy[0x10] = 0; copy[0x20] = null;
        Need(!Object.ReferenceEquals(copy, other) && state.ReadByte(0x10) == 0x81 && state.ReadByte(0x20) == 0x81 &&
            other[0x10] == 0x81 && other[0x20] == 0x81, "exported snapshots cannot mutate the state or one another");
        Need(ArgumentRejected(delegate { state.ReadByte(-1); }) && ArgumentRejected(delegate { state.ReadByte(0x530); }) &&
            ArgumentRejected(delegate { state.ReadLittleEndian(0, 0); }) && ArgumentRejected(delegate { state.ReadLittleEndian(0, 9); }) &&
            ArgumentRejected(delegate { state.ReadLittleEndian(0x529, 8); }) && state.ReadLittleEndian(0x528, 8) == 0x8181818181818181UL,
            "own observation bounds reject invalid reads while preserving the final valid qword");
    }

    static void EntryUnknownObservation()
    {
        var state = new ActorConstructorPrefixState();
        var result = ActorConstructorPrefix.ApplyEntryUntilFirstSubobject(state);
        Need(result.Status == "KnownPrefix" && result.Reason == null && Object.ReferenceEquals(result.State, state) &&
            result.StoreInstructions == 15 && result.MayHaveEffects && !result.IsCompleteConstructor &&
            result.ResumeInstructionRva == 0x7D0C8U, "entry stops inside 07D020 after fifteen stores, before the subobject setup");
        Need(state.ReadLittleEndian(0, 8) == 0x18033E538UL && state.ReadLittleEndian(0x30, 8) == 0x18033DF48UL &&
            state.ReadLittleEndian(0x38, 8) == 0x18033F078UL,
            "final entry tokens reflect the overwrite of 339F78 and do not advance to Actor's post-base tokens");
        Need(state.ReadByte(8) == 0 && state.ReadLittleEndian(0x18, 8) == 0UL && state.ReadLittleEndian(0x20, 8) == 15UL &&
            state.ReadByte(0xA8) == 0 && state.ReadLittleEndian(0xB8, 8) == 0UL && state.ReadLittleEndian(0xC0, 8) == 15UL,
            "both entry inline strings have only their first byte, length and capacity initialized");
        Need(RangeIs(state, 0x78, 0x8F, 0) && state.ReadLittleEndian(0xD8, 8) == 0UL && state.ReadLittleEndian(0x140, 4) == 0UL,
            "entry qwords and the final dword match the independent byte-width goldens");
        int known = 0; bool exact = true;
        for (int i = 0; i < 0x530; i++)
        {
            bool has = state.ReadByte(i).HasValue;
            if (has) known++;
            exact &= has == EntryWritten(i);
        }
        Need(exact && known == 94, "entry's full observation has exactly 94 known bytes and no inferred neighbors");
        Need(!state.ReadByte(0x198).HasValue && !state.ReadLittleEndian(0x1E8, 8).HasValue &&
            !state.ReadByte(0x208).HasValue && !state.ReadLittleEndian(0x3D0, 4).HasValue,
            "neither the pending subobject nor Actor's post-base segment has run");
        Need(ActorConstructorPrefix.FirstSubobjectCallSiteRva == 0x7D0DCU && ActorConstructorPrefix.FirstSubobjectTargetRva == 0xEBDE0U &&
            ActorConstructorPrefix.FirstSubobjectThisOffset == 0x198 && ActorConstructorPrefix.FirstSubobjectEdx == 4U,
            "first unmodeled subobject call retains its exact site, target and arguments");
        var absent = ActorConstructorPrefix.ApplyEntryUntilFirstSubobject(null);
        Need(absent.Status == "Unsupported" && absent.Reason == "UnknownActorFieldStorage" && absent.State == null &&
            absent.StoreInstructions == 0 && !absent.MayHaveEffects && !absent.IsCompleteConstructor && absent.ResumeInstructionRva == 0x259A0U,
            "entry null guard stops at the Actor entry rather than claiming arrival in the base body");
    }

    static void EntryMixedNeighborsAndStop()
    {
        byte?[] seed = Filled(0x6E); seed[9] = null; seed[0xAE] = null;
        seed[0x141] = null; seed[0x144] = 0x91; seed[0x145] = null; seed[0x1E8] = null;
        // An already observed Object token/empty-name layout does not certify
        // completion of 07D020 or permit skipping to Actor's post-base segment.
        byte[] objectToken = { 0x78, 0x9F, 0x33, 0x80, 1, 0, 0, 0 };
        for (int i = 0; i < objectToken.Length; i++) seed[i] = objectToken[i];
        seed[8] = 0;
        for (int i = 0x18; i < 0x28; i++) seed[i] = 0;
        seed[0x20] = 15;
        var state = new ActorConstructorPrefixState(seed); var alias = state;
        bool objectFieldsKnown = state.ReadLittleEndian(0, 8) == 0x180339F78UL && state.ReadByte(8) == 0 &&
            state.ReadLittleEndian(0x18, 8) == 0UL && state.ReadLittleEndian(0x20, 8) == 15UL;
        var result = ActorConstructorPrefix.ApplyEntryUntilFirstSubobject(state);
        bool preserved = true;
        for (int i = 0; i < seed.Length; i++) if (!EntryWritten(i)) preserved &= state.ReadByte(i) == seed[i];
        Need(preserved, "entry preserves every untouched byte across the complete mixed observation");
        bool tails = true;
        for (int i = 1; i < 16; i++) tails &= state.ReadByte(8 + i) == seed[8 + i] && state.ReadByte(0xA8 + i) == seed[0xA8 + i];
        Need(tails && state.ReadByte(8) == 0 && state.ReadByte(0xA8) == 0,
            "both entry string tails preserve nonzero and unknown bytes after the single-byte terminator writes");
        Need(state.ReadLittleEndian(0x140, 4) == 0UL && state.ReadByte(0x144) == 0x91 &&
            !state.ReadByte(0x145).HasValue && !state.ReadLittleEndian(0x140, 8).HasValue,
            "entry's final dword does not clear its unknown or nonzero high-qword neighbors");
        Need(objectFieldsKnown && Object.ReferenceEquals(result.State, alias) && result.StoreInstructions == 15 &&
            alias.ReadLittleEndian(0, 8) == 0x18033E538UL && alias.ReadLittleEndian(0x30, 8) == 0x18033DF48UL,
            "preexisting Object fields do not bypass the fifteen-store entry segment or its base-stage token overwrite");
        bool later = true;
        for (int i = 0x198; i < 0x530; i++) later &= state.ReadByte(i) == seed[i];
        Need(later && result.ResumeInstructionRva == 0x7D0C8U && !result.IsCompleteConstructor,
            "entry cannot implicitly complete the subobject, return from base or execute Actor's later segment");
        byte?[] once = state.CopyObservation(); var again = ActorConstructorPrefix.ApplyEntryUntilFirstSubobject(state);
        Need(again.StoreInstructions == 15 && again.ResumeInstructionRva == 0x7D0C8U && Equal(once, state.CopyObservation()),
            "repeating entry stays within its own fifteen-store prefix");
    }

    static int Main()
    {
        try
        {
            Run("UnknownObservation", UnknownObservation);
            Run("MixedInputAndNarrowWrites", MixedInputAndNarrowWrites);
            Run("TwelveInlineStrings", TwelveInlineStrings);
            Run("StopBeforeDependencies", StopBeforeDependencies);
            Run("IdentityRepetitionAndIsolation", IdentityRepetitionAndIsolation);
            Run("OwnGuardsAndOutputCopies", OwnGuardsAndOutputCopies);
            Run("EntryUnknownObservation", EntryUnknownObservation);
            Run("EntryMixedNeighborsAndStop", EntryMixedNeighborsAndStop);
            Console.WriteLine("PASS: " + assertions + " assertions, " + groups + " scenario groups");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine("FAIL after " + assertions + " assertions, " + groups + " completed scenario groups: " + error.Message);
            return 1;
        }
    }
}
