using System;
using GrimDynasty.TaskItems;

// Bounded own byte observations only. These fixtures do not allocate native
// objects, certify the unknown call-check helper, or run earlier test suites.
class ActorConstructorArrayTests
{
    static int assertions, groups;
    static void Need(bool value, string why) { assertions++; if (!value) throw new Exception(why); }
    static bool In(int value, int first, int last) { return value >= first && value <= last; }
    static byte?[] Filled(byte value)
    {
        var bytes = new byte?[0x208];
        for (int i = 0; i < bytes.Length; i++) bytes[i] = value;
        return bytes;
    }
    static bool Equal(byte?[] left, byte?[] right)
    {
        if (left.Length != right.Length) return false;
        for (int i = 0; i < left.Length; i++) if (left[i] != right[i]) return false;
        return true;
    }
    // Semantic final-byte oracle, independent of the fourteen-store sequence:
    // two empty inline strings; zero qword regions; one signed -2 dword.
    // Null means this leaf must preserve the prior byte, not clear it.
    static byte? LeafValue(int relative)
    {
        if (relative == 0 || relative == 0x20) return 0;
        if (In(relative, 0x10, 0x1F)) return relative == 0x18 ? (byte)15 : (byte)0;
        if (In(relative, 0x30, 0x3F)) return relative == 0x38 ? (byte)15 : (byte)0;
        if (In(relative, 0x40, 0x5F) || In(relative, 0x68, 0x7F)) return 0;
        if (relative == 0x60) return 0xFE;
        if (In(relative, 0x61, 0x63)) return 0xFF;
        return null;
    }
    static bool MatchesLeaf(Actor198ArrayStorage storage, byte?[] before, int index)
    {
        int start = 8 + index * 0x80;
        for (int i = 0; i < before.Length; i++)
        {
            byte? changed = LeafValue(i - start);
            if (storage.ReadByte(i) != (changed.HasValue ? changed : before[i])) return false;
        }
        return true;
    }
    static bool RegionIs(Actor198ArrayStorage storage, int first, int last, byte value)
    {
        for (int i = first; i <= last; i++) if (storage.ReadByte(i) != value) return false;
        return true;
    }
    static bool Rejects(Action action)
    {
        try { action(); } catch (ArgumentException) { return true; }
        return false;
    }
    static bool EarlyActorWritten(int at)
    {
        return In(at, 0, 8) || In(at, 0x18, 0x27) || In(at, 0x30, 0x3F) || In(at, 0x78, 0x8F) ||
            at == 0xA8 || In(at, 0xB8, 0xC7) || In(at, 0xD8, 0xDF) || In(at, 0x140, 0x143) ||
            In(at, 0x198, 0x1A3) || In(at, 0x1A8, 0x1B4);
    }
    static bool EarlyActorGolden(ActorConstructorPrefixState state)
    {
        return state.ReadLittleEndian(0, 8) == 0x18033E538UL && state.ReadLittleEndian(0x30, 8) == 0x18033DF48UL &&
            state.ReadLittleEndian(0x38, 8) == 0x18033F078UL && state.ReadByte(8) == 0 &&
            state.ReadLittleEndian(0x18, 8) == 0UL && state.ReadLittleEndian(0x20, 8) == 15UL &&
            state.ReadLittleEndian(0x78, 8) == 0UL && state.ReadLittleEndian(0x80, 8) == 0UL && state.ReadLittleEndian(0x88, 8) == 0UL &&
            state.ReadByte(0xA8) == 0 && state.ReadLittleEndian(0xB8, 8) == 0UL && state.ReadLittleEndian(0xC0, 8) == 15UL &&
            state.ReadLittleEndian(0xD8, 8) == 0UL && state.ReadLittleEndian(0x140, 4) == 0UL &&
            state.ReadLittleEndian(0x198, 8) == 0x180339B70UL && state.ReadLittleEndian(0x1A0, 4) == 0UL &&
            state.ReadLittleEndian(0x1A8, 8) == 0UL && state.ReadLittleEndian(0x1B0, 4) == 4UL && state.ReadByte(0x1B4) == 0;
    }
    static byte?[] ActorFilled(byte value)
    {
        var bytes = new byte?[0x530];
        for (int i = 0; i < bytes.Length; i++) bytes[i] = value;
        return bytes;
    }
    static readonly int[] actorStrings = { 0x208, 0x228, 0x250, 0x270, 0x290, 0x2B0,
        0x2D0, 0x2F0, 0x388, 0x3D8, 0x440, 0x4E8 };
    // Final changed-byte union of the three distinct native segments, not a
    // replay of their store sequence. Unlisted bytes must retain the input.
    static bool CompleteActorWritten(int at)
    {
        if (EarlyActorWritten(at) || at == 0x40 || In(at, 0x44, 0x74) || In(at, 0x90, 0xA7) ||
            In(at, 0xC8, 0xCC) || In(at, 0xD0, 0xD7) || at == 0x118 || at == 0x11A ||
            In(at, 0x120, 0x13E) || In(at, 0x174, 0x175) || In(at, 0x178, 0x184) || In(at, 0x188, 0x18C) ||
            In(at, 0x1B8, 0x1B9) || In(at, 0x1BC, 0x1C3) || In(at, 0x1C8, 0x1DF) ||
            In(at, 0x1E8, 0x200) || In(at, 0x310, 0x387) || In(at, 0x3D0, 0x3D3) ||
            In(at, 0x3F8, 0x400) || In(at, 0x404, 0x410) || In(at, 0x414, 0x418) ||
            In(at, 0x420, 0x427) || In(at, 0x438, 0x43F) || In(at, 0x510, 0x527)) return true;
        for (int i = 0; i < actorStrings.Length; i++)
            if (at == actorStrings[i] || In(at, actorStrings[i] + 0x10, actorStrings[i] + 0x1F)) return true;
        return false;
    }
    static bool ConstructedArrayGolden(Actor198ArrayStorage storage)
    {
        // The initial memset owns every byte of this oracle, including the
        // 34 bytes each isolated leaf deliberately leaves unmodified.
        for (int i = 0; i < 0x208; i++)
        {
            byte expected = i == 0 ? (byte)4 : (byte)0;
            if (i >= 8) { byte? leaf = LeafValue((i - 8) % 0x80); if (leaf.HasValue) expected = leaf.Value; }
            if (storage.ReadByte(i) != expected) return false;
        }
        return true;
    }
    static void Run(string name, Action body)
    {
        int before = assertions; body(); groups++;
        Console.WriteLine("PASS group " + groups + " " + name + ": " + (assertions - before) + " assertions");
    }

    static void UnknownLeafObservation()
    {
        var storage = new Actor198ArrayStorage(0x100000UL);
        byte?[] before = storage.CopyObservation(); bool unknown = true;
        for (int i = 0; i < before.Length; i++) unknown &= !before[i].HasValue;
        Need(before.Length == 0x208 && unknown, "a known allocation identity does not manufacture known byte contents");
        Actor198ElementConstructor.Apply(storage, 2); int at = 0x108;
        int known = 0, untouched = 0;
        for (int i = 0; i < 0x208; i++) if (storage.ReadByte(i).HasValue) known++;
        for (int i = 0; i < 0x80; i++) if (!storage.ReadByte(at + i).HasValue) untouched++;
        Need(MatchesLeaf(storage, before, 2) && known == 94 && untouched == 34,
            "the whole allocation matches one leaf update: 94 known bytes and 34 unknown element bytes");
        Need(storage.ReadByte(at) == 0 && storage.ReadByte(at + 0x20) == 0 &&
            storage.ReadLittleEndian(at + 0x10, 8) == 0UL && storage.ReadLittleEndian(at + 0x18, 8) == 15UL &&
            storage.ReadLittleEndian(at + 0x30, 8) == 0UL && storage.ReadLittleEndian(at + 0x38, 8) == 15UL,
            "both logical strings have empty length, inline capacity and only a first-byte terminator");
        Need(RegionIs(storage, at + 0x40, at + 0x5F, 0) && RegionIs(storage, at + 0x68, at + 0x7F, 0) &&
            storage.ReadLittleEndian(at + 0x60, 4) == 0xFFFFFFFEUL, "zero regions and the signed-minus-two bit pattern are exact");
        Need(!storage.ReadLittleEndian(at, 8).HasValue && !storage.ReadLittleEndian(at + 0x60, 8).HasValue &&
            !storage.ReadLittleEndian(0, 8).HasValue, "payload tails, dword padding and the cookie stay unknown without memset");
        Need(Actor198ElementConstructor.EntryRva == 0xEBEB0U && Actor198ElementConstructor.ReturnRva == 0xEBEF5U,
            "leaf scope ends at RET before the trailing INT3 bytes");
    }

    static void NonzeroTailAndLastElement()
    {
        byte?[] seed = Filled(0xA5); var storage = new Actor198ArrayStorage(0x200000UL, seed);
        Actor198ElementConstructor.Apply(storage, 3); int at = 0x188;
        Need(MatchesLeaf(storage, seed, 3), "every byte of the allocation matches the final semantic oracle for the last element");
        int preserved = 0; bool tails = true;
        for (int i = 0; i < 0x80; i++) if (!LeafValue(i).HasValue) { preserved++; tails &= storage.ReadByte(at + i) == 0xA5; }
        Need(preserved == 34 && tails && RegionIs(storage, at + 1, at + 15, 0xA5) &&
            RegionIs(storage, at + 0x21, at + 0x2F, 0xA5), "all 34 unwritten bytes preserve nonzero input");
        Need(storage.ReadLittleEndian(at + 0x60, 8) == 0xA5A5A5A5FFFFFFFEUL,
            "the -2 dword does not sign-extend or clear the four bytes at +64..67");
        Need(RegionIs(storage, 0, at - 1, 0xA5) && storage.ReadByte(0x207) == 0,
            "last-element construction reaches the final valid byte without altering the cookie or preceding elements");
        byte?[] once = storage.CopyObservation(); Actor198ElementConstructor.Apply(storage, 3);
        Need(Equal(once, storage.CopyObservation()), "repeating the leaf does not clear additional bytes");
    }

    static void OwnershipAndSeparateElements()
    {
        byte?[] input = Filled(0x7A); byte?[] original = (byte?[])input.Clone();
        var first = new Actor198ArrayStorage(0x300000UL, input); var alias = first;
        var second = new Actor198ArrayStorage(0x400000UL, input);
        byte?[] oldSnapshot = first.CopyObservation();
        input[0x100] = 0; input[0] = null;
        Need(Equal(first.CopyObservation(), original) && Equal(second.CopyObservation(), original),
            "construction owns a copy rather than borrowing the caller's input array");
        Actor198ElementConstructor.Apply(first, 1);
        Need(Object.ReferenceEquals(first, alias) && alias.ReadLittleEndian(0x88 + 0x60, 4) == 0xFFFFFFFEUL &&
            Equal(second.CopyObservation(), original) && Equal(oldSnapshot, original),
            "the existing alias observes writes while another array and an earlier snapshot remain unchanged");
        byte?[] copy = first.CopyObservation(); byte?[] otherCopy = first.CopyObservation();
        copy[0x88 + 0x60] = 0; copy[0] = null;
        Need(!Object.ReferenceEquals(copy, otherCopy) && first.ReadByte(0x88 + 0x60) == 0xFE && first.ReadByte(0) == 0x7A &&
            otherCopy[0x88 + 0x60] == 0xFE && otherCopy[0] == 0x7A, "output copies cannot mutate storage or one another");
        byte?[] afterOne = first.CopyObservation(); Actor198ElementConstructor.Apply(first, 2);
        Need(MatchesLeaf(first, afterOne, 2) && first.ReadLittleEndian(0x88 + 0x18, 8) == 15UL,
            "constructing another element preserves the previous element and all unrelated bytes");
        Actor198ElementConstructor.Apply(second, 0);
        Need(MatchesLeaf(second, original, 0) && RegionIs(first, 8, 0x87, 0x7A) && first.BaseToken != second.BaseToken,
            "independent allocation identities and element selections never cross-update");
    }

    static void LayoutAndOwnGuards()
    {
        var storage = new Actor198ArrayStorage(0x500000UL, Filled(0x39));
        Need(Actor198ArrayStorage.RequestedBytes == 0x208 && Actor198ArrayStorage.ElementCount == 4 &&
            Actor198ArrayStorage.ElementStride == 0x80 && storage.ElementsToken == 0x500008UL &&
            Actor198ArrayStorage.ElementOffset(0) == 8 && Actor198ArrayStorage.ElementOffset(1) == 0x88 &&
            Actor198ArrayStorage.ElementOffset(2) == 0x108 && Actor198ArrayStorage.ElementOffset(3) == 0x188,
            "the fixed cookie-plus-four-elements layout uses numeric identity tokens only");
        Need(Rejects(delegate { new Actor198ArrayStorage(0UL); }) && Rejects(delegate { new Actor198ArrayStorage(ulong.MaxValue); }),
            "missing or overflowing identity spans are rejected as own observation inputs");
        Need(Rejects(delegate { new Actor198ArrayStorage(1UL, null); }) &&
            Rejects(delegate { new Actor198ArrayStorage(1UL, new byte?[0x207]); }) &&
            Rejects(delegate { new Actor198ArrayStorage(1UL, new byte?[0x209]); }), "the observation must cover exactly the fixed allocation span");
        byte?[] before = storage.CopyObservation();
        Need(Rejects(delegate { Actor198ElementConstructor.Apply(storage, -1); }) &&
            Rejects(delegate { Actor198ElementConstructor.Apply(storage, 4); }) && Equal(before, storage.CopyObservation()),
            "out-of-range element indices fail before any byte changes");
        Need(Rejects(delegate { Actor198ElementConstructor.Apply(null, 0); }) && Equal(before, storage.CopyObservation()),
            "missing leaf storage is an own guard, not a fabricated native constructor success");
        Need(Rejects(delegate { storage.ReadByte(-1); }) && Rejects(delegate { storage.ReadByte(0x208); }) &&
            Rejects(delegate { storage.ReadLittleEndian(0, 0); }) && Rejects(delegate { storage.ReadLittleEndian(0, 9); }) &&
            Rejects(delegate { storage.ReadLittleEndian(0x201, 8); }) && storage.ReadLittleEndian(0x200, 8) == 0x3939393939393939UL,
            "read bounds admit the final valid qword and reject invalid spans");
    }

    static void MissingInitialAllocation()
    {
        var state = new ActorConstructorPrefixState();
        var result = ActorConstructorArray.ApplyUntilUnresolvedCheck(state, null);
        Need(result.Status == "Unsupported" && result.Reason == "MissingKnownInitialAllocation" &&
            result.ResumeInstructionRva == 0xEBE41U && Object.ReferenceEquals(result.State, state) && result.Array == null,
            "missing known allocation stops at the allocator call after preserving the supplied Actor state");
        Need(result.ActorStoreInstructions == 20 && result.ArrayStoreInstructions == 0 && result.BulkZeroBytes == 0 &&
            result.RequestedAllocationBytes == 520UL && !result.KnownInitialAllocationUsed,
            "fifteen entry stores and five subobject stores precede the fixed 520-byte request");
        Need(EarlyActorGolden(state), "entry and subobject header values match independently specified native bits");
        bool exactMask = true; int known = 0;
        for (int i = 0; i < 0x530; i++)
        {
            bool has = state.ReadByte(i).HasValue;
            if (has) known++;
            exactMask &= has == EarlyActorWritten(i);
        }
        Need(exactMask && known == 119, "only the 119 bytes in the entry and subobject header become known");
        Need(result.MayHaveEffects && !result.ArrayPublished && !result.BaseReturned && !result.IsCompleteActorConstructor,
            "a retained initialization prefix is neither array publication nor successful base return");
        Need(result.ConstructedElements == 0 && result.PreElementCalls == 0 && !result.ArchivedFileInitialCheckUsed,
            "no element, check or archived-check assumption is invented at the missing-allocation boundary");
        Need(state.ReadLittleEndian(0x1A8, 8) == 0UL && !state.ReadLittleEndian(0x1B8, 2).HasValue &&
            !state.ReadLittleEndian(0x174, 2).HasValue && !state.ReadLittleEndian(0x1E8, 8).HasValue && !state.ReadByte(0x208).HasValue,
            "null-initialized payload field stays unpublished and both later constructor segments remain untouched");
    }

    static void MissingActorDoesNotUseAllocation()
    {
        var array = new Actor198ArrayStorage(0x600000UL, Filled(0xC3)); byte?[] before = array.CopyObservation();
        var result = ActorConstructorArray.ApplyUntilUnresolvedCheck(null, array);
        Need(result.Status == "Unsupported" && result.Reason == "UnknownActorFieldStorage" && result.State == null &&
            Object.ReferenceEquals(result.Array, array) && result.ResumeInstructionRva == 0x259A0U,
            "null Actor storage stops at entry while retaining only the explicit allocation observation reference");
        Need(result.ActorStoreInstructions == 0 && result.ArrayStoreInstructions == 0 && result.BulkZeroBytes == 0 &&
            !result.RequestedAllocationBytes.HasValue && !result.KnownInitialAllocationUsed && !result.MayHaveEffects && Equal(before, array.CopyObservation()),
            "entry guard neither clears a supplied allocation nor reports an allocation request");
        Need(!result.ArrayPublished && !result.BaseReturned && !result.IsCompleteActorConstructor &&
            !result.ArchivedFileInitialCheckUsed && result.ConstructedElements == 0 && result.PreElementCalls == 0,
            "entry guard produces no downstream completion claims");
        var bothMissing = ActorConstructorArray.ApplyUntilUnresolvedCheck(null, null);
        Need(bothMissing.Reason == "UnknownActorFieldStorage" && bothMissing.Array == null && !bothMissing.MayHaveEffects,
            "missing Actor takes precedence when both inputs are absent");
    }

    static void DefaultStopsAtLoadedCheck()
    {
        byte?[] actorSeed = ActorFilled(0x7C); actorSeed[0x1A4] = null; actorSeed[0x1B5] = null;
        byte?[] arraySeed = Filled(0xD4); arraySeed[0] = null; arraySeed[0x100] = null;
        byte?[] arrayInputBefore = (byte?[])arraySeed.Clone();
        var state = new ActorConstructorPrefixState(actorSeed); var array = new Actor198ArrayStorage(0x700000UL, arraySeed);
        var result = ActorConstructorArray.ApplyUntilUnresolvedCheck(state, array);
        Need(result.Status == "Unsupported" && result.Reason == "UnresolvedLoadedPreElementCheck" &&
            result.ResumeInstructionRva == 0x2DCC75U && Object.ReferenceEquals(result.State, state) && Object.ReferenceEquals(result.Array, array),
            "default route stops before the unresolved loaded check with exact state identities");
        Need(ActorConstructorArray.PreElementCallSiteRva == 0x2DCC75U && ActorConstructorArray.PreElementTargetRva == 0x2DD4C0U &&
            ActorConstructorArray.PreElementSlotRva == 0x302D38U, "the unresolved check has the observed call site, helper and indirect slot");
        Need(result.ActorStoreInstructions == 20 && result.ArrayStoreInstructions == 1 && result.BulkZeroBytes == 520 &&
            result.RequestedAllocationBytes == 520UL && result.KnownInitialAllocationUsed && result.MayHaveEffects,
            "known initial allocation permits only full memset and one cookie store after the twenty Actor stores");
        bool exactArray = true;
        for (int i = 0; i < 0x208; i++) exactArray &= array.ReadByte(i) == (byte)(i == 0 ? 4 : 0);
        Need(exactArray && array.ReadLittleEndian(0, 8) == 4UL,
            "the entire allocation is cleared, including unknown bytes, before the count-four cookie is stored");
        bool preserved = true;
        for (int i = 0; i < 0x530; i++) if (!EarlyActorWritten(i)) preserved &= state.ReadByte(i) == actorSeed[i];
        Need(EarlyActorGolden(state) && preserved && state.ReadLittleEndian(0x1A8, 8) == 0UL,
            "the complete Actor observation still has only the early prefix and has not published the payload token");
        Need(result.ConstructedElements == 0 && result.PreElementCalls == 0 && !result.ArrayPublished &&
            !result.BaseReturned && !result.ArchivedFileInitialCheckUsed && !result.IsCompleteActorConstructor,
            "zeroing an allocation does not certify the loaded check, any element, publication or constructor completion");
        bool noConstructors = true;
        for (int i = 0; i < 4; i++)
        {
            int at = 8 + i * 0x80;
            noConstructors &= array.ReadLittleEndian(at + 0x18, 8) == 0UL && array.ReadLittleEndian(at + 0x60, 4) == 0UL;
        }
        Need(noConstructors && Equal(arraySeed, arrayInputBefore),
            "all element capacities and -2 fields remain unconstructed, and the caller's input array is unchanged");
    }

    static void ArchivedConditionalArrayAndScope()
    {
        var state = new ActorConstructorPrefixState(); byte?[] seed = Filled(0xB6); seed[0x11] = null;
        var array = new Actor198ArrayStorage(0x800000UL, seed);
        var result = ActorConstructorArray.ApplyWithArchivedFileInitialCheckTarget(state, array);
        Need(result.Status == "ConditionalPrefix" && result.Reason == null && result.ArchivedFileInitialCheckUsed &&
            result.ResumeInstructionRva == 0x25C55U && !result.IsCompleteActorConstructor &&
            ActorConstructorArray.ArchivedFileInitialCheckTargetRva == 0xCE70U,
            "only the explicitly selected archived-file target permits this conditional prefix");
        Need(result.ActorStoreInstructions == 130 && result.ArrayStoreInstructions == 57 && result.BulkZeroBytes == 520 &&
            result.ConstructedElements == 4 && result.PreElementCalls == 4 && result.KnownInitialAllocationUsed &&
            result.RequestedAllocationBytes == 520UL && result.MayHaveEffects,
            "the composed counts include entry, header, publication, base suffix and Actor suffix exactly once");
        Need(Object.ReferenceEquals(result.State, state) && Object.ReferenceEquals(result.Array, array) &&
            result.ArrayPublished && result.BaseReturned && state.ReadLittleEndian(0x1A8, 8) == 0x800008UL &&
            state.ReadLittleEndian(0x198, 8) == 0x18033E828UL,
            "normal conditional composition publishes the payload identity then applies the base continuation");
        Need(ConstructedArrayGolden(array) && array.ReadLittleEndian(0, 8) == 4UL,
            "all 520 allocation bytes match the cookie-plus-four-elements semantic oracle");
        int zeroPadding = 0; bool clean = true;
        for (int element = 0; element < 4; element++) for (int i = 0; i < 0x80; i++)
            if (!LeafValue(i).HasValue) { zeroPadding++; clean &= array.ReadByte(8 + element * 0x80 + i) == 0; }
        Need(clean && zeroPadding == 136, "memset accounts for all 136 bytes left untouched by the four leaf constructors");
        bool exactActor = true;
        for (int i = 0; i < 0x530; i++) exactActor &= state.ReadByte(i).HasValue == CompleteActorWritten(i);
        Need(exactActor, "the full Actor observation has precisely the combined known-byte mask, preserving every unknown gap");
        Need(state.ReadLittleEndian(0, 8) == 0x18033A0D8UL && state.ReadLittleEndian(0x30, 8) == 0x18033A098UL &&
            state.ReadLittleEndian(0x38, 8) == 0x18033F078UL && state.ReadByte(8) == 0 &&
            state.ReadLittleEndian(0x20, 8) == 15UL && state.ReadLittleEndian(0x1E8, 8) == 0UL && state.ReadLittleEndian(0x268, 8) == 15UL,
            "both existing prefix APIs contribute naturally through the new conditional chain");
        Need(ActorConstructorArray.NextCallSiteRva == 0x25C5FU && ActorConstructorArray.NextCallTargetRva == 0x22CE0U &&
            !state.ReadLittleEndian(0x1E0, 8).HasValue && !state.ReadLittleEndian(0x3B0, 4).HasValue &&
            !state.ReadLittleEndian(0x464, 8).HasValue && !state.ReadByte(0x4E1).HasValue,
            "the conditional chain still stops before the next helper, later allocation and global-derived fields");
    }

    static void ConditionalNarrowStoresAndNeighbors()
    {
        byte?[] seed = ActorFilled(0x6D); seed[0x13F] = null; seed[0x144] = null; seed[0x1BB] = null;
        var state = new ActorConstructorPrefixState(seed);
        var array = new Actor198ArrayStorage(0x900000UL, Filled(0xC5));
        var result = ActorConstructorArray.ApplyWithArchivedFileInitialCheckTarget(state, array);
        bool untouched = true;
        for (int i = 0; i < seed.Length; i++) if (!CompleteActorWritten(i)) untouched &= state.ReadByte(i) == seed[i];
        Need(result.Status == "ConditionalPrefix" && untouched, "every unlisted Actor byte preserves its mixed input across the complete conditional chain");
        Need(state.ReadLittleEndian(0x174, 2) == 0x100UL && state.ReadByte(0x174) == 0 && state.ReadByte(0x175) == 1 &&
            state.ReadByte(0x173) == 0x6D && state.ReadByte(0x176) == 0x6D &&
            state.ReadLittleEndian(0xC8, 4) == 0UL && state.ReadByte(0xCC) == 0 && state.ReadByte(0xCD) == 0x6D,
            "word 0100 and the unaligned CB word retain exact byte order and neighboring storage");
        Need(state.ReadLittleEndian(0x1B8, 2) == 0UL && state.ReadByte(0x1BA) == 0x6D && !state.ReadByte(0x1BB).HasValue &&
            state.ReadLittleEndian(0x1BC, 8) == 0UL && state.ReadLittleEndian(0x1C4, 4) == 0x6D6D6D6DUL,
            "unaligned qword 1BC neither aligns downward nor clears the surrounding gaps");
        Need(state.ReadLittleEndian(0x13C, 2) == 0UL && state.ReadByte(0x13E) == 0 && !state.ReadByte(0x13F).HasValue &&
            state.ReadLittleEndian(0x140, 4) == 0UL && !state.ReadByte(0x144).HasValue &&
            state.ReadByte(0x118) == 1 && state.ReadByte(0x119) == 0x6D && state.ReadByte(0x11A) == 0 && state.ReadByte(0x11B) == 0x6D,
            "word, dword and separated byte flags do not widen into their neighbors");
        Need(state.ReadLittleEndian(0x188, 4) == 3UL && state.ReadByte(0x18C) == 1 && state.ReadByte(0x18D) == 0x6D &&
            state.ReadByte(0x184) == 0 && state.ReadByte(0x185) == 0x6D,
            "base scalar and flag values are applied without normalizing surrounding bytes");
        Need(state.ReadLittleEndian(0x3D0, 4) == 0xFFFFFFFFUL && state.ReadLittleEndian(0x404, 4) == 0x3F4CCCCDUL &&
            state.ReadLittleEndian(0x408, 8) == 0xAC44UL && state.ReadLittleEndian(0x40C, 4) == 0UL &&
            state.ReadLittleEndian(0x414, 4) == 0x40B00000UL, "the integrated Actor suffix preserves raw dword constants and its full qword store");
        bool strings = true;
        for (int i = 0; i < actorStrings.Length; i++)
        {
            int at = actorStrings[i];
            strings &= state.ReadByte(at) == 0 && state.ReadLittleEndian(at + 0x10, 8) == 0UL && state.ReadLittleEndian(at + 0x18, 8) == 15UL;
            for (int tail = 1; tail < 16; tail++) strings &= state.ReadByte(at + tail) == 0x6D;
        }
        Need(strings && ConstructedArrayGolden(array), "Actor inline tails retain input while allocated element tails were cleared by memset");
    }

    static void ExplicitProfileDoesNotChangeDefault()
    {
        byte?[] actorInput = ActorFilled(0x52); byte?[] arrayInput = Filled(0x63);
        var conditionalState = new ActorConstructorPrefixState(actorInput); var defaultState = new ActorConstructorPrefixState(actorInput);
        var conditionalArray = new Actor198ArrayStorage(0xA00000UL, arrayInput); var defaultArray = new Actor198ArrayStorage(0xB00000UL, arrayInput);
        var conditional = ActorConstructorArray.ApplyWithArchivedFileInitialCheckTarget(conditionalState, conditionalArray);
        byte?[] completedActor = conditionalState.CopyObservation(); byte?[] completedArray = conditionalArray.CopyObservation();
        var stopped = ActorConstructorArray.ApplyUntilUnresolvedCheck(defaultState, defaultArray);
        Need(conditional.Status == "ConditionalPrefix" && conditional.ArchivedFileInitialCheckUsed &&
            stopped.Status == "Unsupported" && stopped.Reason == "UnresolvedLoadedPreElementCheck" && !stopped.ArchivedFileInitialCheckUsed,
            "using the explicit file-initializer profile does not silently authorize the default loaded-check path");
        Need(conditional.PreElementCalls == 4 && conditional.ConstructedElements == 4 && conditional.ActorStoreInstructions == 130 &&
            stopped.PreElementCalls == 0 && stopped.ConstructedElements == 0 && stopped.ActorStoreInstructions == 20,
            "each result reports only its own proven conditional or unresolved progress");
        Need(conditionalState.ReadLittleEndian(0x1A8, 8) == 0xA00008UL && defaultState.ReadLittleEndian(0x1A8, 8) == 0UL &&
            conditionalState.ReadLittleEndian(0x198, 8) == 0x18033E828UL && defaultState.ReadLittleEndian(0x198, 8) == 0x180339B70UL &&
            conditionalState.ReadLittleEndian(0, 8) == 0x18033A0D8UL && defaultState.ReadLittleEndian(0, 8) == 0x18033E538UL,
            "publication and stage-specific identity tokens visibly distinguish the two routes");
        Need(conditionalArray.ReadLittleEndian(8 + 0x18, 8) == 15UL && conditionalArray.ReadLittleEndian(8 + 0x60, 4) == 0xFFFFFFFEUL &&
            defaultArray.ReadLittleEndian(8 + 0x18, 8) == 0UL && defaultArray.ReadLittleEndian(8 + 0x60, 4) == 0UL &&
            !stopped.ArrayPublished && !stopped.BaseReturned && !conditional.IsCompleteActorConstructor,
            "cleared allocation bytes remain distinct from constructed elements and neither route claims complete Actor construction");
        Need(Equal(completedActor, conditionalState.CopyObservation()) && Equal(completedArray, conditionalArray.CopyObservation()) &&
            actorInput[0] == 0x52 && arrayInput[0] == 0x63,
            "the later default call cannot mutate an independent completed observation or shared caller inputs");
    }

    static void ArchivedProfilePreservesMissingInputStops()
    {
        var array = new Actor198ArrayStorage(0xC00000UL, Filled(0xE7)); byte?[] before = array.CopyObservation();
        var absent = ActorConstructorArray.ApplyWithArchivedFileInitialCheckTarget(null, array);
        Need(absent.Status == "Unsupported" && absent.Reason == "UnknownActorFieldStorage" && absent.ResumeInstructionRva == 0x259A0U &&
            absent.State == null && Object.ReferenceEquals(absent.Array, array) && Equal(before, array.CopyObservation()),
            "opting into the archived target does not bypass the missing-Actor entry guard");
        Need(!absent.MayHaveEffects && !absent.KnownInitialAllocationUsed && !absent.ArchivedFileInitialCheckUsed &&
            !absent.RequestedAllocationBytes.HasValue && absent.PreElementCalls == 0 && absent.ConstructedElements == 0 &&
            !absent.ArrayPublished && !absent.BaseReturned, "missing Actor leaves every conditional downstream stage unclaimed");
        byte?[] seed = ActorFilled(0x28); var state = new ActorConstructorPrefixState(seed);
        var missing = ActorConstructorArray.ApplyWithArchivedFileInitialCheckTarget(state, null);
        Need(missing.Status == "Unsupported" && missing.Reason == "MissingKnownInitialAllocation" && missing.ResumeInstructionRva == 0xEBE41U &&
            missing.ActorStoreInstructions == 20 && missing.ArrayStoreInstructions == 0 && missing.BulkZeroBytes == 0 &&
            missing.RequestedAllocationBytes == 520UL && missing.MayHaveEffects && EarlyActorGolden(state),
            "missing allocation retains the exact twenty-store prefix without treating a null allocator result as normal success");
        bool preserved = true;
        for (int i = 0; i < seed.Length; i++) if (!EarlyActorWritten(i)) preserved &= state.ReadByte(i) == seed[i];
        Need(preserved && !missing.KnownInitialAllocationUsed && !missing.ArchivedFileInitialCheckUsed && missing.ConstructedElements == 0 &&
            missing.PreElementCalls == 0 && !missing.ArrayPublished && !missing.BaseReturned && !missing.IsCompleteActorConstructor,
            "the explicit profile cannot publish or continue after an unresolved allocation prerequisite");
    }

    static int Main()
    {
        try
        {
            Run("UnknownLeafObservation", UnknownLeafObservation);
            Run("NonzeroTailAndLastElement", NonzeroTailAndLastElement);
            Run("OwnershipAndSeparateElements", OwnershipAndSeparateElements);
            Run("LayoutAndOwnGuards", LayoutAndOwnGuards);
            Run("MissingInitialAllocation", MissingInitialAllocation);
            Run("MissingActorDoesNotUseAllocation", MissingActorDoesNotUseAllocation);
            Run("DefaultStopsAtLoadedCheck", DefaultStopsAtLoadedCheck);
            Run("ArchivedConditionalArrayAndScope", ArchivedConditionalArrayAndScope);
            Run("ConditionalNarrowStoresAndNeighbors", ConditionalNarrowStoresAndNeighbors);
            Run("ExplicitProfileDoesNotChangeDefault", ExplicitProfileDoesNotChangeDefault);
            Run("ArchivedProfilePreservesMissingInputStops", ArchivedProfilePreservesMissingInputStops);
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
