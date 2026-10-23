using System;
using GrimDynasty.TaskItems;

// Bounded own state only. No OS TLS, lock, wait, event, callback or native image
// is executed. Synchronization/notification success remains explicit input.
// The old completion suite is a separate build-script invocation, not called here.
public static class ActorMatrixInitializationTests
{
    const ulong ActorToken = 0x500000UL, SubToken = 0x200000UL;
    static int assertions, groups;
    sealed class Fixture
    {
        public ActorContinuationResult Prior;
        public ActorConstructorStorage Sub, Sentinel;
        public byte?[] ActorBefore, SubBefore, ArrayBefore;
    }
    sealed class ProbeSource : IActorMatrixStateSource
    {
        readonly Func<int, ActorMatrixReadResult> read;
        public int Calls;
        public ProbeSource(Func<int, ActorMatrixReadResult> read) { this.read = read; }
        public ActorMatrixReadResult Read()
        {
            Calls++;
            if (Calls > 2) throw new Exception("Fixture source exceeded its independent two-call bound");
            return read(Calls);
        }
    }
    static void Need(bool condition, string message)
    {
        assertions++;
        if (!condition) throw new Exception(message);
    }
    static void Run(string name, Action body)
    {
        int before = assertions; body(); groups++;
        Console.WriteLine("PASS " + name + ": " + (assertions - before) + " assertions");
    }
    static byte?[] Payload(int length, int seed)
    {
        byte?[] bytes = new byte?[length];
        for (int i = 0; i < bytes.Length; i++) bytes[i] = (byte)(1 + ((seed + i * 31) % 254));
        return bytes;
    }
    static byte?[] TailInitialized(byte?[] input)
    {
        byte?[] expected = (byte?[])input.Clone();
        for (int i = 36; i < 48; i++) expected[i] = 0;
        return expected;
    }
    static bool Same(byte?[] a, byte?[] b)
    {
        if (a == null || b == null || a.Length != b.Length) return false;
        for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
        return true;
    }
    static bool BytesAt(ActorConstructorStorage storage, int at, byte?[] expected)
    {
        for (int i = 0; i < expected.Length; i++) if (storage.ReadByte(at + i) != expected[i]) return false;
        return true;
    }
    static bool PrefixCopied(Fixture f, int at, byte?[] source, int count)
    {
        for (int i = 0; i < 48; i++)
            if (f.Sub.ReadByte(at + i) != (i < count ? source[i] : f.SubBefore[at + i])) return false;
        return true;
    }
    static Fixture MakeFixture()
    {
        Fixture f = new Fixture();
        ActorConstructorPrefixState actor = new ActorConstructorPrefixState(Payload(0x530, 17));
        Actor198ArrayStorage array = new Actor198ArrayStorage(0x100000UL, Payload(0x208, 29));
        ActorArrayConstructionResult p = ActorConstructorArray.ApplyWithArchivedFileInitialCheckTarget(actor, array);
        f.Prior = ActorConstructorContinuation.ContinueUsingArchivedConstant(p, SubToken);
        f.Sub = new ActorConstructorStorage(SubToken, Payload(0x358, 41));
        f.Sentinel = new ActorConstructorStorage(0x300000UL, Payload(56, 53));
        f.ActorBefore = actor.CopyObservation(); f.SubBefore = f.Sub.CopyObservation(); f.ArrayBefore = array.CopyObservation();
        return f;
    }
    static ActorCompletionObservations Data()
    {
        // New live source supplies the matrix states; these old snapshots stay absent.
        return ActorCompletionObservations.WithArchivedConstants(null, null, new ActorGlobalObservation(0x600000UL, 7UL, 7UL));
    }
    static ActorCompletionResult Complete(Fixture f, IActorMatrixStateSource source, ActorCompletionObservations data)
    {
        return ActorConstructorCompletion.ContinueWithMatrixState(f.Prior, ActorToken, f.Sub, f.Sentinel, data, source);
    }
    static bool PriorPreserved(Fixture f)
    {
        return f.Prior.TotalActorStoreInstructions == 142 && f.Prior.ResumeInstructionRva == 0x25CC9 &&
            f.Prior.Prior.ActorStoreInstructions == 130 && Same(f.ArrayBefore, f.Prior.Prior.Array.CopyObservation());
    }
    static bool Returned(ActorMatrixReadResult result, string path)
    {
        return result.Returned && result.Path == path && result.Reason == null;
    }

    static void StorageAndArchivedFactories()
    {
        byte?[] input = Payload(48, 7), expected = (byte?[])Payload(48, 7).Clone();
        ActorMatrixInitializationState own = new ActorMatrixInitializationState(3, 9, input);
        input[0] = null; byte?[] copy = own.CopySource(); copy[47] = null;
        Need(Same(own.CopySource(), expected) && own.Provenance == "SuppliedState", "state snapshots input and exports independent copies");
        ActorMatrixInitializationState unknown = new ActorMatrixInitializationState(null, null, null);
        bool allUnknown = true; byte?[] unknownBytes = unknown.CopySource();
        for (int i = 0; i < unknownBytes.Length; i++) allUnknown &= unknownBytes[i] == null;
        Need(unknownBytes.Length == 48 && allUnknown && unknown.GuardEpoch == null && unknown.GlobalEpoch == null,
            "null source is forty-eight unknown bytes, not an initialized zero matrix");
        ActorMatrixInitializationState archived = ActorMatrixInitializationState.CreateArchivedInitialState();
        byte?[] golden = new byte?[48]; for (int i = 0; i < 48; i++) golden[i] = 0;
        golden[2] = golden[18] = golden[34] = 128; golden[3] = golden[19] = golden[35] = 63;
        Need(archived.GuardEpoch == 0 && archived.GlobalEpoch == int.MinValue && Same(archived.CopySource(), golden) &&
            archived.Provenance == "ArchivedFileInitialState", "archived state uses the verified guard, global epoch and exact file bits");
        ActorMatrixThreadState thread = ActorMatrixThreadState.CreateArchivedTemplateThread();
        Need(thread.ValidTlsPath && thread.Epoch == int.MinValue && thread.Provenance == "OwnThreadFromArchivedTemplate",
            "archived template creates an explicit own logical thread with the verified epoch");
        Need(archived.GuardStores == 0 && archived.GlobalEpochStores == 0 && archived.SourceStores == 0 && thread.EpochStores == 0,
            "observed initial values do not count as executed initialization stores");
        byte?[] changed = archived.CopySource(); changed[2] = 0;
        Need(Same(archived.CopySource(), golden) && Same(ActorMatrixInitializationState.CreateArchivedInitialState().CopySource(), golden),
            "archived factories and returned arrays do not share mutable source storage");
    }

    static void ColdThenWarm()
    {
        byte?[] before = Payload(48, 13);
        ActorMatrixInitializationState state = new ActorMatrixInitializationState(0, 10, before);
        ActorMatrixThreadState thread = new ActorMatrixThreadState(true, int.MinValue);
        ActorMatrixInitializationSource source = new ActorMatrixInitializationSource(state, thread, true, true);
        ActorMatrixReadResult first = source.Read();
        Need(Returned(first, "Initialized") && source.ReadCalls == 1 && source.HeaderCalls == 1 && source.FooterCalls == 1 &&
            source.NotificationCalls == 1 && source.InitializationReturns == 1 && source.WarmReturns == 0,
            "cold read performs the header, initialization and footer rather than relabeling itself warm");
        Need(state.GuardEpoch == 11 && state.GlobalEpoch == 11 && thread.Epoch == 11,
            "footer increments global, publishes guard, and updates the logical thread");
        Need(state.GuardStores == 2 && state.GlobalEpochStores == 1 && state.SourceStores == 2 && thread.EpochStores == 1,
            "claim, two source writes, footer and thread stores are counted separately");
        Need(Same(state.CopySource(), TailInitialized(before)), "cold initialization preserves the first thirty-six bytes and clears only the last twelve");
        ActorMatrixReadResult second = source.Read();
        Need(Returned(second, "Warm") && source.ReadCalls == 2 && source.WarmReturns == 1 && source.InitializationReturns == 1 &&
            source.HeaderCalls == 1 && source.FooterCalls == 1 && source.NotificationCalls == 1 && source.WaitCalls == 0,
            "second read samples updated shared state and takes the actual warm branch");
        Need(state.GuardStores == 2 && state.GlobalEpochStores == 1 && state.SourceStores == 2 && thread.EpochStores == 1 &&
            Same(state.CopySource(), TailInitialized(before)), "warm reread performs no initialization or epoch writes");
    }

    static void SharedStateHeaderExisting()
    {
        ActorMatrixInitializationState state = new ActorMatrixInitializationState(0, 10, Payload(48, 23));
        ActorMatrixThreadState firstThread = new ActorMatrixThreadState(true, int.MinValue);
        ActorMatrixInitializationSource first = new ActorMatrixInitializationSource(state, firstThread, true, true);
        first.Read(); byte?[] initialized = state.CopySource();
        ActorMatrixThreadState secondThread = new ActorMatrixThreadState(true, int.MinValue);
        ActorMatrixInitializationSource second = new ActorMatrixInitializationSource(state, secondThread, true, false);
        ActorMatrixReadResult result = second.Read();
        Need(Returned(result, "HeaderExisting") && second.HeaderCalls == 1 && second.HeaderExistingReturns == 1 &&
            second.FooterCalls == 0 && second.NotificationCalls == 0 && second.InitializationReturns == 0,
            "second logical thread uses the initialized header branch without another initializer or notification");
        Need(secondThread.Epoch == 11 && secondThread.EpochStores == 1 && firstThread.Epoch == 11 && firstThread.EpochStores == 1,
            "shared state updates only the thread reaching the header");
        Need(state.GuardStores == 2 && state.GlobalEpochStores == 1 && state.SourceStores == 2 && Same(initialized, state.CopySource()),
            "header-existing leaves the shared initialized data and guard untouched");
        ActorMatrixReadResult warm = second.Read();
        Need(Returned(warm, "Warm") && second.WarmReturns == 1 && second.HeaderCalls == 1 && secondThread.EpochStores == 1,
            "that thread subsequently becomes warm through its own updated epoch");
        ActorMatrixInitializationState unequal = new ActorMatrixInitializationState(7, 99, Payload(48, 31));
        ActorMatrixThreadState oldThread = new ActorMatrixThreadState(true, 0);
        ActorMatrixInitializationSource explicitState = new ActorMatrixInitializationSource(unequal, oldThread, true, false);
        ActorMatrixReadResult copiedGlobal = explicitState.Read();
        Need(Returned(copiedGlobal, "HeaderExisting") && oldThread.Epoch == 99 && unequal.GuardEpoch == 7,
            "header copies the observed global value, not the guard value");
        Need(unequal.GuardStores == 0 && unequal.GlobalEpochStores == 0 && unequal.SourceStores == 0 && oldThread.EpochStores == 1,
            "explicit unequal guard/global values do not invent additional state repair");
    }

    static void EntryAndSynchronizationStops()
    {
        ActorMatrixInitializationState[] states = new ActorMatrixInitializationState[] {
            new ActorMatrixInitializationState(0, 1, Payload(48, 7)), new ActorMatrixInitializationState(0, 1, Payload(48, 7)),
            new ActorMatrixInitializationState(0, 1, Payload(48, 7)), null,
            new ActorMatrixInitializationState(null, 1, Payload(48, 7)) };
        ActorMatrixThreadState[] threads = new ActorMatrixThreadState[] { null, new ActorMatrixThreadState(false, 0),
            new ActorMatrixThreadState(true, null), new ActorMatrixThreadState(true, 0), new ActorMatrixThreadState(true, 0) };
        uint[] sites = new uint[] { 0x273AB4, 0x273AB4, 0x273ACC, 0x273ACF, 0x273ACF };
        bool exact = true, unchanged = true;
        for (int i = 0; i < states.Length; i++)
        {
            byte?[] before = states[i] == null ? null : states[i].CopySource();
            ActorMatrixInitializationSource source = new ActorMatrixInitializationSource(states[i], threads[i], true, true);
            ActorMatrixReadResult r = source.Read();
            exact &= !r.Returned && r.StopRva == sites[i] && source.ReadCalls == 1 && source.HeaderCalls == 0 &&
                source.FooterCalls == 0 && source.WarmReturns == 0;
            unchanged &= (states[i] == null || (states[i].GuardStores == 0 && states[i].GlobalEpochStores == 0 &&
                states[i].SourceStores == 0 && Same(before, states[i].CopySource()))) && (threads[i] == null || threads[i].EpochStores == 0);
        }
        Need(exact, "unknown TLS, thread epoch and guard stop in their native read order");
        Need(unchanged, "entry failures cannot claim or initialize the shared state");
        ActorMatrixInitializationState noSync = new ActorMatrixInitializationState(0, 10, Payload(48, 17));
        ActorMatrixThreadState syncThread = new ActorMatrixThreadState(true, int.MinValue);
        ActorMatrixInitializationSource sync = new ActorMatrixInitializationSource(noSync, syncThread, false, true);
        ActorMatrixReadResult syncResult = sync.Read();
        Need(!syncResult.Returned && syncResult.Reason == "UnresolvedMatrixSynchronization" && syncResult.StopRva == 0x2DCAF0 &&
            sync.HeaderCalls == 1 && sync.FooterCalls == 0, "slow branch requires the explicit normal exclusive synchronization contract");
        Need(noSync.GuardEpoch == 0 && noSync.GuardStores == 0 && noSync.SourceStores == 0 && syncThread.EpochStores == 0,
            "unknown synchronization stops before guard claim or source writes");
        ActorMatrixInitializationState waiting = new ActorMatrixInitializationState(-1, 10, Payload(48, 19));
        ActorMatrixThreadState waitingThread = new ActorMatrixThreadState(true, int.MinValue);
        ActorMatrixInitializationSource wait = new ActorMatrixInitializationSource(waiting, waitingThread, true, true);
        ActorMatrixReadResult waitResult = wait.Read();
        Need(!waitResult.Returned && waitResult.Reason == "UnresolvedMatrixWait" && waitResult.StopRva == 0x2DCB05 &&
            wait.WaitCalls == 1 && wait.HeaderCalls == 1 && wait.FooterCalls == 0, "contended guard stops at the wait call without executing a loop");
        Need(waiting.GuardEpoch == -1 && waiting.GuardStores == 0 && waiting.SourceStores == 0 && waitingThread.EpochStores == 0,
            "unresolved wait does not synthesize a wake-up, claim or epoch update");
        Need(wait.ReadCalls == 1 && wait.NotificationCalls == 0 && wait.InitializationReturns == 0 && wait.HeaderExistingReturns == 0,
            "wait path returns no initialization or header-existing success");
    }

    static void GlobalAndNotificationPrefixes()
    {
        ActorMatrixInitializationState headerState = new ActorMatrixInitializationState(5, null, Payload(48, 29));
        ActorMatrixThreadState headerThread = new ActorMatrixThreadState(true, 0);
        ActorMatrixInitializationSource header = new ActorMatrixInitializationSource(headerState, headerThread, true, true);
        ActorMatrixReadResult h = header.Read();
        Need(!h.Returned && h.Reason == "MissingMatrixGlobalEpoch" && h.StopRva == 0x2DCB2A && header.HeaderCalls == 1 &&
            header.FooterCalls == 0 && header.HeaderExistingReturns == 0, "existing-header branch stops at its global read");
        Need(headerState.GuardEpoch == 5 && headerState.GuardStores == 0 && headerState.SourceStores == 0 && headerThread.EpochStores == 0,
            "missing header global does not overwrite thread or shared state");
        byte?[] before = Payload(48, 37);
        ActorMatrixInitializationState footerState = new ActorMatrixInitializationState(0, null, before);
        ActorMatrixThreadState footerThread = new ActorMatrixThreadState(true, int.MinValue);
        ActorMatrixInitializationSource footer = new ActorMatrixInitializationSource(footerState, footerThread, true, true);
        ActorMatrixReadResult f = footer.Read();
        Need(!f.Returned && f.StopRva == 0x2DCA96 && f.Reason == "MissingMatrixGlobalEpoch" && footer.FooterCalls == 1 &&
            footerState.GuardEpoch == -1 && footerState.GuardStores == 1 && footerState.SourceStores == 2,
            "missing footer global retains guard claim and both initialization writes");
        Need(Same(footerState.CopySource(), TailInitialized(before)) && footerState.GlobalEpochStores == 0 &&
            footerThread.Epoch == int.MinValue && footerThread.EpochStores == 0 && footer.NotificationCalls == 0,
            "footer failure preserves the first thirty-six bytes and cannot invent a global or thread epoch");
        ActorMatrixInitializationState notifyState = new ActorMatrixInitializationState(0, 23, Payload(48, 43));
        ActorMatrixThreadState notifyThread = new ActorMatrixThreadState(true, int.MinValue);
        ActorMatrixInitializationSource notify = new ActorMatrixInitializationSource(notifyState, notifyThread, true, false);
        ActorMatrixReadResult n = notify.Read();
        Need(!n.Returned && n.Reason == "UnresolvedMatrixNotification" && n.StopRva == 0x2DCB48 &&
            notify.NotificationCalls == 1 && notify.InitializationReturns == 0, "notification is an explicit unresolved dependency after footer publication");
        Need(notifyState.GuardEpoch == 24 && notifyState.GlobalEpoch == 24 && notifyThread.Epoch == 24 &&
            notifyState.GuardStores == 2 && notifyState.GlobalEpochStores == 1 && notifyState.SourceStores == 2 && notifyThread.EpochStores == 1,
            "unknown notification does not roll back the published guard/global/thread prefix");
        Need(h.Path == "Stopped" && f.Path == "Stopped" && n.Path == "Stopped" &&
            header.WaitCalls == 0 && footer.WaitCalls == 0 && notify.WaitCalls == 0,
            "none of these stopped dependencies is promoted to a warm or initialized return");
    }

    static void SignedOrderingAndWrap()
    {
        int[] guards = new int[] { -1, int.MinValue, int.MinValue, int.MaxValue };
        int[] epochs = new int[] { 0, int.MaxValue, int.MinValue, int.MaxValue };
        bool warm = true, noWrites = true;
        for (int i = 0; i < guards.Length; i++)
        {
            ActorMatrixInitializationState state = new ActorMatrixInitializationState(guards[i], null, Payload(48, 11));
            ActorMatrixThreadState thread = new ActorMatrixThreadState(true, epochs[i]);
            ActorMatrixInitializationSource source = new ActorMatrixInitializationSource(state, thread, false, false);
            ActorMatrixReadResult r = source.Read();
            warm &= Returned(r, "Warm") && source.WarmReturns == 1 && source.HeaderCalls == 0 && source.WaitCalls == 0;
            noWrites &= state.GuardStores == 0 && state.GlobalEpochStores == 0 && state.SourceStores == 0 && thread.EpochStores == 0;
        }
        Need(warm, "signed warm comparison precedes special guard-minus-one handling and requires no synchronization/global observation");
        Need(noWrites, "all signed warm cases preserve shared and per-thread state");
        int[] globals = new int[] { int.MaxValue, -1, int.MinValue };
        int[] next = new int[] { int.MinValue, 0, int.MinValue + 1 };
        bool wraps = true, counts = true;
        for (int i = 0; i < globals.Length; i++)
        {
            ActorMatrixInitializationState state = new ActorMatrixInitializationState(0, globals[i], Payload(48, 13));
            ActorMatrixThreadState thread = new ActorMatrixThreadState(true, int.MinValue);
            ActorMatrixInitializationSource source = new ActorMatrixInitializationSource(state, thread, true, true);
            ActorMatrixReadResult first = source.Read(), second = source.Read();
            wraps &= Returned(first, "Initialized") && Returned(second, "Warm") && state.GlobalEpoch == next[i] &&
                state.GuardEpoch == next[i] && thread.Epoch == next[i];
            counts &= state.GuardStores == 2 && state.GlobalEpochStores == 1 && state.SourceStores == 2 && thread.EpochStores == 1 &&
                source.InitializationReturns == 1 && source.WarmReturns == 1;
        }
        Need(wraps, "footer arithmetic wraps exactly at thirty-two bits, including a published zero epoch");
        Need(counts, "wraparound does not repeat initialization on the immediate equal-epoch reread");
        ActorMatrixInitializationState explicitState = new ActorMatrixInitializationState(int.MaxValue, 1, Payload(48, 17));
        ActorMatrixThreadState explicitThread = new ActorMatrixThreadState(true, int.MinValue);
        ActorMatrixInitializationSource explicitSource = new ActorMatrixInitializationSource(explicitState, explicitThread, true, true);
        ActorMatrixReadResult one = explicitSource.Read();
        Need(Returned(one, "HeaderExisting") && explicitThread.Epoch == 1 && explicitState.GuardEpoch == int.MaxValue,
            "header-existing returns after copying global without imposing an invented guard/global invariant");
        ActorMatrixReadResult two = explicitSource.Read();
        Need(Returned(two, "HeaderExisting") && explicitSource.HeaderCalls == 2 && explicitSource.WarmReturns == 0 &&
            explicitState.GuardStores == 0 && explicitThread.EpochStores == 2,
            "a subsequent call rechecks actual epochs rather than caching a warm label");
    }

    static void DeferredIntegration()
    {
        Fixture f = MakeFixture(); byte?[] raw = Payload(48, 31), expected = TailInitialized(Payload(48, 31));
        ActorMatrixInitializationState state = new ActorMatrixInitializationState(0, 10, raw);
        ActorMatrixThreadState thread = new ActorMatrixThreadState(true, int.MinValue);
        ActorMatrixInitializationSource source = new ActorMatrixInitializationSource(state, thread, true, true);
        bool correctTiming = true;
        ProbeSource probe = new ProbeSource(delegate(int call) {
            if (call == 1) correctTiming &= f.Sub.ReadLittleEndian(0x19C, 8) == 0x3F8000003F800000UL &&
                PrefixCopied(f, 0xE0, expected, 0) && Same(f.ActorBefore, f.Prior.State.CopyObservation());
            else correctTiming &= BytesAt(f.Sub, 0xE0, expected) && f.Sub.ReadLittleEndian(0x22C, 8) == 0x3F8000003F800000UL &&
                f.Sub.ReadLittleEndian(0x1BC, 4) == 0UL && PrefixCopied(f, 0x1E4, expected, 0);
            return source.Read();
        });
        ActorCompletionResult result = Complete(f, probe, Data());
        Need(correctTiming && probe.Calls == 2 && source.ReadCalls == 2, "same source is read at the two actual matrix sites, never pre-sampled at entry");
        Need(result.Status == "ConditionalNormalReturn" && result.MatrixStateSourceCalls == 2 && result.MatrixInitializationReturns == 1 &&
            result.MatrixWarmReturns == 1 && result.MatrixHeaderExistingReturns == 0 && result.MatrixChunksCopied == 6 &&
            result.SubobjectConstructorStores == 108 && result.TotalActorStores == 158,
            "real initialization then warm return retain distinct counters through the full chain");
        Need(BytesAt(f.Sub, 0xE0, expected) && BytesAt(f.Sub, 0x1E4, expected),
            "both matrices use state returned at their call, including the initialized tail");
        Need(state.GlobalEpoch == 11 && state.GuardEpoch == 11 && thread.Epoch == 11 && state.SourceStores == 2 &&
            state.GuardStores == 2 && thread.EpochStores == 1 && source.FooterCalls == 1,
            "first read updates live state and second read observes it without reinitializing");
        Need(result.ReturnedSubobjectToken == SubToken && result.ReturnedActorToken == ActorToken && result.BackReferenceStores == 2 &&
            f.Prior.State.ReadLittleEndian(0x1E0, 8) == SubToken && f.Sub.ReadLittleEndian(0x118, 8) == ActorToken,
            "initialization integration retains the proven constructor and caller return identities");
        Need(PriorPreserved(f), "integration preserves previous reports and the original 520-byte array");
        Need(result.NormalReturnReached && !result.IsCompleteNativeConstructor && result.ArchivedFileInitialCheckUsed &&
            result.ConstantProvenance == "ArchivedFileImage", "normal return does not assert OS TLS, native guard, notification or compatibility recovery");
    }

    static void EarlyIntegrationStops()
    {
        Fixture absent = MakeFixture(); ProbeSource unused = new ProbeSource(delegate(int n) { return ActorMatrixReadResult.Success("Warm", Payload(48, 7)); });
        ActorCompletionResult noPrior = ActorConstructorCompletion.ContinueWithMatrixState(null, ActorToken, absent.Sub, absent.Sentinel, Data(), unused);
        Need(noPrior.Reason == "MissingPriorActorState" && unused.Calls == 0 && noPrior.MatrixStateSourceCalls == 0,
            "prior validation does not touch the state source");
        Fixture early = MakeFixture(); ActorCompletionObservations noVector = new ActorCompletionObservations(null, 0x3F800000U, null,
            ActorConstructorContinuation.CopyArchivedConstant(), null, 0x3F800000U, new ActorGlobalObservation(0x600000UL, 0UL, 0UL));
        ActorCompletionResult beforeMatrix = Complete(early, unused, noVector);
        Need(beforeMatrix.Reason == "MissingSubobjectVector" && beforeMatrix.SubobjectConstructorStores == 47 && unused.Calls == 0,
            "an earlier constant dependency prevents even the first matrix state read");
        Fixture partial = MakeFixture(); ActorMatrixInitializationState state = new ActorMatrixInitializationState(0, null, Payload(48, 13));
        ActorMatrixThreadState thread = new ActorMatrixThreadState(true, int.MinValue);
        ActorMatrixInitializationSource source = new ActorMatrixInitializationSource(state, thread, true, true);
        ActorCompletionResult stopped = Complete(partial, source, Data());
        Need(stopped.Reason == "MissingMatrixGlobalEpoch" && stopped.ResumeInstructionRva == 0x2DCA96 && source.ReadCalls == 1 &&
            stopped.MatrixStateSourceCalls == 1 && stopped.SubobjectConstructorStores == 81 && stopped.MatrixInitializationReturns == 0,
            "first state-source failure propagates its exact inner instruction and prevents the second read");
        Need(state.GuardEpoch == -1 && state.SourceStores == 2 && stopped.MatrixChunksCopied == 0 &&
            Same(partial.ActorBefore, partial.Prior.State.CopyObservation()) && PrefixCopied(partial, 0xE0, Payload(48, 13), 0) &&
            !stopped.SubobjectReturned && PriorPreserved(partial), "state initialization prefix survives although caller matrix copy has not begun");
        Fixture missingScale = MakeFixture(); ActorMatrixInitializationSource initialized = new ActorMatrixInitializationSource(
            new ActorMatrixInitializationState(0, 10, Payload(48, 17)), new ActorMatrixThreadState(true, int.MinValue), true, true);
        ActorCompletionObservations noScale = new ActorCompletionObservations(new byte[16], 0x3F800000U, null, null, null,
            0x3F800000U, new ActorGlobalObservation(0x600000UL, 0UL, 0UL));
        ActorCompletionResult scaleStop = Complete(missingScale, initialized, noScale);
        Need(scaleStop.Reason == "MissingSubobjectScale" && scaleStop.SubobjectConstructorStores == 84 && initialized.ReadCalls == 1 &&
            scaleStop.MatrixInitializationReturns == 1 && scaleStop.MatrixChunksCopied == 3 && !scaleStop.ActorObjectPublished,
            "a dependency between matrix calls retains the first initialization and copy without reading the second time");
        Fixture nullSource = MakeFixture(); ActorCompletionResult noSource = Complete(nullSource, null, Data());
        Need(noSource.Reason == "MissingMatrixStateSource" && noSource.ResumeInstructionRva == 0x273AB4 &&
            noSource.MatrixStateSourceCalls == 0 && noSource.SubobjectConstructorStores == 81,
            "missing live source is checked only when its first call is reached");
    }

    static void PartialMatrixSources()
    {
        Fixture unknown = MakeFixture(); ActorMatrixInitializationState state = new ActorMatrixInitializationState(0, 10, null);
        ActorMatrixThreadState thread = new ActorMatrixThreadState(true, int.MinValue);
        ActorMatrixInitializationSource source = new ActorMatrixInitializationSource(state, thread, true, true);
        ActorCompletionResult r = Complete(unknown, source, Data());
        Need(r.Reason == "MissingMatrixSourceChunk" && r.ResumeInstructionRva == 0xBDE27 && r.MatrixInitializationReturns == 1 &&
            r.MatrixStateSourceCalls == 1 && r.MatrixChunksCopied == 0 && r.SubobjectConstructorStores == 81,
            "completed initialization cannot invent the unknown first thirty-six source bytes");
        byte?[] expected = new byte?[48]; for (int i = 36; i < 48; i++) expected[i] = 0;
        Need(Same(state.CopySource(), expected) && state.GuardEpoch == 11 && thread.Epoch == 11 &&
            !r.ActorObjectPublished && Same(unknown.ActorBefore, unknown.Prior.State.CopyObservation()),
            "state completion stays visible despite caller copy failure");
        uint[] sites = new uint[] { 0xBDE27, 0xBDE31, 0xBDE3C };
        bool exact = true, preserved = true;
        for (int chunk = 0; chunk < 3; chunk++)
        {
            Fixture f = MakeFixture(); byte?[] bytes = Payload(48, 23); bytes[chunk * 16 + 15] = null;
            ActorMatrixInitializationState warmState = new ActorMatrixInitializationState(0, null, bytes);
            ActorMatrixThreadState warmThread = new ActorMatrixThreadState(true, 0);
            ActorMatrixInitializationSource warm = new ActorMatrixInitializationSource(warmState, warmThread, false, false);
            ActorCompletionResult one = Complete(f, warm, Data());
            exact &= one.Reason == "MissingMatrixSourceChunk" && one.ResumeInstructionRva == sites[chunk] &&
                one.SubobjectConstructorStores == 81 + chunk && one.MatrixWarmReturns == 1 && one.MatrixStateSourceCalls == 1 &&
                one.MatrixChunksCopied == chunk && warm.ReadCalls == 1;
            preserved &= PrefixCopied(f, 0xE0, Payload(48, 23), chunk * 16) && Same(f.ActorBefore, f.Prior.State.CopyObservation()) &&
                warmState.SourceStores == 0 && warmThread.EpochStores == 0 && PriorPreserved(f);
        }
        Need(exact, "live warm results retain all three distinct first-copy failure sites");
        Need(preserved, "complete prior chunks remain while an unknown final byte prevents the whole current sixteen-byte store");
        Fixture second = MakeFixture(); ProbeSource two = new ProbeSource(delegate(int n) {
            byte?[] bytes = Payload(48, n == 1 ? 31 : 71); if (n == 2) bytes[47] = null;
            return ActorMatrixReadResult.Success("Warm", bytes);
        });
        ActorCompletionResult partial = Complete(second, two, Data());
        Need(partial.ResumeInstructionRva == 0xBDED3 && partial.MatrixStateSourceCalls == 2 && partial.MatrixWarmReturns == 2 &&
            partial.MatrixChunksCopied == 5 && partial.SubobjectConstructorStores == 97 && two.Calls == 2,
            "second live result is read independently and reports its own third-chunk stop");
        Need(BytesAt(second.Sub, 0xE0, Payload(48, 31)) && PrefixCopied(second, 0x1E4, Payload(48, 71), 32) &&
            !partial.SubobjectReturned && Same(second.ActorBefore, second.Prior.State.CopyObservation()),
            "second partial copy preserves the distinct first result and avoids premature publication");
    }

    static void ReadResultsAndOldApiParity()
    {
        Fixture copy = MakeFixture(); ProbeSource cloned = new ProbeSource(delegate(int n) {
            byte?[] bytes = Payload(48, n == 1 ? 37 : 79);
            ActorMatrixReadResult result = ActorMatrixReadResult.Success("Warm", bytes);
            bytes[0] = null; bytes[47] = null; return result;
        });
        ActorCompletionResult copied = Complete(copy, cloned, Data());
        Need(copied.NormalReturnReached && copied.MatrixStateSourceCalls == 2 && copied.MatrixWarmReturns == 2,
            "returned result snapshots remain valid after the source mutates its input array");
        Need(BytesAt(copy.Sub, 0xE0, Payload(48, 37)) && BytesAt(copy.Sub, 0x1E4, Payload(48, 79)),
            "each successful result owns an independent exact source snapshot");
        Fixture missing = MakeFixture(); ProbeSource returnsNull = new ProbeSource(delegate(int n) { return null; });
        ActorCompletionResult nullResult = Complete(missing, returnsNull, Data());
        Need(nullResult.Reason == "MissingMatrixStateResult" && nullResult.ResumeInstructionRva == 0x273AB4 &&
            nullResult.MatrixStateSourceCalls == 1 && returnsNull.Calls == 1 && nullResult.SubobjectConstructorStores == 81,
            "a null source result is unsupported without invoking the second call");
        Fixture explicitStop = MakeFixture(); ProbeSource unsupported = new ProbeSource(delegate(int n) {
            return ActorMatrixReadResult.Unsupported("ExplicitOwnedStop", 0x273AEA);
        });
        ActorCompletionResult propagated = Complete(explicitStop, unsupported, Data());
        Need(propagated.Reason == "ExplicitOwnedStop" && propagated.ResumeInstructionRva == 0x273AEA &&
            propagated.MatrixStateSourceCalls == 1 && propagated.MatrixWarmReturns == 0,
            "unsupported source result retains its reason and exact stop without classifying it as returned");
        Fixture old = MakeFixture(), live = MakeFixture(); byte?[] stable = Payload(48, 47);
        ActorMatrixWarmObservation observed = new ActorMatrixWarmObservation(true, 5, 5, stable);
        ActorCompletionObservations oldData = ActorCompletionObservations.WithArchivedConstants(observed, observed,
            new ActorGlobalObservation(0x600000UL, 7UL, 7UL));
        // One new stable comparison, not execution of the old suite.
        ActorCompletionResult oldResult = ActorConstructorCompletion.Continue(old.Prior, ActorToken, old.Sub, old.Sentinel, oldData);
        ActorMatrixInitializationState warmState = new ActorMatrixInitializationState(5, null, stable);
        ActorMatrixThreadState warmThread = new ActorMatrixThreadState(true, 5);
        ActorMatrixInitializationSource liveSource = new ActorMatrixInitializationSource(warmState, warmThread, false, false);
        ActorCompletionResult liveResult = Complete(live, liveSource, Data());
        Need(Same(old.Prior.State.CopyObservation(), live.Prior.State.CopyObservation()) && Same(old.Sub.CopyObservation(), live.Sub.CopyObservation()) &&
            Same(old.Sentinel.CopyObservation(), live.Sentinel.CopyObservation()), "stable old snapshots and new live warm state produce identical own byte observations");
        Need(oldResult.NormalReturnReached && liveResult.NormalReturnReached && oldResult.MatrixStateSourceCalls == 0 &&
            liveResult.MatrixStateSourceCalls == 2 && oldResult.MatrixWarmReturns == 2 && liveResult.MatrixWarmReturns == 2 &&
            oldResult.MatrixInitializationReturns == 0 && liveResult.MatrixInitializationReturns == 0 &&
            oldResult.TotalActorStores == 158 && liveResult.TotalActorStores == 158,
            "old interface counters remain unchanged while new state-source reads are separately counted");
        Need(liveSource.HeaderCalls == 0 && liveSource.FooterCalls == 0 && warmState.SourceStores == 0 &&
            warmState.GuardStores == 0 && warmThread.EpochStores == 0 && !liveResult.IsCompleteNativeConstructor,
            "stable warm parity does not imply initialized OS state or introduce extra mutations");
    }

    public static int Main()
    {
        try
        {
            Run("StorageAndArchivedFactories", StorageAndArchivedFactories);
            Run("ColdThenWarm", ColdThenWarm);
            Run("SharedStateHeaderExisting", SharedStateHeaderExisting);
            Run("EntryAndSynchronizationStops", EntryAndSynchronizationStops);
            Run("GlobalAndNotificationPrefixes", GlobalAndNotificationPrefixes);
            Run("SignedOrderingAndWrap", SignedOrderingAndWrap);
            Run("DeferredIntegration", DeferredIntegration);
            Run("EarlyIntegrationStops", EarlyIntegrationStops);
            Run("PartialMatrixSources", PartialMatrixSources);
            Run("ReadResultsAndOldApiParity", ReadResultsAndOldApiParity);
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
