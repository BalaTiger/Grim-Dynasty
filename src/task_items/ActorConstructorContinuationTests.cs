using System;
using GrimDynasty.TaskItems;

// Own, bounded observations only. These fixtures execute no native code and
// establish neither loaded guard behavior nor allocator retries/native EH.
// Existing prefix/array APIs only prepare the new continuation fixtures;
// none of the old test suites is invoked.
public static class ActorConstructorContinuationTests
{
    static int assertions;
    static int groups;

    static void Need(bool condition, string message)
    {
        assertions++;
        if (!condition) throw new Exception(message);
    }

    static void Run(string name, Action body)
    {
        int before = assertions;
        body();
        groups++;
        Console.WriteLine("PASS " + name + ": " + (assertions - before) + " assertions");
    }

    static byte?[] Mixed(int length)
    {
        byte?[] bytes = new byte?[length];
        for (int i = 0; i < bytes.Length; i++) bytes[i] = (byte)(1 + ((i * 37 + 93) % 254));
        return bytes;
    }

    static void SeedWord(byte?[] bytes, int at, int width, ulong value)
    {
        for (int i = 0; i < width; i++) bytes[at + i] = unchecked((byte)(value >> (8 * i)));
    }

    static bool Same(byte?[] left, byte?[] right)
    {
        if (left == null || right == null || left.Length != right.Length) return false;
        for (int i = 0; i < left.Length; i++) if (left[i] != right[i]) return false;
        return true;
    }

    static bool Same(byte[] left, byte[] right)
    {
        if (left == null || right == null || left.Length != right.Length) return false;
        for (int i = 0; i < left.Length; i++) if (left[i] != right[i]) return false;
        return true;
    }

    static bool SameSpan(byte?[] before, byte?[] after, int start, int length)
    {
        for (int i = start; i < start + length; i++) if (before[i] != after[i]) return false;
        return true;
    }

    static bool HasBytes(ActorConstructorPrefixState state, int start, byte[] expected)
    {
        for (int i = 0; i < expected.Length; i++) if (state.ReadByte(start + i) != expected[i]) return false;
        return true;
    }

    // Aggregate bit-value oracle for the eight scalar writes, independent of
    // their implementation order. Neighbor preservation is checked separately.
    static bool ScalarsHaveExpectedBits(ActorConstructorPrefixState state)
    {
        return state.ReadLittleEndian(0x3B0, 4) == 0UL &&
               state.ReadByte(0x3B4) == 0 &&
               state.ReadLittleEndian(0x3B8, 4) == 0x3F800000UL &&
               state.ReadLittleEndian(0x3BC, 4) == 0x3F000000UL &&
               state.ReadLittleEndian(0x3C0, 2) == 1UL &&
               state.ReadLittleEndian(0x3C4, 4) == 0UL &&
               state.ReadByte(0x3C8) == 0 && state.ReadByte(0x460) == 0;
    }

    // Only the bounded write footprint is excluded. Every other observed byte
    // must preserve its original value/unknownness; this catches widened writes.
    static bool OutsideContinuationPreserved(byte?[] before, byte?[] after, bool copiedConstant)
    {
        for (int i = 0; i < before.Length; i++)
        {
            bool written = i == 0x250 || (i >= 0x260 && i <= 0x267) ||
                (i >= 0x3B0 && i <= 0x3B4) || (i >= 0x3B8 && i <= 0x3C1) ||
                (i >= 0x3C4 && i <= 0x3C8) || i == 0x460 ||
                (copiedConstant && i >= 0x464 && i <= 0x474);
            if (!written && before[i] != after[i]) return false;
        }
        return true;
    }

    static byte[] MixedConstant()
    {
        // Deliberately not four floats: exercise every bit of the MOVUPS copy.
        return new byte[] { 0x80, 0x00, 0xFF, 0x7F, 0x01, 0xFE, 0x55, 0xAA,
                            0x13, 0x37, 0xC0, 0xDE, 0xF0, 0x0D, 0x82, 0x61 };
    }

    static ActorArrayConstructionResult MakePrior(bool mixed)
    {
        ActorConstructorPrefixState state = mixed
            ? new ActorConstructorPrefixState(Mixed(ActorConstructorPrefixState.CapturedExtent))
            : new ActorConstructorPrefixState();
        Actor198ArrayStorage array = new Actor198ArrayStorage(0x100000UL, Mixed(0x208));
        return ActorConstructorArray.ApplyWithArchivedFileInitialCheckTarget(state, array);
    }

    static bool PriorReportUnchanged(ActorArrayConstructionResult prior)
    {
        return prior.Status == "ConditionalPrefix" && prior.Reason == null &&
               prior.ResumeInstructionRva == 0x25C55 && prior.ActorStoreInstructions == 130 &&
               prior.ArrayStoreInstructions == 57 && prior.BulkZeroBytes == 520 &&
               prior.ConstructedElements == 4 && prior.PreElementCalls == 4 &&
               prior.ArrayPublished && prior.BaseReturned && prior.KnownInitialAllocationUsed &&
               prior.ArchivedFileInitialCheckUsed && prior.RequestedAllocationBytes == 520UL &&
               !prior.IsCompleteActorConstructor;
    }

    static void IndependentFreshAssignment()
    {
        byte?[] initial = Mixed(ActorConstructorPrefixState.CapturedExtent);
        SeedWord(initial, 0x260, 8, 0);
        SeedWord(initial, 0x268, 8, 15);
        initial[0x250] = 0xDA;
        ActorConstructorPrefixState state = new ActorConstructorPrefixState(initial);
        byte?[] expected = (byte?[])initial.Clone();
        expected[0x250] = 0;
        ActorEmptyAssignmentResult result = ActorEmptyAssignment.ApplyFreshTarget(state);
        Need(result.Status == "KnownReturn" && result.Reason == null && result.Returned &&
             result.StoreInstructions == 2, "fresh empty assignment must execute two stores");
        Need(Object.ReferenceEquals(result.State, state) && state.ReadByte(0x250) == 0 &&
             initial[0x250] == 0xDA, "empty assignment changes the caller state, not its input copy");
        Need(Same(expected, state.CopyObservation()), "fresh assignment preserves the full tail and all unrelated bytes");
        Need(state.ReadLittleEndian(0x260, 8) == 0UL && state.ReadLittleEndian(0x268, 8) == 15UL,
             "fresh inline length and capacity remain zero and fifteen");
        Need(!result.SourcePayloadRead && result.CalleeCalls == 0 && ActorEmptyAssignment.Count == 0UL &&
             ActorEmptyAssignment.SourcePreferredIdentity == 0x180307E61UL,
             "fixed empty assignment does not read source payload or call another helper");
        ActorEmptyAssignmentResult second = ActorEmptyAssignment.ApplyFreshTarget(state);
        Need(second.Returned && second.StoreInstructions == 2 && Same(expected, state.CopyObservation()),
             "repeat assignment still executes both stores with stable bytes");
    }

    static void FreshGuards()
    {
        ActorEmptyAssignmentResult absent = ActorEmptyAssignment.ApplyFreshTarget(null);
        Need(absent.Status == "Unsupported" && absent.Reason == "UnknownActorFieldStorage" &&
             absent.State == null && !absent.Returned && absent.StoreInstructions == 0,
             "missing state is an own guard without a native return claim");
        ActorConstructorPrefixState unknown = new ActorConstructorPrefixState();
        byte?[] unknownBefore = unknown.CopyObservation();
        ActorEmptyAssignmentResult unproven = ActorEmptyAssignment.ApplyFreshTarget(unknown);
        Need(unproven.Reason == "UnprovenFreshInlineTarget" && unproven.StoreInstructions == 0 &&
             Same(unknownBefore, unknown.CopyObservation()), "unknown metadata cannot establish a fresh target");

        bool partialsPreserved = true;
        for (int i = 0; i < 2; i++)
        {
            byte?[] seed = new byte?[ActorConstructorPrefixState.CapturedExtent];
            if (i == 0) SeedWord(seed, 0x260, 8, 0);
            else SeedWord(seed, 0x268, 8, 15);
            seed[0x250] = 0xA5;
            ActorConstructorPrefixState state = new ActorConstructorPrefixState(seed);
            ActorEmptyAssignmentResult result = ActorEmptyAssignment.ApplyFreshTarget(state);
            partialsPreserved &= result.Status == "Unsupported" && result.Reason == "UnprovenFreshInlineTarget" &&
                result.StoreInstructions == 0 && Same(seed, state.CopyObservation());
        }
        Need(partialsPreserved, "either unknown length or unknown capacity must preserve every byte");

        ulong[] lengths = new ulong[] { 1, 0, 0 };
        ulong[] capacities = new ulong[] { 15, 14, 16 };
        bool nonfreshPreserved = true;
        for (int i = 0; i < lengths.Length; i++)
        {
            byte?[] seed = Mixed(ActorConstructorPrefixState.CapturedExtent);
            SeedWord(seed, 0x260, 8, lengths[i]);
            SeedWord(seed, 0x268, 8, capacities[i]);
            ActorConstructorPrefixState state = new ActorConstructorPrefixState(seed);
            ActorEmptyAssignmentResult result = ActorEmptyAssignment.ApplyFreshTarget(state);
            nonfreshPreserved &= result.Status == "Unsupported" && result.Reason == "UnprovenFreshInlineTarget" &&
                result.StoreInstructions == 0 && Same(seed, state.CopyObservation());
        }
        Need(nonfreshPreserved, "nonfresh length or capacity takes no invented general string branch");
    }

    static void IncompletePrior()
    {
        ActorContinuationResult absent = ActorConstructorContinuation.ContinueWithObservation(null, MixedConstant(), 0x200000UL);
        Need(absent.Status == "Unsupported" && absent.Reason == "MissingPriorActorState" && absent.State == null &&
             absent.ResumeInstructionRva == 0x259A0 && absent.TotalActorStoreInstructions == 0 && !absent.MayHaveEffects,
             "missing prior cannot begin continuation");
        ActorArrayConstructionResult noState = ActorConstructorArray.ApplyUntilUnresolvedCheck(null, null);
        ActorContinuationResult noStateResult = ActorConstructorContinuation.ContinueUsingArchivedConstant(noState, 0x200000UL);
        Need(noStateResult.Reason == "MissingPriorActorState" && noStateResult.AddedActorStoreInstructions == 0 &&
             Object.ReferenceEquals(noStateResult.Prior, noState), "prior with missing state retains its identity and stop");

        ActorConstructorPrefixState state = new ActorConstructorPrefixState(Mixed(ActorConstructorPrefixState.CapturedExtent));
        Actor198ArrayStorage array = new Actor198ArrayStorage(0x100000UL);
        ActorArrayConstructionResult prior = ActorConstructorArray.ApplyUntilUnresolvedCheck(state, array);
        byte?[] stateBefore = state.CopyObservation();
        byte?[] arrayBefore = array.CopyObservation();
        ActorContinuationResult result = ActorConstructorContinuation.ContinueWithObservation(prior, MixedConstant(), 0x200000UL);
        Need(result.Status == "Unsupported" && result.Reason == "PriorConstructorStageIncomplete" &&
             result.ResumeInstructionRva == 0x2DCC75 && result.AddedActorStoreInstructions == 0 &&
             result.TotalActorStoreInstructions == 20, "default loaded-guard stop cannot be promoted by supplying later observations");
        Need(Same(stateBefore, state.CopyObservation()) && Same(arrayBefore, array.CopyObservation()),
             "blocked continuation preserves both actor and array");
        Need(!result.EmptyAssignmentReturned && result.ConstantBytesWritten == 0 && result.ConstantProvenance == "NotUsed" &&
             result.RequestedAllocationBytes == null && !result.KnownInitialAllocationAccepted &&
             !result.ArchivedFileInitialCheckUsed && result.MayHaveEffects,
             "new operations remain unexecuted while existing prefix effects remain visible");
        Need(prior.Reason == "UnresolvedLoadedPreElementCheck" && prior.ActorStoreInstructions == 20 &&
             prior.PreElementCalls == 0 && !prior.ArrayPublished && !prior.BaseReturned &&
             !result.SubobjectConstructorInvoked && !result.IsCompleteActorConstructor,
             "default profile never claims loaded check, base return, or complete Actor");
    }

    static void CompleteSuppliedContinuation()
    {
        ActorArrayConstructionResult prior = MakePrior(true);
        byte?[] before = prior.State.CopyObservation();
        byte?[] arrayBefore = prior.Array.CopyObservation();
        byte[] constant = MixedConstant();
        byte[] expectedConstant = (byte[])constant.Clone();
        ActorContinuationResult result = ActorConstructorContinuation.ContinueWithObservation(prior, constant, 0x200000UL);
        Need(result.Status == "ConditionalPrefix" && result.Reason == null && result.ResumeInstructionRva == 0x25CC9 &&
             result.AddedActorStoreInstructions == 12 && result.TotalActorStoreInstructions == 142,
             "supplied normal observations stop at the next constructor call with twelve added stores");
        Need(Object.ReferenceEquals(result.Prior, prior) && Object.ReferenceEquals(result.State, prior.State) &&
             PriorReportUnchanged(prior), "continuation shares the state but leaves prior report counts and flags unchanged");
        Need(result.EmptyAssignmentReturned && result.ConstantBytesWritten == 16 &&
             result.ConstantProvenance == "SuppliedObservation" && result.ArchivedFileInitialCheckUsed,
             "explicit file-initial-check dependency and supplied constant provenance remain visible");
        Need(result.RequestedAllocationBytes == 0x358UL && result.KnownInitialAllocationAccepted &&
             result.PendingConstructorThisToken == 0x200000UL && !result.SubobjectConstructorInvoked &&
             !result.IsCompleteActorConstructor && result.MayHaveEffects,
             "allocation observation supplies only pending this identity, not construction success");
        Need(HasBytes(prior.State, 0x464, expectedConstant) && prior.State.ReadByte(0x474) == 0,
             "MOVUPS preserves all supplied bit patterns and the following flag is cleared");
        constant[0] ^= 0xFF; constant[7] ^= 0xFF; constant[15] ^= 0xFF;
        Need(HasBytes(prior.State, 0x464, expectedConstant), "later input mutation cannot alter the stored constant");
        Need(ScalarsHaveExpectedBits(prior.State) && prior.State.ReadByte(0x250) == 0 &&
             prior.State.ReadLittleEndian(0x260, 8) == 0UL,
             "eight scalars have exact raw values and empty target metadata remains valid");
        byte?[] after = prior.State.CopyObservation();
        Need(OutsideContinuationPreserved(before, after, true) &&
             SameSpan(before, after, 0x3C2, 2) && SameSpan(before, after, 0x461, 3),
             "word and byte writes preserve every adjacent byte including the narrow-field gaps");
        Need(SameSpan(before, after, 0x1E0, 8) && Same(arrayBefore, prior.Array.CopyObservation()) &&
             prior.State.ReadLittleEndian(0x1A8, 8) == prior.Array.ElementsToken,
             "BDBE0 return slot is not published and the earlier allocation/published token stay unchanged");
    }

    static void MissingConstant()
    {
        ActorArrayConstructionResult prior = MakePrior(false);
        byte?[] before = prior.State.CopyObservation();
        byte?[] arrayBefore = prior.Array.CopyObservation();
        ActorContinuationResult result = ActorConstructorContinuation.ContinueWithObservation(prior, null, 0x200000UL);
        Need(result.Status == "Unsupported" && result.Reason == "MissingConstantObservation" &&
             result.ResumeInstructionRva == 0x25CA2 && result.AddedActorStoreInstructions == 10 &&
             result.TotalActorStoreInstructions == 140, "missing constant stops after empty assignment and eight scalars");
        Need(result.EmptyAssignmentReturned && ScalarsHaveExpectedBits(prior.State) && result.MayHaveEffects,
             "missing constant does not roll back the ten completed stores");
        byte?[] after = prior.State.CopyObservation();
        Need(SameSpan(before, after, 0x464, 17) && prior.State.ReadByte(0x474) == null &&
             OutsideContinuationPreserved(before, after, false), "missing vector and following flag remain unknown, with untouched neighbors");
        Need(result.ConstantBytesWritten == 0 && result.ConstantProvenance == "NotUsed" &&
             result.RequestedAllocationBytes == null && !result.KnownInitialAllocationAccepted &&
             result.PendingConstructorThisToken == null, "allocation dependency is not reached before the vector is known");
        Need(PriorReportUnchanged(prior) && Same(arrayBefore, prior.Array.CopyObservation()),
             "partial continuation leaves prior counters and existing array unchanged");
        Need(!result.SubobjectConstructorInvoked && !result.IsCompleteActorConstructor &&
             SameSpan(before, after, 0x1E0, 8), "partial result cannot fabricate a constructed or published subobject");
    }

    static void AllocationObservationGuards()
    {
        ActorArrayConstructionResult prior = MakePrior(true);
        byte?[] before = prior.State.CopyObservation();
        byte[] constant = MixedConstant();
        ActorContinuationResult missing = ActorConstructorContinuation.ContinueWithObservation(prior, constant, null);
        Need(missing.Status == "Unsupported" && missing.Reason == "MissingKnownInitialAllocation" &&
             missing.ResumeInstructionRva == 0x25CBC && missing.AddedActorStoreInstructions == 12 &&
             missing.TotalActorStoreInstructions == 142 && missing.RequestedAllocationBytes == 856UL,
             "missing allocation retains all twelve writes at the allocation call site");
        Need(HasBytes(prior.State, 0x464, constant) && prior.State.ReadByte(0x474) == 0 &&
             ScalarsHaveExpectedBits(prior.State) && OutsideContinuationPreserved(before, prior.State.CopyObservation(), true),
             "constant and narrow scalar prefix remain committed before allocation is known");
        Need(!missing.KnownInitialAllocationAccepted && missing.PendingConstructorThisToken == null &&
             !missing.SubobjectConstructorInvoked && !missing.IsCompleteActorConstructor && PriorReportUnchanged(prior),
             "missing allocation is neither a native null return nor a completed subobject");

        ulong[] invalid = new ulong[] { 0UL, ulong.MaxValue - 854UL };
        bool invalidResults = true;
        bool invalidPrefixes = true;
        for (int i = 0; i < invalid.Length; i++)
        {
            ActorArrayConstructionResult item = MakePrior(true);
            byte?[] itemBefore = item.State.CopyObservation();
            byte?[] arrayBefore = item.Array.CopyObservation();
            ActorContinuationResult result = ActorConstructorContinuation.ContinueWithObservation(item, constant, invalid[i]);
            invalidResults &= result.Status == "Unsupported" && result.Reason == "InvalidKnownInitialAllocationIdentity" &&
                result.ResumeInstructionRva == 0x25CBC && result.AddedActorStoreInstructions == 12 &&
                result.TotalActorStoreInstructions == 142 && result.RequestedAllocationBytes == 856UL &&
                !result.KnownInitialAllocationAccepted && result.PendingConstructorThisToken == null &&
                !result.SubobjectConstructorInvoked && !result.IsCompleteActorConstructor;
            invalidPrefixes &= HasBytes(item.State, 0x464, constant) && ScalarsHaveExpectedBits(item.State) &&
                item.State.ReadByte(0x474) == 0 && OutsideContinuationPreserved(itemBefore, item.State.CopyObservation(), true) &&
                Same(arrayBefore, item.Array.CopyObservation()) && PriorReportUnchanged(item);
        }
        Need(invalidResults, "zero and overflowing token spans are rejected as own observation guards");
        Need(invalidPrefixes, "invalid token rejection preserves the completed prefix and the earlier array");
        ActorArrayConstructionResult edgePrior = MakePrior(false);
        ActorContinuationResult edge = ActorConstructorContinuation.ContinueWithObservation(edgePrior, constant, ulong.MaxValue - 855UL);
        Need(edge.Status == "ConditionalPrefix" && edge.KnownInitialAllocationAccepted &&
             edge.PendingConstructorThisToken == ulong.MaxValue - 855UL && edge.ResumeInstructionRva == 0x25CC9,
             "last non-overflowing 856-byte observation span is accepted without dereferencing the token");
    }

    static void ArchivedConstantCopies()
    {
        byte[] golden = new byte[] { 0, 0, 128, 63, 0, 0, 128, 63,
                                     0, 0, 128, 63, 0, 0, 128, 63 };
        byte[] first = ActorConstructorContinuation.CopyArchivedConstant();
        byte[] second = ActorConstructorContinuation.CopyArchivedConstant();
        Need(!Object.ReferenceEquals(first, second) && Same(first, golden) && Same(second, golden),
             "archived observation returns independent exact sixteen-byte copies");
        first[0] = 0xFF; first[15] = 0xFF;
        byte[] third = ActorConstructorContinuation.CopyArchivedConstant();
        Need(Same(second, golden) && Same(third, golden) && !Object.ReferenceEquals(second, third),
             "mutating one archived copy cannot change existing or future copies");
        ActorArrayConstructionResult prior = MakePrior(true);
        byte?[] before = prior.State.CopyObservation();
        ActorContinuationResult result = ActorConstructorContinuation.ContinueUsingArchivedConstant(prior, 0x300000UL);
        Need(result.Status == "ConditionalPrefix" && result.Reason == null && result.ConstantProvenance == "ArchivedFileImage" &&
             result.ConstantBytesWritten == 16 && result.ArchivedFileInitialCheckUsed,
             "explicit archived path identifies both conditional data sources");
        Need(HasBytes(prior.State, 0x464, golden) && prior.State.ReadLittleEndian(0x464, 8) == 0x3F8000003F800000UL &&
             prior.State.ReadLittleEndian(0x46C, 8) == 0x3F8000003F800000UL,
             "archived vector stores four exact raw one-point-zero patterns");
        second[4] = 0xEC; third[12] = 0xAC;
        Need(HasBytes(prior.State, 0x464, golden) && OutsideContinuationPreserved(before, prior.State.CopyObservation(), true),
             "returned copy mutations do not alias actor storage and narrow-write neighbors remain intact");
        Need(result.AddedActorStoreInstructions == 12 && result.TotalActorStoreInstructions == 142 &&
             result.ResumeInstructionRva == 0x25CC9 && PriorReportUnchanged(prior) &&
             !result.SubobjectConstructorInvoked && !result.IsCompleteActorConstructor,
             "archived-file profile stops before BDBE0 and does not certify runtime initialization");
    }

    static void InvalidConstantLengths()
    {
        int[] lengths = new int[] { 0, 15, 17 };
        bool statuses = true;
        bool bytesPreserved = true;
        bool metadataPreserved = true;
        bool stagesPreserved = true;
        for (int i = 0; i < lengths.Length; i++)
        {
            ActorArrayConstructionResult prior = MakePrior(true);
            byte?[] before = prior.State.CopyObservation();
            byte?[] arrayBefore = prior.Array.CopyObservation();
            ActorContinuationResult result = ActorConstructorContinuation.ContinueWithObservation(prior, new byte[lengths[i]], 0x200000UL);
            statuses &= result.Status == "Unsupported" && result.Reason == "InvalidConstantObservation" &&
                result.ResumeInstructionRva == 0x25C5F && result.AddedActorStoreInstructions == 0 &&
                result.TotalActorStoreInstructions == 130;
            bytesPreserved &= Same(before, prior.State.CopyObservation()) && Same(arrayBefore, prior.Array.CopyObservation());
            metadataPreserved &= !result.EmptyAssignmentReturned && result.ConstantBytesWritten == 0 &&
                result.ConstantProvenance == "NotUsed" && result.RequestedAllocationBytes == null &&
                !result.KnownInitialAllocationAccepted && result.PendingConstructorThisToken == null;
            stagesPreserved &= PriorReportUnchanged(prior) && result.MayHaveEffects &&
                !result.SubobjectConstructorInvoked && !result.IsCompleteActorConstructor;
        }
        Need(statuses, "wrong vector lengths are rejected at the own pre-write guard");
        Need(bytesPreserved, "wrong vector lengths cannot clear even the inline target or mutate the earlier array");
        Need(metadataPreserved, "invalid observation does not report any new dependency as executed");
        Need(stagesPreserved, "invalid observation preserves the conditional prior and its existing effects");
    }

    public static int Main()
    {
        try
        {
            Run("IndependentFreshAssignment", IndependentFreshAssignment);
            Run("FreshGuards", FreshGuards);
            Run("IncompletePrior", IncompletePrior);
            Run("CompleteSuppliedContinuation", CompleteSuppliedContinuation);
            Run("MissingConstant", MissingConstant);
            Run("AllocationObservationGuards", AllocationObservationGuards);
            Run("ArchivedConstantCopies", ArchivedConstantCopies);
            Run("InvalidConstantLengths", InvalidConstantLengths);
            Console.WriteLine("PASS: " + assertions + " assertions, " + groups + " scenario groups");
            return 0;
        }
        catch (Exception error)
        {
            Console.WriteLine("FAIL after " + assertions + " assertions and " + groups + " scenario groups: " + error.Message);
            return 1;
        }
    }
}
