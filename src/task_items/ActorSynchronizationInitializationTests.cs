using System;
using GrimDynasty.TaskItems;

// Own call observations only: no OS, original DLL, thread or native callback runs.
// Exceptions injected below describe this own interface, not native EH behavior.
public static class ActorSynchronizationInitializationTests
{
    const ulong Module = 0x7000UL, Initialize = 0x1111UL;
    const ulong Wait = 0x0123456789ABCDEFUL, Wake = 0xFEDCBA9876543210UL;
    const ulong OldEvent = 0x99UL, OldWait = 0xA1UL, OldWake = 0xA2UL;
    static int assertions, groups;

    sealed class Calls : IActorSyncInitializationCalls
    {
        public ulong? Critical = 0, ModuleValue = Module, InitializeValue = Initialize;
        public ulong? WaitValue = Wait, WakeValue = Wake, EventValue = 0xEEUL;
        public ulong? CleanupValue = 0xFFFFFFFFUL; // EAX=-1 is zero-extended into RAX.
        public ActorSyncInitOperation? Unresolved, NullObservation;
        public Action<ActorSyncInitCall, ActorSyncInitializationState> Before;
        public int Count;
        public ActorSyncCallObservation Invoke(ActorSyncInitCall call, ActorSyncInitializationState state)
        {
            Count++;
            if (Before != null) Before(call, state);
            if (Unresolved.HasValue && call.Operation == Unresolved.Value) return ActorSyncCallObservation.Unresolved();
            if (NullObservation.HasValue && call.Operation == NullObservation.Value) return null;
            switch (call.Operation)
            {
                case ActorSyncInitOperation.InitializeCriticalSection: return ActorSyncCallObservation.Returned(Critical);
                case ActorSyncInitOperation.GetModule: return ActorSyncCallObservation.Returned(ModuleValue);
                case ActorSyncInitOperation.GetInitializeCallback: return ActorSyncCallObservation.Returned(InitializeValue);
                case ActorSyncInitOperation.GetWaitCallback: return ActorSyncCallObservation.Returned(WaitValue);
                case ActorSyncInitOperation.GetWakeCallback: return ActorSyncCallObservation.Returned(WakeValue);
                case ActorSyncInitOperation.CreateEvent: return ActorSyncCallObservation.Returned(EventValue);
                case ActorSyncInitOperation.RegisterCleanup: return ActorSyncCallObservation.Returned(CleanupValue);
                case ActorSyncInitOperation.CheckIndirectTarget:
                case ActorSyncInitOperation.InitializeCondition:
                case ActorSyncInitOperation.FatalHandler: return ActorSyncCallObservation.Returned(null);
                default: throw new Exception("Unexpected fixture operation");
            }
        }
    }
    static ActorSyncInitializationState State(ulong? cookie)
    {
        return new ActorSyncInitializationState(cookie, OldEvent, OldWait, OldWake);
    }
    static void Need(bool condition, string message)
    {
        assertions++;
        if (!condition) throw new Exception(message);
    }
    static void Group(string name, Action run)
    {
        int before = assertions; run(); groups++;
        Console.WriteLine("PASS " + name + ": " + (assertions - before) + " assertions");
    }
    static bool Args(ActorSyncInitCall call, params ulong[] expected)
    {
        ulong[] actual = call.Arguments;
        if (actual.Length != expected.Length) return false;
        for (int i = 0; i < actual.Length; i++) if (actual[i] != expected[i]) return false;
        return true;
    }
    static bool Sequence(ActorSynchronizationInitialization run, params ActorSyncInitOperation[] expected)
    {
        ActorSyncInitCall[] actual = run.Trace;
        if (actual.Length != expected.Length) return false;
        for (int i = 0; i < actual.Length; i++) if (actual[i].Operation != expected[i]) return false;
        return true;
    }
    static bool Stop(ActorSyncInitializationResult result, string reason, uint rva)
    {
        return !result.ReturnedNormally && result.Path == "Stopped" && result.ReturnValue == null &&
            result.Reason == reason && result.StopRva == rva && !result.IsNativeSynchronizationReady;
    }
    static bool Throws<T>(Action action) where T : Exception
    {
        try { action(); return false; }
        catch (Exception e) { return e.GetType() == typeof(T); }
    }
    static bool OriginalEncoded(ActorSyncInitializationState state)
    {
        return state.EncodedWait == OldWait && state.EncodedWake == OldWake;
    }
    static void ConditionOrderAndCookie()
    {
        var state = State(0); var calls = new Calls();
        bool clearedAtGuard = false, clearedAtCallback = false;
        calls.Before = delegate(ActorSyncInitCall call, ActorSyncInitializationState s)
        {
            if (call.Operation == ActorSyncInitOperation.CheckIndirectTarget)
                clearedAtGuard = s.EventSlot == 0 && s.Stores.Length == 1 && OriginalEncoded(s);
            if (call.Operation == ActorSyncInitOperation.InitializeCondition)
            {
                clearedAtCallback = s.EventSlot == 0 && s.Stores.Length == 1 && OriginalEncoded(s);
                s.Cookie = 1; // The writer must re-read this after the own callback.
            }
        };
        var run = new ActorSynchronizationInitialization(state, calls); var result = run.Run();
        Need(result.ReturnedNormally && result.Path == "ConditionVariable" && result.ReturnValue == 0 &&
            !result.IsNativeSynchronizationReady, "Own normal return is not native readiness");
        Need(state.CriticalSectionCallReturned && state.ConditionCallReturned,
            "Ignored critical return zero and void callback return still continue");
        Need(Sequence(run, ActorSyncInitOperation.InitializeCriticalSection, ActorSyncInitOperation.GetModule,
            ActorSyncInitOperation.GetInitializeCallback, ActorSyncInitOperation.GetWaitCallback,
            ActorSyncInitOperation.GetWakeCallback, ActorSyncInitOperation.CheckIndirectTarget,
            ActorSyncInitOperation.InitializeCondition, ActorSyncInitOperation.RegisterCleanup), "Exact condition call sequence");
        Need(Args(run.Trace[0], 0x420D88, 4000, 0) && Args(run.Trace[1], 0x307388) &&
            Args(run.Trace[2], Module, 0x3073A8) && Args(run.Trace[3], Module, 0x3073C8) &&
            Args(run.Trace[4], Module, 0x3073E8), "Fixed initialization and all lookup arguments");
        Need(Args(run.Trace[5], Initialize) && Args(run.Trace[6], Initialize, 0x420DB0) &&
            Args(run.Trace[7], 0x2DCA28), "Guard, callback and cleanup identity arguments");
        Need(clearedAtGuard && clearedAtCallback, "Event clear precedes guard and callback");
        Need(state.Cookie == 1 && state.EncodedWait == 0x02468ACF13579BDFUL &&
            state.EncodedWake == 0xFDB97530ECA86420UL, "Encoding uses the changed post-callback cookie");
        var stores = state.Stores;
        Need(stores.Length == 3 && stores[0].InstructionRva == 0x2DC98C && stores[0].SlotRva == 0x420DB8 && stores[0].Value == 0 &&
            stores[1].InstructionRva == 0x2DC9C4 && stores[1].SlotRva == 0x420DC0 &&
            stores[2].InstructionRva == 0x2DC9CB && stores[2].SlotRva == 0x420DC8,
            "Writer ordering is preserved despite cleanup returning minus one");
    }
    static void EncodingVectors()
    {
        Need(ActorSynchronizationInitialization.Encode(Wait, 0) == 0x0123456789ABCDEFUL, "Literal rotation-zero golden");
        Need(ActorSynchronizationInitialization.Encode(Wait, 1) == 0x02468ACF13579BDFUL, "Literal rotation-one golden");
        Need(ActorSynchronizationInitialization.Encode(Wait, 63) == 0x8091A2B3C4D5E6C8UL, "Literal rotation-63 golden");
        // Independently precomputed by integer product/modulo and quotient:
        // (T*2^17 mod2^64)+(T/2^47)=8ACF13579BDE0246, then XOR the explicit cookie.
        Need(ActorSynchronizationInitialization.Encode(Wait, 0xA5A55A5AF0F00F11UL) == 0x2F6A490D6B2E0D57UL,
            "Literal count17 golden includes nonzero upper cookie bits");
        Need(ActorSynchronizationInitialization.Decode(0x0123456789ABCDEFUL, 0) == Wait &&
            ActorSynchronizationInitialization.Decode(0x02468ACF13579BDFUL, 1) == Wait &&
            ActorSynchronizationInitialization.Decode(0x8091A2B3C4D5E6C8UL, 63) == Wait &&
            ActorSynchronizationInitialization.Decode(0x2F6A490D6B2E0D57UL, 0xA5A55A5AF0F00F11UL) == Wait,
            "Independent encoded goldens decode");
        bool inverse = true;
        for (int i = 0; i < 64; i++)
        {
            ulong cookie = 0xA5A55A5AF0F00F00UL | (ulong)i;
            inverse &= ActorSynchronizationInitialization.Decode(ActorSynchronizationInitialization.Encode(Wait, cookie), cookie) == Wait;
            inverse &= ActorSynchronizationInitialization.Decode(ActorSynchronizationInitialization.Encode(Wake, cookie), cookie) == Wake;
        }
        Need(inverse, "All64 rotation counts with nonzero upper cookie bits, counted as one bounded check");
    }
    static void FallbackEachMissingPointer()
    {
        for (int missing = 0; missing < 3; missing++)
        {
            var state = State(null); var calls = new Calls();
            if (missing == 0) calls.InitializeValue = 0;
            if (missing == 1) calls.WaitValue = 0;
            if (missing == 2) calls.WakeValue = 0;
            bool publishedAtCleanup = false;
            calls.Before = delegate(ActorSyncInitCall call, ActorSyncInitializationState s)
            {
                if (call.Operation == ActorSyncInitOperation.RegisterCleanup)
                    publishedAtCleanup = s.EventSlot == 0xEE && s.Stores.Length == 1 && OriginalEncoded(s);
            };
            var run = new ActorSynchronizationInitialization(state, calls); var result = run.Run();
            Need(result.ReturnedNormally && result.Path == "Event" && result.ReturnValue == 0 && OriginalEncoded(state) &&
                state.EventSlot == 0xEE && state.Stores.Length == 1 && state.Stores[0].InstructionRva == 0x2DCA02 &&
                !state.ConditionCallReturned && publishedAtCleanup, "Missing pointer selects event without touching encoded slots: " + missing);
            Need(Sequence(run, ActorSyncInitOperation.InitializeCriticalSection, ActorSyncInitOperation.GetModule,
                ActorSyncInitOperation.GetInitializeCallback, ActorSyncInitOperation.GetWaitCallback,
                ActorSyncInitOperation.GetWakeCallback, ActorSyncInitOperation.CreateEvent, ActorSyncInitOperation.RegisterCleanup) &&
                Args(run.Trace[5], 0, 1, 0, 0), "All three queries precede fallback and no condition call occurs: " + missing);
        }
    }
    static void FatalAndWriteOrder()
    {
        var state = State(1); var calls = new Calls(); calls.ModuleValue = 0;
        var run = new ActorSynchronizationInitialization(state, calls); var result = run.Run();
        Need(Stop(result, "FatalHandlerReturnedToInt3", 0x2DCA1A) && state.Stores.Length == 0 &&
            Sequence(run, ActorSyncInitOperation.InitializeCriticalSection, ActorSyncInitOperation.GetModule, ActorSyncInitOperation.FatalHandler) &&
            Args(run.Trace[2], 7), "Missing module calls fatal and a returning fatal reaches INT3 stop");
        calls = new Calls(); calls.ModuleValue = 0; calls.Unresolved = ActorSyncInitOperation.FatalHandler;
        state = State(1); result = new ActorSynchronizationInitialization(state, calls).Run();
        Need(Stop(result, "UnresolvedExternalReturn", 0x2DCA15) && state.Stores.Length == 0, "Module fatal nonreturn remains at call");
        calls = new Calls(); calls.InitializeValue = 0; calls.EventValue = 0; state = State(1);
        bool zeroPublished = false;
        calls.Before = delegate(ActorSyncInitCall call, ActorSyncInitializationState s)
        { if (call.Operation == ActorSyncInitOperation.FatalHandler) zeroPublished = s.EventSlot == 0 && s.Stores.Length == 1; };
        run = new ActorSynchronizationInitialization(state, calls); result = run.Run();
        Need(Stop(result, "FatalHandlerReturnedToInt3", 0x2DCA25) && zeroPublished && OriginalEncoded(state), "Zero event is published before fatal check");
        Need(run.Trace.Length == 7 && run.Trace[6].Rva == 0x2DCA20 && Args(run.Trace[6], 7) &&
            state.Stores[0].InstructionRva == 0x2DCA02, "Event failure does not register cleanup or roll back publication");
        calls = new Calls(); calls.InitializeValue = 0; calls.EventValue = 0; calls.Unresolved = ActorSyncInitOperation.FatalHandler;
        state = State(1); result = new ActorSynchronizationInitialization(state, calls).Run();
        Need(Stop(result, "UnresolvedExternalReturn", 0x2DCA20) && state.EventSlot == 0 && state.Stores.Length == 1,
            "Unresolved event fatal retains published zero");
        calls = new Calls(); calls.ModuleValue = null; state = State(1);
        run = new ActorSynchronizationInitialization(state, calls); result = run.Run();
        Need(Stop(result, "UnknownModuleResult", 0x2DC93B) && state.CriticalSectionCallReturned && run.Trace.Length == 2 && state.Stores.Length == 0,
            "Normally returned unknown module is not fabricated zero");
        calls = new Calls(); calls.Unresolved = ActorSyncInitOperation.InitializeCriticalSection; state = State(1);
        run = new ActorSynchronizationInitialization(state, calls); result = run.Run();
        Need(Stop(result, "UnresolvedExternalReturn", 0x2DC926) && !state.CriticalSectionCallReturned && run.Trace.Length == 1 && state.Stores.Length == 0,
            "Critical nonreturn prevents every later operation");
    }
    static void UnknownValueVsReturnPrefixes()
    {
        for (int unknown = 0; unknown < 3; unknown++)
        {
            var state = State(1); var calls = new Calls();
            if (unknown == 0) calls.InitializeValue = null;
            if (unknown == 1) calls.WaitValue = null;
            if (unknown == 2) calls.WakeValue = null;
            string[] reasons = { "UnknownInitializeCallback", "UnknownWaitCallback", "UnknownWakeCallback" };
            uint[] sites = { 0x2DC97D, 0x2DC982, 0x2DC987 };
            var run = new ActorSynchronizationInitialization(state, calls); var result = run.Run();
            Need(Stop(result, reasons[unknown], sites[unknown]) && run.Trace.Length == 5 && state.Stores.Length == 0,
                "Returned unknown lookup is checked only after all three queries: " + unknown);
        }
        var s = State(1); var c = new Calls(); c.InitializeValue = 0; c.Unresolved = ActorSyncInitOperation.GetWakeCallback;
        var r = new ActorSynchronizationInitialization(s, c).Run();
        Need(Stop(r, "UnresolvedExternalReturn", 0x2DC974) && s.Stores.Length == 0 && s.EventSlot == OldEvent,
            "Known earlier zero cannot bypass a later query that never returns");
        s = State(1); c = new Calls(); c.InitializeValue = 0; c.EventValue = null;
        r = new ActorSynchronizationInitialization(s, c).Run();
        Need(Stop(r, "UnknownEventResult", 0x2DCA09) && !s.EventSlot.HasValue && s.Stores.Length == 1 && OriginalEncoded(s),
            "Normally returned unknown event is written before its test");
        s = State(1); c = new Calls(); c.InitializeValue = 0; c.Unresolved = ActorSyncInitOperation.CreateEvent;
        r = new ActorSynchronizationInitialization(s, c).Run();
        Need(Stop(r, "UnresolvedExternalReturn", 0x2DC9FC) && s.EventSlot == OldEvent && s.Stores.Length == 0,
            "Event nonreturn does not reach the slot write");
        s = State(1); c = new Calls(); c.NullObservation = ActorSyncInitOperation.CheckIndirectTarget;
        var runGuard = new ActorSynchronizationInitialization(s, c); r = runGuard.Run();
        Need(Stop(r, "UnresolvedExternalReturn", 0x2DC997) && s.EventSlot == 0 && s.Stores.Length == 1 &&
            OriginalEncoded(s) && runGuard.Trace.Length == 6 && !s.ConditionCallReturned, "Missing guard observation retains event clear");
        s = State(1); c = new Calls(); c.Unresolved = ActorSyncInitOperation.InitializeCondition;
        r = new ActorSynchronizationInitialization(s, c).Run();
        Need(Stop(r, "UnresolvedExternalReturn", 0x2DC9A3) && s.EventSlot == 0 && s.Stores.Length == 1 &&
            OriginalEncoded(s) && !s.ConditionCallReturned, "Condition nonreturn cannot publish encodings");
        s = State(1); c = new Calls();
        c.Before = delegate(ActorSyncInitCall call, ActorSyncInitializationState state)
        { if (call.Operation == ActorSyncInitOperation.InitializeCondition) state.Cookie = null; };
        r = new ActorSynchronizationInitialization(s, c).Run();
        Need(Stop(r, "UnknownCookieAfterCallback", 0x2DC9A5) && s.ConditionCallReturned && s.EventSlot == 0 &&
            s.Stores.Length == 1 && OriginalEncoded(s), "Unknown post-callback cookie preserves clear and callback prefix");
    }
    static void LateUnknownShortCircuit()
    {
        var state = State(null); var calls = new Calls(); calls.InitializeValue = 0; calls.WaitValue = null; calls.WakeValue = null;
        var run = new ActorSynchronizationInitialization(state, calls); var result = run.Run();
        Need(result.ReturnedNormally && result.Path == "Event" && run.Trace.Length == 7 && OriginalEncoded(state),
            "Known initialize zero bypasses later unknown values after all queries returned");
        state = State(null); calls = new Calls(); calls.WaitValue = 0; calls.WakeValue = null;
        result = new ActorSynchronizationInitialization(state, calls).Run();
        Need(result.ReturnedNormally && result.Path == "Event" && OriginalEncoded(state), "Known wait zero bypasses unknown wake value");
        state = State(1); calls = new Calls(); calls.InitializeValue = null; calls.WaitValue = 0;
        result = new ActorSynchronizationInitialization(state, calls).Run();
        Need(Stop(result, "UnknownInitializeCallback", 0x2DC97D) && state.Stores.Length == 0,
            "Later known zero cannot bypass earlier unknown initialize test");
        state = State(0); calls = new Calls(); calls.Critical = null; calls.CleanupValue = null;
        result = new ActorSynchronizationInitialization(state, calls).Run();
        Need(result.ReturnedNormally && state.CriticalSectionCallReturned && state.ConditionCallReturned && state.Stores.Length == 3,
            "Ignored normally returned values may be unknown without inventing nonreturn");
        state = State(0); calls = new Calls(); calls.Unresolved = ActorSyncInitOperation.RegisterCleanup;
        result = new ActorSynchronizationInitialization(state, calls).Run();
        Need(Stop(result, "UnresolvedExternalReturn", 0x2DC9D9) && state.Stores.Length == 3 &&
            state.EncodedWait == Wait && state.EncodedWake == Wake, "Cleanup nonreturn preserves fully published condition slots");
    }
    static void NoReplayAndOwnExceptions()
    {
        var state = State(0); var calls = new Calls(); var run = new ActorSynchronizationInitialization(state, calls);
        run.Run(); int count = calls.Count;
        Need(Throws<InvalidOperationException>(delegate { run.Run(); }) && calls.Count == count && state.Stores.Length == 3,
            "Successful attempt cannot be replayed");
        state = State(0); calls = new Calls(); calls.ModuleValue = null; run = new ActorSynchronizationInitialization(state, calls);
        run.Run(); count = calls.Count; calls.ModuleValue = Module;
        Need(Throws<InvalidOperationException>(delegate { run.Run(); }) && calls.Count == count && state.Stores.Length == 0,
            "Fixing observations does not replay a stopped attempt");
        state = State(0); calls = new Calls(); var marker = new Exception("Own injected callback fault");
        calls.Before = delegate(ActorSyncInitCall call, ActorSyncInitializationState s)
        { if (call.Operation == ActorSyncInitOperation.InitializeCondition) throw marker; };
        run = new ActorSynchronizationInitialization(state, calls); Exception caught = null;
        try { run.Run(); } catch (Exception e) { caught = e; }
        Need(Object.ReferenceEquals(caught, marker) && state.EventSlot == 0 && state.Stores.Length == 1 &&
            OriginalEncoded(state) && !state.ConditionCallReturned, "Own exception propagates with the completed clear prefix");
        count = calls.Count;
        Need(Throws<InvalidOperationException>(delegate { run.Run(); }) && calls.Count == count && run.Trace.Length == 7,
            "Thrown attempt also cannot be replayed");
        state = State(0); calls = new Calls(); new ActorSynchronizationInitialization(state, calls).Run();
        var second = new ActorSynchronizationInitialization(state, calls).Run();
        Need(second.ReturnedNormally && calls.Count == 16 && state.Stores.Length == 6,
            "A distinct own invocation is not globally deduplicated");
    }
    static void SnapshotIsolation()
    {
        var fresh = ActorSyncInitializationState.CreateOwnStorage(63);
        Need(fresh.Cookie == 63 && fresh.EventSlot == 0 && fresh.EncodedWait == 0 && fresh.EncodedWake == 0 &&
            fresh.Provenance == "OwnZeroedSlotsWithExplicitCookie" && fresh.Stores.Length == 0 &&
            !fresh.CriticalSectionCallReturned && !fresh.ConditionCallReturned,
            "Own storage factory preserves explicit cookie and records no native initialization");
        ulong[] supplied = { 3, 4 }; var call = new ActorSyncInitCall(ActorSyncInitOperation.GetModule, 7, supplied); supplied[0] = 99;
        Need(call.Arguments[0] == 3, "Call input argument array is copied");
        ulong[] copied = call.Arguments; copied[1] = 88;
        Need(call.Arguments[1] == 4, "Call output argument array is copied");
        var state = State(0); var calls = new Calls(); var run = new ActorSynchronizationInitialization(state, calls); run.Run();
        ActorSyncInitCall[] trace = run.Trace; trace[0] = null;
        Need(run.Trace.Length == 8 && run.Trace[0].Operation == ActorSyncInitOperation.InitializeCriticalSection,
            "Trace array mutation cannot remove a recorded call");
        ActorSyncSlotStore[] stores = state.Stores; stores[0] = null;
        Need(state.Stores.Length == 3 && state.Stores[0].Value == 0 && state.EventSlot == 0,
            "Store history array mutation cannot alter storage or its history");
        var independent = State(63); state.Cookie = 7;
        Need(independent.Cookie == 63 && independent.EventSlot == OldEvent && OriginalEncoded(independent) && independent.Stores.Length == 0,
            "Separate storage retains its own supplied observations");
        Need(Throws<ArgumentNullException>(delegate { new ActorSynchronizationInitialization(null, calls); }) &&
            Throws<ArgumentNullException>(delegate { new ActorSynchronizationInitialization(state, null); }),
            "Own missing dependencies are constructor guards, not native return behavior");
    }
    public static int Main()
    {
        try
        {
            Group("ConditionOrderAndCookie", ConditionOrderAndCookie);
            Group("EncodingVectors", EncodingVectors);
            Group("FallbackEachMissingPointer", FallbackEachMissingPointer);
            Group("FatalAndWriteOrder", FatalAndWriteOrder);
            Group("UnknownValueVsReturnPrefixes", UnknownValueVsReturnPrefixes);
            Group("LateUnknownShortCircuit", LateUnknownShortCircuit);
            Group("NoReplayAndOwnExceptions", NoReplayAndOwnExceptions);
            Group("SnapshotIsolation", SnapshotIsolation);
            Console.WriteLine("PASS: " + assertions + " assertions, " + groups + " scenario groups");
            return 0;
        }
        catch (Exception e) { Console.Error.WriteLine("FAIL: " + e); return 1; }
    }
}
