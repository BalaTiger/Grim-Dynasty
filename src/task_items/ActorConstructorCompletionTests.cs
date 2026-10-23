using System;
using GrimDynasty.TaskItems;

// Own bounded fixtures only. The archived guard and warm TLS observations are
// explicit conditional inputs, not recovered loaded-runtime initialization.
// No original binary, native pointer, file, or old test suite is used here.
public static class ActorConstructorCompletionTests
{
    const ulong ActorToken = 0x500000UL;
    const ulong SubToken = 0x200000UL;
    const ulong SentinelToken = 0x300000UL;
    static int assertions, groups;

    sealed class Fixture
    {
        public ActorContinuationResult Prior;
        public ActorConstructorStorage Sub, Sentinel;
        public byte?[] ActorBefore, SubBefore, SentinelBefore, ArrayBefore;
    }

    static void Need(bool condition, string message)
    {
        assertions++;
        if (!condition) throw new Exception(message);
    }
    static void Run(string name, Action body)
    {
        int before = assertions;
        body(); groups++;
        Console.WriteLine("PASS " + name + ": " + (assertions - before) + " assertions");
    }
    static byte?[] Payload(int length, int seed)
    {
        byte?[] bytes = new byte?[length];
        for (int i = 0; i < length; i++) bytes[i] = (byte)((seed + i * 29) & 255);
        return bytes;
    }
    static byte[] Vector(int seed)
    {
        byte[] bytes = new byte[16];
        for (int i = 0; i < bytes.Length; i++) bytes[i] = (byte)((seed + i * 47) & 255);
        return bytes;
    }
    static bool Same(byte?[] a, byte?[] b)
    {
        if (a == null || b == null || a.Length != b.Length) return false;
        for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
        return true;
    }
    static bool SpanSame(byte?[] a, byte?[] b, int at, int count)
    {
        for (int i = at; i < at + count; i++) if (a[i] != b[i]) return false;
        return true;
    }
    static bool BytesAt(ActorConstructorStorage storage, int at, byte?[] expected)
    {
        for (int i = 0; i < expected.Length; i++) if (storage.ReadByte(at + i) != expected[i]) return false;
        return true;
    }
    static bool BytesAt(ActorConstructorStorage storage, int at, byte[] expected)
    {
        for (int i = 0; i < expected.Length; i++) if (storage.ReadByte(at + i) != expected[i]) return false;
        return true;
    }
    static bool PrefixCopied(ActorConstructorStorage storage, byte?[] before, int at, byte?[] source, int copied)
    {
        for (int i = 0; i < 48; i++)
        {
            byte? expected = i < copied ? source[i] : before[at + i];
            if (storage.ReadByte(at + i) != expected) return false;
        }
        return true;
    }
    static ActorMatrixWarmObservation Warm(int seed)
    {
        return new ActorMatrixWarmObservation(true, 4, 3, Payload(48, seed));
    }
    static ActorGlobalObservation Global()
    {
        return new ActorGlobalObservation(0x600000UL, 7UL, 7UL);
    }
    static ActorCompletionObservations Archived()
    {
        return ActorCompletionObservations.WithArchivedConstants(Warm(11), Warm(91), Global());
    }
    static Fixture MakeFixture()
    {
        Fixture f = new Fixture();
        ActorConstructorPrefixState actor = new ActorConstructorPrefixState(Payload(0x530, 37));
        Actor198ArrayStorage array = new Actor198ArrayStorage(0x100000UL, Payload(0x208, 53));
        ActorArrayConstructionResult prior = ActorConstructorArray.ApplyWithArchivedFileInitialCheckTarget(actor, array);
        f.Prior = ActorConstructorContinuation.ContinueUsingArchivedConstant(prior, SubToken);
        f.Sub = new ActorConstructorStorage(SubToken, Payload(0x358, 73));
        f.Sentinel = new ActorConstructorStorage(SentinelToken, Payload(0x38, 109));
        f.ActorBefore = actor.CopyObservation(); f.SubBefore = f.Sub.CopyObservation();
        f.SentinelBefore = f.Sentinel.CopyObservation(); f.ArrayBefore = array.CopyObservation();
        return f;
    }
    static ActorCompletionResult Complete(Fixture f, ActorCompletionObservations data)
    {
        return ActorConstructorCompletion.Continue(f.Prior, ActorToken, f.Sub, f.Sentinel, data);
    }
    static bool PriorUnchanged(Fixture f)
    {
        return f.Prior.Status == "ConditionalPrefix" && f.Prior.Reason == null &&
            f.Prior.ResumeInstructionRva == 0x25CC9 && f.Prior.AddedActorStoreInstructions == 12 &&
            f.Prior.TotalActorStoreInstructions == 142 && f.Prior.PendingConstructorThisToken == SubToken &&
            !f.Prior.SubobjectConstructorInvoked && !f.Prior.IsCompleteActorConstructor &&
            f.Prior.Prior.ActorStoreInstructions == 130 && f.Prior.Prior.ResumeInstructionRva == 0x25C55 &&
            Same(f.ArrayBefore, f.Prior.Prior.Array.CopyObservation());
    }
    static bool CompleteCounts(ActorCompletionResult r)
    {
        return r.Status == "ConditionalNormalReturn" && r.Reason == null && r.ResumeInstructionRva == 0x25D8C &&
            r.SubobjectConstructorStores == 108 && r.SentinelStores == 2 && r.BackReferenceStores == 2 &&
            r.ActorStoresAdded == 16 && r.TotalActorStores == 158 && r.NewPreElementCalls == 1 &&
            r.MatrixHelperCalls == 2 && r.MatrixWarmReturns == 2 && r.MatrixChunksCopied == 6;
    }

    static void CompleteArchivedIntegration()
    {
        Fixture f = MakeFixture(); ActorCompletionResult r = Complete(f, Archived());
        Need(CompleteCounts(r), "complete conditional chain has independent native-store and call counts");
        Need(Object.ReferenceEquals(r.Prior, f.Prior) && Object.ReferenceEquals(r.State, f.Prior.State) &&
            Object.ReferenceEquals(r.Subobject, f.Sub) && Object.ReferenceEquals(r.Sentinel, f.Sentinel) && PriorUnchanged(f),
            "integration shares own storage without rewriting the earlier reports or array");
        Need(r.ElementReturned && r.EmptyVectorHelperReturned && r.SubobjectReturned && r.ActorObjectPublished &&
            r.NormalReturnReached && r.ReturnedSubobjectToken == SubToken && r.ReturnedActorToken == ActorToken,
            "proven subobject return and final Actor return have distinct correct identities");
        Need(f.Prior.State.ReadLittleEndian(0x1E0, 8) == SubToken && f.Sub.ReadLittleEndian(0x118, 8) == ActorToken &&
            f.Sub.ReadLittleEndian(0x110, 4) == 0UL && r.BackReferenceStores == 2,
            "caller publishes constructor return then establishes both back references");
        Need(r.RequestedSentinelBytes == 56UL && r.KnownInitialSentinelAllocationUsed &&
            f.Sentinel.ReadLittleEndian(0, 8) == SentinelToken && f.Sentinel.ReadLittleEndian(8, 8) == SentinelToken &&
            f.Sub.ReadLittleEndian(0x148, 8) == SentinelToken,
            "element publishes the allocated sentinel with two real self links");
        Need(BytesAt(f.Sub, 0xE0, Payload(48, 11)) && BytesAt(f.Sub, 0x1E4, Payload(48, 91)),
            "two matrix calls use independent source observations");
        Need(r.GlobalSlotRead && r.Global678Read && r.Global668Read && f.Prior.State.ReadByte(0x4E1) == 1 &&
            f.Prior.State.ReadLittleEndian(0x49C, 8) == 0x3F8000003F800000UL &&
            f.Prior.State.ReadLittleEndian(0x4A4, 4) == 0x3F800000UL,
            "tail uses full observations and exact archived scalar bits");
        Need(r.ConstantProvenance == "ArchivedFileImage" && r.ArchivedFileInitialCheckUsed &&
            !r.IsCompleteNativeConstructor && r.MayHaveEffects,
            "normal return remains conditional on archived guard and explicit runtime observations");
    }

    static void PriorAndIdentityGuards()
    {
        Fixture f = MakeFixture();
        ActorCompletionResult absent = ActorConstructorCompletion.Continue(null, ActorToken, f.Sub, f.Sentinel, Archived());
        Need(absent.Reason == "MissingPriorActorState" && absent.ResumeInstructionRva == 0x25CC9 &&
            absent.SubobjectConstructorStores == 0 && absent.ActorStoresAdded == 0 && !absent.MayHaveEffects,
            "missing prior cannot start the constructor");

        ActorConstructorPrefixState blockedState = new ActorConstructorPrefixState(Payload(0x530, 17));
        Actor198ArrayStorage blockedArray = new Actor198ArrayStorage(0x100000UL);
        ActorArrayConstructionResult defaultPrefix = ActorConstructorArray.ApplyUntilUnresolvedCheck(blockedState, blockedArray);
        ActorContinuationResult blockedPrior = ActorConstructorContinuation.ContinueUsingArchivedConstant(defaultPrefix, SubToken);
        byte?[] blockedBefore = blockedState.CopyObservation();
        ActorCompletionResult blocked = ActorConstructorCompletion.Continue(blockedPrior, ActorToken, f.Sub, f.Sentinel, Archived());
        Need(blocked.Reason == "PriorConstructorStageIncomplete" && blocked.ResumeInstructionRva == 0x2DCC75 &&
            blocked.SubobjectConstructorStores == 0 && !blocked.NormalReturnReached && !blocked.ArchivedFileInitialCheckUsed &&
            Same(blockedBefore, blockedState.CopyObservation()), "default loaded-guard stop cannot enter completion");

        ulong[] badActor = new ulong[] { 0UL, ulong.MaxValue - 0x52EUL };
        bool identitiesRejected = true;
        for (int i = 0; i < badActor.Length; i++)
        {
            ActorCompletionResult r = ActorConstructorCompletion.Continue(f.Prior, badActor[i], f.Sub, f.Sentinel, Archived());
            identitiesRejected &= r.Reason == "InvalidActorIdentity" && r.ResumeInstructionRva == 0x25CC9 &&
                r.SubobjectConstructorStores == 0 && r.ActorStoresAdded == 0;
        }
        Need(identitiesRejected, "zero and overflowing Actor spans are own pre-write guards");
        ActorCompletionResult noStorage = ActorConstructorCompletion.Continue(f.Prior, ActorToken, null, f.Sentinel, Archived());
        Need(noStorage.Reason == "MissingSubobjectStorage" && noStorage.SubobjectConstructorStores == 0 &&
            !noStorage.SubobjectReturned, "allocation observation alone does not supply constructor storage");
        ActorConstructorStorage[] wrong = new ActorConstructorStorage[] {
            new ActorConstructorStorage(SubToken + 0x10000UL, 0x358), new ActorConstructorStorage(SubToken, 0x38) };
        bool wrongStorageRejected = true;
        for (int i = 0; i < wrong.Length; i++)
        {
            byte?[] before = wrong[i].CopyObservation();
            ActorCompletionResult r = ActorConstructorCompletion.Continue(f.Prior, ActorToken, wrong[i], f.Sentinel, Archived());
            wrongStorageRejected &= r.Reason == "SubobjectAllocationIdentityMismatch" && r.SubobjectConstructorStores == 0 &&
                Same(before, wrong[i].CopyObservation());
        }
        Need(wrongStorageRejected, "subobject must have the exact pending identity and 856-byte extent");
        Need(Same(f.ActorBefore, f.Prior.State.CopyObservation()) && Same(f.SubBefore, f.Sub.CopyObservation()) &&
            Same(f.SentinelBefore, f.Sentinel.CopyObservation()), "all early guards preserve all supplied storages");
        Need(PriorUnchanged(f) && absent.Status == "Unsupported" && blocked.Status == "Unsupported" &&
            !blocked.ActorObjectPublished, "guard failures preserve existing prefix effects without claiming publication");
    }

    static void SentinelBoundary()
    {
        Fixture missing = MakeFixture();
        ActorCompletionResult a = ActorConstructorCompletion.Continue(missing.Prior, ActorToken, missing.Sub, null, Archived());
        Need(a.Reason == "MissingKnownInitialSentinelAllocation" && a.ResumeInstructionRva == 0x1F185 &&
            a.SubobjectConstructorStores == 25 && a.SentinelStores == 0 && a.RequestedSentinelBytes == 56UL &&
            !a.KnownInitialSentinelAllocationUsed && !a.ElementReturned,
            "missing sentinel stops after the actual pre-allocation element prefix");
        Need(missing.Sub.ReadLittleEndian(0x148, 8) == 0UL && missing.Sub.ReadLittleEndian(0x130, 4) == 0x3F800000UL &&
            Same(missing.ActorBefore, missing.Prior.State.CopyObservation()) && !a.SubobjectReturned && !a.ActorObjectPublished,
            "sentinel failure preserves early scalar writes but cannot publish the Actor subobject");
        Fixture invalid = MakeFixture(); ActorConstructorStorage wrong = new ActorConstructorStorage(0x400000UL, 0x358);
        byte?[] wrongBefore = wrong.CopyObservation();
        ActorCompletionResult b = ActorConstructorCompletion.Continue(invalid.Prior, ActorToken, invalid.Sub, wrong, Archived());
        Need(b.Reason == "InvalidSentinelStorage" && b.SubobjectConstructorStores == 25 && b.SentinelStores == 0 &&
            Same(wrongBefore, wrong.CopyObservation()), "wrong sentinel extent preserves the allocated observation");
        Fixture unknown = MakeFixture(); unknown.Sentinel = new ActorConstructorStorage(SentinelToken, 0x38);
        ActorCompletionResult c = Complete(unknown, Archived());
        Need(CompleteCounts(c) && unknown.Sentinel.ReadLittleEndian(0, 8) == SentinelToken &&
            unknown.Sentinel.ReadLittleEndian(8, 8) == SentinelToken && unknown.Sub.ReadLittleEndian(0x148, 8) == SentinelToken,
            "unknown sentinel bytes become exactly two self links and a published node identity");
        bool tailUnknown = true;
        for (int i = 16; i < 56; i++) tailUnknown &= unknown.Sentinel.ReadByte(i) == null;
        Need(tailUnknown, "the other forty sentinel bytes remain unknown rather than fabricated zero");
        Need(PriorUnchanged(missing) && PriorUnchanged(invalid) && PriorUnchanged(unknown) &&
            a.ActorStoresAdded == 0 && b.ActorStoresAdded == 0,
            "sentinel outcomes do not rewrite earlier array or prior report state");
    }

    static void ConstantDependencyStops()
    {
        string[] reasons = new string[] { "MissingSubobjectVector", "MissingSubobjectUnit", "MissingSubobjectScale", "MissingActorUnit" };
        uint[] sites = new uint[] { 0xBDD0B, 0xBDDDB, 0xBDE47, 0x25D15 };
        int[] subCounts = new int[] { 47, 77, 84, 108 };
        int[] untouchedOffsets = new int[] { 0x2DC, 0x19C, 0x22C };
        bool exactStops = true, noInventedBytes = true, prefixKept = true, downstreamBlocked = true;
        for (int i = 0; i < reasons.Length; i++)
        {
            Fixture f = MakeFixture();
            ActorCompletionObservations data = new ActorCompletionObservations(i == 0 ? null : Vector(19),
                i == 1 ? (uint?)null : 0x3F800000U, Warm(11), i == 2 ? null : Vector(77), Warm(91),
                i == 3 ? (uint?)null : 0x3F800000U, Global());
            ActorCompletionResult r = Complete(f, data);
            exactStops &= r.Status == "Unsupported" && r.Reason == reasons[i] && r.ResumeInstructionRva == sites[i] &&
                r.SubobjectConstructorStores == subCounts[i] && r.ActorStoresAdded == (i == 3 ? 8 : 0) &&
                r.TotalActorStores == (i == 3 ? 150 : 142) && r.SentinelStores == 2;
            if (i < 3) noInventedBytes &= SpanSame(f.SubBefore, f.Sub.CopyObservation(), untouchedOffsets[i], i == 1 ? 12 : 16);
            else noInventedBytes &= SpanSame(f.ActorBefore, f.Prior.State.CopyObservation(), 0x49C, 12);
            prefixKept &= f.Sub.ReadLittleEndian(0x2D4, 8) == 1UL && f.Sub.ReadLittleEndian(0x148, 8) == SentinelToken &&
                PriorUnchanged(f) && (i < 3 ? Same(f.ActorBefore, f.Prior.State.CopyObservation()) :
                f.Prior.State.ReadLittleEndian(0x1E0, 8) == SubToken && f.Sub.ReadLittleEndian(0x118, 8) == ActorToken);
            downstreamBlocked &= !r.NormalReturnReached && !r.GlobalSlotRead && !r.Global678Read && !r.Global668Read &&
                r.ReturnedActorToken == null && r.SubobjectReturned == (i == 3) && r.BackReferenceStores == (i == 3 ? 2 : 0);
        }
        Need(exactStops, "each missing constant stops at its own actual load with the exact completed prefix");
        Need(noInventedBytes, "missing observations cannot overwrite their destination or dependent following scalar");
        Need(prefixKept, "constant stops retain sentinel and already completed caller/back-reference effects");
        Need(downstreamBlocked, "a missing earlier constant cannot be bypassed by later supplied observations");
        Fixture none = MakeFixture(); ActorCompletionResult absentData = Complete(none, null);
        Need(absentData.Reason == "MissingSubobjectVector" && absentData.SubobjectConstructorStores == 47 &&
            absentData.ConstantProvenance == "NotUsed" && absentData.SentinelStores == 2,
            "absent data stops only when the first external constant is actually required");
        Need(Same(none.ActorBefore, none.Prior.State.CopyObservation()) && !absentData.SubobjectReturned &&
            absentData.MayHaveEffects && !absentData.IsCompleteNativeConstructor,
            "missing data preserves partial construction without promoting it to a return");
    }

    static void RawConstantsAndInputCopies()
    {
        Fixture f = MakeFixture(); byte[] vector = Vector(0x80), scale = Vector(0xF1);
        byte[] vectorExpected = (byte[])vector.Clone(), scaleExpected = (byte[])scale.Clone();
        byte?[] first = Payload(48, 21), second = Payload(48, 113);
        byte?[] firstExpected = (byte?[])first.Clone(), secondExpected = (byte?[])second.Clone();
        ActorMatrixWarmObservation firstObservation = new ActorMatrixWarmObservation(true, 0, -1, first);
        ActorMatrixWarmObservation secondObservation = new ActorMatrixWarmObservation(true, 0, -1, second);
        ActorCompletionObservations data = new ActorCompletionObservations(vector, 0xFFC01234U, firstObservation,
            scale, secondObservation, 0x80000000U, Global());
        vector[0] ^= 255; vector[15] ^= 255; scale[7] ^= 255;
        first[0] = null; first[47] = null; second[16] = null;
        ActorCompletionResult r = Complete(f, data);
        Need(CompleteCounts(r) && r.ConstantProvenance == "SuppliedObservations", "raw supplied observations preserve their provenance");
        Need(BytesAt(f.Sub, 0x2DC, vectorExpected) && BytesAt(f.Sub, 0x22C, scaleExpected) &&
            BytesAt(f.Sub, 0x244, scaleExpected), "both sixteen-byte vectors are copied bit-for-bit from owned snapshots");
        Need(f.Sub.ReadLittleEndian(0x19C, 8) == 0xFFC01234FFC01234UL && f.Sub.ReadLittleEndian(0x1A4, 4) == 0x3F800000UL,
            "subobject scalar duplicates raw NaN bits without floating point canonicalization");
        Need(f.Prior.State.ReadLittleEndian(0x49C, 8) == 0x8000000080000000UL &&
            f.Prior.State.ReadLittleEndian(0x4A4, 4) == 0x3F800000UL,
            "Actor scalar duplicates negative-zero bits and keeps the separate fixed third lane");
        Need(BytesAt(f.Sub, 0xE0, firstExpected) && BytesAt(f.Sub, 0x1E4, secondExpected),
            "matrix constructors snapshot their separate forty-eight-byte inputs");
        Fixture archived = MakeFixture(); ActorCompletionObservations archivedData = Archived();
        byte[] unrelatedCopy = ActorConstructorContinuation.CopyArchivedConstant(); unrelatedCopy[0] = 0xEE;
        ActorCompletionResult ar = Complete(archived, archivedData);
        Need(CompleteCounts(ar) && ar.ConstantProvenance == "ArchivedFileImage" &&
            archived.Sub.ReadLittleEndian(0x22C, 8) == 0x3F8000003F800000UL &&
            archived.Sub.ReadLittleEndian(0x2DC, 8) == 0x000000003F800000UL,
            "archived data owns its constants and does not inherit mutations to an unrelated returned copy");
    }

    static void MatrixEpochBoundaries()
    {
        ActorMatrixWarmObservation[] bad = new ActorMatrixWarmObservation[] {
            null, new ActorMatrixWarmObservation(false, 0, 0, Payload(48, 11)),
            new ActorMatrixWarmObservation(true, null, 0, Payload(48, 11)),
            new ActorMatrixWarmObservation(true, 0, null, Payload(48, 11)),
            new ActorMatrixWarmObservation(true, int.MinValue, int.MaxValue, Payload(48, 11)),
            new ActorMatrixWarmObservation(true, -1, 0, Payload(48, 11)) };
        string[] reasons = new string[] { "MissingValidTlsObservation", "MissingValidTlsObservation", "MissingThreadEpoch",
            "MissingMatrixGuardEpoch", "UnresolvedMatrixInitialization", "UnresolvedMatrixInitialization" };
        uint[] sites = new uint[] { 0x273AB4, 0x273AB4, 0x273ACC, 0x273ACF, 0x273AEA, 0x273AEA };
        bool stops = true, prefixes = true;
        for (int i = 0; i < bad.Length; i++)
        {
            Fixture f = MakeFixture(); ActorCompletionResult r = Complete(f,
                ActorCompletionObservations.WithArchivedConstants(bad[i], Warm(91), Global()));
            stops &= r.Reason == reasons[i] && r.ResumeInstructionRva == sites[i] && r.SubobjectConstructorStores == 81 &&
                r.MatrixHelperCalls == 1 && r.MatrixWarmReturns == 0 && r.MatrixChunksCopied == 0;
            prefixes &= PrefixCopied(f.Sub, f.SubBefore, 0xE0, Payload(48, 11), 0) &&
                Same(f.ActorBefore, f.Prior.State.CopyObservation()) && r.SentinelStores == 2 &&
                !r.SubobjectReturned && !r.ActorObjectPublished && PriorUnchanged(f);
        }
        Need(stops, "TLS, epoch, guard, and signed slow-path failures have distinct exact stops");
        Need(prefixes, "matrix entry failures retain earlier construction but do not touch its destination");
        int[] threads = new int[] { int.MaxValue, int.MinValue, 0, int.MaxValue };
        int[] guards = new int[] { int.MinValue, int.MinValue, -1, int.MaxValue };
        bool warmReturns = true, warmCopies = true;
        for (int i = 0; i < threads.Length; i++)
        {
            Fixture f = MakeFixture(); ActorCompletionResult r = Complete(f, ActorCompletionObservations.WithArchivedConstants(
                new ActorMatrixWarmObservation(true, threads[i], guards[i], Payload(48, 11)), Warm(91), Global()));
            warmReturns &= CompleteCounts(r);
            warmCopies &= BytesAt(f.Sub, 0xE0, Payload(48, 11));
        }
        Need(warmReturns, "signed guard <= thread, including both extrema and equality, is the warm branch");
        Need(warmCopies, "all covered signed warm cases use the observed source bytes");
        Fixture later = MakeFixture(); ActorCompletionResult late = Complete(later, ActorCompletionObservations.WithArchivedConstants(
            Warm(11), new ActorMatrixWarmObservation(true, int.MinValue, int.MaxValue, Payload(48, 91)), Global()));
        Need(late.Reason == "UnresolvedMatrixInitialization" && late.ResumeInstructionRva == 0x273AEA &&
            late.SubobjectConstructorStores == 95 && late.MatrixHelperCalls == 2 && late.MatrixWarmReturns == 1 &&
            late.MatrixChunksCopied == 3, "the second call independently evaluates its own signed epochs");
        Need(BytesAt(later.Sub, 0xE0, Payload(48, 11)) && PrefixCopied(later.Sub, later.SubBefore, 0x1E4, Payload(48, 91), 0) &&
            Same(later.ActorBefore, later.Prior.State.CopyObservation()), "second slow path cannot roll back the first matrix or publish the Actor object");
        Fixture source = MakeFixture(); ActorCompletionResult noSource = Complete(source, ActorCompletionObservations.WithArchivedConstants(
            new ActorMatrixWarmObservation(true, 0, 0, null), Warm(91), Global()));
        Need(noSource.Reason == "MissingMatrixSourceChunk" && noSource.ResumeInstructionRva == 0xBDE27 &&
            noSource.MatrixWarmReturns == 1 && noSource.MatrixChunksCopied == 0,
            "a warm helper return alone does not invent readable matrix source bytes");
    }

    static void FirstMatrixChunks()
    {
        uint[] sites = new uint[] { 0xBDE27, 0xBDE31, 0xBDE3C };
        bool stops = true, bytes = true, earlyEffects = true, notPublished = true, priors = true;
        for (int chunk = 0; chunk < 3; chunk++)
        {
            Fixture f = MakeFixture(); byte?[] source = Payload(48, 11); source[chunk * 16 + 15] = null;
            ActorCompletionResult r = Complete(f, ActorCompletionObservations.WithArchivedConstants(
                new ActorMatrixWarmObservation(true, 0, 0, source), Warm(91), Global()));
            stops &= r.Reason == "MissingMatrixSourceChunk" && r.ResumeInstructionRva == sites[chunk] &&
                r.SubobjectConstructorStores == 81 + chunk && r.MatrixHelperCalls == 1 &&
                r.MatrixWarmReturns == 1 && r.MatrixChunksCopied == chunk;
            bytes &= PrefixCopied(f.Sub, f.SubBefore, 0xE0, Payload(48, 11), chunk * 16);
            earlyEffects &= r.ElementReturned && r.EmptyVectorHelperReturned && r.SentinelStores == 2 &&
                f.Sub.ReadLittleEndian(0x148, 8) == SentinelToken && f.Sub.ReadLittleEndian(0x19C, 8) == 0x3F8000003F800000UL;
            notPublished &= !r.SubobjectReturned && !r.ActorObjectPublished && !r.NormalReturnReached &&
                Same(f.ActorBefore, f.Prior.State.CopyObservation()) && r.BackReferenceStores == 0;
            priors &= PriorUnchanged(f);
        }
        Need(stops, "first matrix has three independent instruction-level missing-chunk stops");
        Need(bytes, "earlier chunks remain copied, but an unknown last byte prevents the entire current chunk write");
        Need(earlyEffects, "matrix source failures keep sentinel, element, empty-vector and unit prefixes");
        Need(notPublished, "partial first matrix cannot return or publish the subobject");
        Need(priors, "first matrix failures preserve prior reports and earlier allocation");
        Fixture secondDataIgnored = MakeFixture(); ActorCompletionResult r0 = Complete(secondDataIgnored,
            ActorCompletionObservations.WithArchivedConstants(new ActorMatrixWarmObservation(true, 0, 0, null), null, null));
        Need(r0.Reason == "MissingMatrixSourceChunk" && r0.ResumeInstructionRva == 0xBDE27 &&
            r0.MatrixHelperCalls == 1 && !r0.GlobalSlotRead,
            "missing later observations do not replace the first required source failure");
    }

    static void SecondMatrixChunks()
    {
        uint[] sites = new uint[] { 0xBDEBE, 0xBDEC8, 0xBDED3 };
        bool stops = true, firstPreserved = true, partial = true, futureUntouched = true, priors = true;
        for (int chunk = 0; chunk < 3; chunk++)
        {
            Fixture f = MakeFixture(); byte?[] source = Payload(48, 91); source[chunk * 16 + 15] = null;
            ActorCompletionResult r = Complete(f, ActorCompletionObservations.WithArchivedConstants(Warm(11),
                new ActorMatrixWarmObservation(true, 0, 0, source), Global()));
            stops &= r.Reason == "MissingMatrixSourceChunk" && r.ResumeInstructionRva == sites[chunk] &&
                r.SubobjectConstructorStores == 95 + chunk && r.MatrixHelperCalls == 2 && r.MatrixWarmReturns == 2 &&
                r.MatrixChunksCopied == 3 + chunk;
            firstPreserved &= BytesAt(f.Sub, 0xE0, Payload(48, 11)) && f.Sub.ReadLittleEndian(0x22C, 8) == 0x3F8000003F800000UL;
            partial &= PrefixCopied(f.Sub, f.SubBefore, 0x1E4, Payload(48, 91), chunk * 16);
            futureUntouched &= SpanSame(f.SubBefore, f.Sub.CopyObservation(), 0x1D8, 12) &&
                SpanSame(f.SubBefore, f.Sub.CopyObservation(), 0x214, 24) &&
                Same(f.ActorBefore, f.Prior.State.CopyObservation()) && !r.SubobjectReturned && !r.ActorObjectPublished;
            priors &= PriorUnchanged(f);
        }
        Need(stops, "second matrix has its own three load sites and preserves completed first-call counts");
        Need(firstPreserved, "second source failures retain the independent first matrix and intervening scale stores");
        Need(partial, "second matrix commits complete earlier chunks without partially storing a missing chunk");
        Need(futureUntouched, "post-matrix tail and Actor publication remain untouched before second copy completion");
        Need(priors, "second matrix failures preserve earlier reports and array");
        Fixture missing = MakeFixture(); ActorCompletionResult absent = Complete(missing,
            ActorCompletionObservations.WithArchivedConstants(Warm(11), new ActorMatrixWarmObservation(true, 0, 0, null), Global()));
        Need(absent.ResumeInstructionRva == 0xBDEBE && absent.MatrixWarmReturns == 2 && absent.MatrixChunksCopied == 3 &&
            BytesAt(missing.Sub, 0xE0, Payload(48, 11)), "a missing second source cannot reuse the first call's data");
    }

    static void GlobalEquality()
    {
        ulong[] a = new ulong[] { 0, ulong.MaxValue, 0x0000000112345678UL, 0x8000000000000000UL, 7 };
        ulong[] b = new ulong[] { 0, ulong.MaxValue, 0x0000000212345678UL, 0, 8 };
        byte[] expected = new byte[] { 1, 1, 0, 0, 0 };
        bool counts = true, comparisons = true, tails = true, identities = true;
        for (int i = 0; i < a.Length; i++)
        {
            Fixture f = MakeFixture(); ActorCompletionResult r = Complete(f,
                ActorCompletionObservations.WithArchivedConstants(Warm(11), Warm(91), new ActorGlobalObservation(0x600000UL, a[i], b[i])));
            counts &= CompleteCounts(r) && r.GlobalSlotRead && r.Global678Read && r.Global668Read;
            comparisons &= f.Prior.State.ReadByte(0x4E1) == expected[i];
            tails &= f.Prior.State.ReadByte(0x4E0) == 0 && f.Prior.State.ReadByte(0x4E2) == 0 &&
                f.Prior.State.ReadLittleEndian(0x4E4, 4) == 0UL && f.Prior.State.ReadLittleEndian(0x48C, 8) == 0UL &&
                f.Prior.State.ReadLittleEndian(0x494, 8) == 0UL && f.Prior.State.ReadLittleEndian(0x528, 4) == 0UL;
            identities &= r.ReturnedSubobjectToken == SubToken && r.ReturnedActorToken == ActorToken &&
                f.Prior.State.ReadLittleEndian(0x1E0, 8) == SubToken && f.Sub.ReadLittleEndian(0x118, 8) == ActorToken;
        }
        Need(counts, "all known global comparisons finish the same conditional instruction path");
        Need(comparisons, "equality compares all sixty-four bits; zero/zero is valid only with a valid observed object");
        Need(tails, "both equality outcomes execute identical following zero stores");
        Need(identities, "global comparison never substitutes or changes either constructor return identity");
    }

    static void GlobalObservationStops()
    {
        ActorGlobalObservation[] observations = new ActorGlobalObservation[] {
            null, new ActorGlobalObservation(null, 0UL, 0UL), new ActorGlobalObservation(0UL, 0UL, 0UL),
            new ActorGlobalObservation(ulong.MaxValue - 0x67EUL, 0UL, 0UL),
            new ActorGlobalObservation(0x600000UL, null, 0UL), new ActorGlobalObservation(0x600000UL, 0UL, null) };
        string[] reasons = new string[] { "MissingGlobalSlotObservation", "MissingGlobalSlotObservation",
            "InvalidGlobalObjectIdentity", "InvalidGlobalObjectIdentity", "MissingGlobal678", "MissingGlobal668" };
        uint[] sites = new uint[] { 0x25D3D, 0x25D3D, 0x25D44, 0x25D44, 0x25D44, 0x25D4B };
        bool stops = true, reads = true, prefix = true, future = true, narrow = true, priors = true;
        for (int i = 0; i < observations.Length; i++)
        {
            Fixture f = MakeFixture(); ActorCompletionResult r = Complete(f,
                ActorCompletionObservations.WithArchivedConstants(Warm(11), Warm(91), observations[i]));
            stops &= r.Status == "Unsupported" && r.Reason == reasons[i] && r.ResumeInstructionRva == sites[i] &&
                r.SubobjectConstructorStores == 108 && r.ActorStoresAdded == 10 && r.TotalActorStores == 152;
            reads &= r.GlobalSlotRead == (i >= 2) && r.Global678Read == (i == 5) && !r.Global668Read;
            prefix &= r.SubobjectReturned && r.ActorObjectPublished && r.ReturnedSubobjectToken == SubToken &&
                r.BackReferenceStores == 2 && f.Sub.ReadLittleEndian(0x118, 8) == ActorToken &&
                f.Prior.State.ReadLittleEndian(0x49C, 8) == 0x3F8000003F800000UL &&
                !r.NormalReturnReached && r.ReturnedActorToken == null;
            byte?[] after = f.Prior.State.CopyObservation();
            future &= SpanSame(f.ActorBefore, after, 0x4E0, 8) && SpanSame(f.ActorBefore, after, 0x48C, 16) &&
                SpanSame(f.ActorBefore, after, 0x528, 4);
            narrow &= SpanSame(f.ActorBefore, after, 0x249, 7) && SpanSame(f.ActorBefore, after, 0x4C1, 7) &&
                SpanSame(f.ActorBefore, after, 0x4A8, 12);
            priors &= PriorUnchanged(f);
        }
        Need(stops, "slot, invalid object, first qword and second qword each stop at their actual dependency");
        Need(reads, "global read flags follow slot then 678 then 668 without eager consumption");
        Need(prefix, "global failures retain real subobject return, publication, back references and preceding scalar copy");
        Need(future, "global failures preserve equality destination and all later stores instead of rolling forward");
        Need(narrow, "completed byte and scalar writes do not widen across adjacent fields");
        Need(priors, "global failures leave earlier report counts and original array unchanged");
        Fixture edge = MakeFixture(); ActorCompletionResult atLimit = Complete(edge,
            ActorCompletionObservations.WithArchivedConstants(Warm(11), Warm(91), new ActorGlobalObservation(ulong.MaxValue - 0x67FUL, 0UL, 0UL)));
        Need(CompleteCounts(atLimit) && edge.Prior.State.ReadByte(0x4E1) == 1,
            "last non-overflowing global object span is an accepted own identity observation");
    }

    static void WidthAndStorageIsolation()
    {
        Fixture first = MakeFixture(), second = MakeFixture(); ActorCompletionResult r = Complete(first, Archived());
        byte?[] actorAfter = first.Prior.State.CopyObservation(); bool outside = true;
        for (int i = 0; i < actorAfter.Length; i++)
        {
            bool written = (i >= 0x1E0 && i <= 0x1E7) || i == 0x248 || i == 0x4C0 ||
                (i >= 0x4B4 && i <= 0x4BF) || (i >= 0x4C8 && i <= 0x4D3) ||
                (i >= 0x488 && i <= 0x4A7) || (i >= 0x4E0 && i <= 0x4E2) ||
                (i >= 0x4E4 && i <= 0x4E7) || (i >= 0x528 && i <= 0x52B);
            if (!written && actorAfter[i] != first.ActorBefore[i]) outside = false;
        }
        Need(outside, "full Actor observation preserves every byte outside the proven tail footprint");
        byte?[] subAfter = first.Sub.CopyObservation();
        int[] starts = new int[] { 0x3D, 0x114, 0x129, 0x171, 0x184, 0x241, 0x2CC, 0x2D2, 0x2EC, 0x301, 0x30A, 0x315, 0x331, 0x352 };
        int[] sizes = new int[] { 3, 4, 3, 7, 4, 3, 4, 2, 4, 7, 2, 3, 15, 6 };
        bool gaps = true;
        for (int i = 0; i < starts.Length; i++) gaps &= SpanSame(first.SubBefore, subAfter, starts[i], sizes[i]);
        Need(gaps, "subobject narrow, unaligned, inline-string and back-reference writes preserve their real gaps");
        Need(SpanSame(first.SentinelBefore, first.Sentinel.CopyObservation(), 16, 40),
            "nonzero initial sentinel payload is preserved beyond the two link qwords");
        Need(Same(second.ActorBefore, second.Prior.State.CopyObservation()) && Same(second.SubBefore, second.Sub.CopyObservation()) &&
            Same(second.SentinelBefore, second.Sentinel.CopyObservation()), "separate own fixtures cannot share mutable storage accidentally");
        byte?[] input = Payload(0x38, 149); byte? saved = input[20];
        ActorConstructorStorage isolated = new ActorConstructorStorage(0x400000UL, input); input[20] = null;
        byte?[] output = isolated.CopyObservation(); output[20] = null;
        byte?[] returnedSub = first.Sub.CopyObservation(); returnedSub[0x118] = null;
        Need(isolated.ReadByte(20) == saved && first.Sub.ReadLittleEndian(0x118, 8) == ActorToken,
            "storage copies its input and returns observation copies rather than live byte arrays");
        Need(CompleteCounts(r) && PriorUnchanged(first) && !r.IsCompleteNativeConstructor && r.NormalReturnReached,
            "bounded normal-return integration preserves the native-compatibility limitation and earlier counters");
    }

    public static int Main()
    {
        try
        {
            Run("CompleteArchivedIntegration", CompleteArchivedIntegration);
            Run("PriorAndIdentityGuards", PriorAndIdentityGuards);
            Run("SentinelBoundary", SentinelBoundary);
            Run("ConstantDependencyStops", ConstantDependencyStops);
            Run("RawConstantsAndInputCopies", RawConstantsAndInputCopies);
            Run("MatrixEpochBoundaries", MatrixEpochBoundaries);
            Run("FirstMatrixChunks", FirstMatrixChunks);
            Run("SecondMatrixChunks", SecondMatrixChunks);
            Run("GlobalEquality", GlobalEquality);
            Run("GlobalObservationStops", GlobalObservationStops);
            Run("WidthAndStorageIsolation", WidthAndStorageIsolation);
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
