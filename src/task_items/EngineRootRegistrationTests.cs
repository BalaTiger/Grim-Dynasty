using System;
using System.Collections.Generic;
using GrimDynasty.TaskItems;

// Own, bounded fixtures only. No native address is dereferenced or invoked.
// This executable does not call an older suite. Compilation/execution belongs
// to the separately approved root task, not preparation of this source file.
class EngineRootRegistrationTests
{
    static int assertions, scenarios;
    static readonly EngineModelLimits Limits = new EngineModelLimits(128, 8);

    static void Need(bool value, string why)
    {
        assertions++;
        if (!value) throw new Exception(why);
    }

    static EngineDependencyResult<T> Normal<T>(T value)
    {
        return EngineDependencyResult<T>.Normal(value);
    }

    static EngineRootChildStorage Storage(params EngineRootNode[] nodes)
    {
        return new EngineRootChildStorage(nodes);
    }

    static EngineRootNode Node(EngineRootNode parent, int count, EngineRootChildStorage children)
    {
        return new EngineRootNode(new object(), parent, count, children, new object());
    }

    static EngineExitState State()
    {
        return new EngineExitState(new object(), 0UL, ulong.MaxValue);
    }

    static EngineRootDependencies Basic()
    {
        return new EngineRootDependencies {
            DuplicateName = delegate(ulong source) { return Normal<object>(new object()); },
            Malloc = delegate(ulong size) {
                // An own-fixture guard: never allocate from an arbitrary native-sized request.
                if (size > 64 || size % 8 != 0) throw new Exception("fixture allocation exceeds eight slots");
                return Normal(Storage(new EngineRootNode[(int)(size / 8)]));
            },
            NewHandler = delegate(ulong size) { throw new Exception("unexpected handler in successful fixture"); },
            Throw = delegate(EngineThrowRequest request) { throw new Exception("unexpected throw in successful fixture"); },
            Free = delegate(EngineRootChildStorage old) { return Normal(EngineUnit.Value); },
            CrtAtExit = delegate(ulong token) { return Normal(0); },
            RegisterOnExit = delegate(EngineExitState state, ulong token) { return Normal(0); }
        };
    }

    static int EventIndex(EngineRootResult result, string kind)
    {
        int index = 0;
        foreach (var item in result.Trace) {
            if (String.Equals(Convert.ToString(item.Kind), kind, StringComparison.Ordinal)) return index;
            index++;
        }
        return -1;
    }

    static void Before(EngineRootResult result, string first, string second, string why)
    {
        int a = EventIndex(result, first), b = EventIndex(result, second);
        Need(a >= 0 && b > a, why);
    }

    static void WriterIdentitiesAndNullParents()
    {
        EngineRootWriter[] writers = { EngineRootWriter.Actor, EngineRootWriter.Entity, EngineRootWriter.Object };
        ulong[] writerRvas = { 0x5c70UL, 0x61f0UL, 0x7270UL };
        ulong[] nodeRvas = { 0x41cfc0UL, 0x41cf30UL, 0x41cf00UL };
        ulong[] sources = { 0x180309e64UL, 0x180309ac4UL, 0x180309aecUL };
        ulong[] callbacks = { 0x180300f90UL, 0x180301110UL, 0x180301590UL };
        for (int i = 0; i < writers.Length; i++) {
            var spec = EngineRootRegistration.GetWriterSpec(writers[i]);
            Need(spec.WriterRva == writerRvas[i] && spec.NodeRva == nodeRvas[i], "writer and record identities are fixed evidence");
            Need(spec.NameSourceToken == sources[i] && spec.ExitCallbackToken == callbacks[i], "source and callback tokens do not infer string content");
            var existing = Node(null, 0, null);
            var ownChildren = Storage(existing, null);
            var node = Node(null, 2, ownChildren);
            object factory = node.FactoryIdentity, name = new object();
            ulong seenSource = 0, seenCallback = 0;
            int mallocs = 0, frees = 0, exits = 0;
            var deps = Basic();
            deps.DuplicateName = delegate(ulong source) { seenSource = source; return Normal(name); };
            deps.Malloc = delegate(ulong size) { mallocs++; return Normal<EngineRootChildStorage>(null); };
            deps.Free = delegate(EngineRootChildStorage old) { frees++; return Normal(EngineUnit.Value); };
            deps.CrtAtExit = delegate(ulong token) { exits++; seenCallback = token; return Normal(0); };
            var result = EngineRootRegistration.Apply(writers[i], node, deps, State(), Limits);
            Need(result.Status == EngineModelStatus.Completed && result.ReturnValue == 0, "null-parent writer completes through exit registration");
            Need(seenSource == sources[i] && seenCallback == callbacks[i] && exits == 1, "each writer passes its own fixed input tokens");
            Need(mallocs == 0 && frees == 0 && result.SelectedParent == null, "null parent skips allocation and free");
            Need(Object.ReferenceEquals(node.Name, name) && result.NameStored, "returned name identity is published");
            Need(node.Parent == null && node.Count == 2 && Object.ReferenceEquals(node.Children, ownChildren) &&
                Object.ReferenceEquals(node.FactoryIdentity, factory), "writer preserves its own parent, children and factory");
            Before(result, "DuplicateNameCall", "ParentRead", "parent selection follows string call");
            Before(result, "ParentRead", "NameStore", "parent load precedes name publication");
        }
        scenarios++;
    }

    static void StringResultsAndParentSelection()
    {
        var original = Node(null, 0, null);
        var alternate = Node(null, 0, null);
        var node = Node(original, 0, null);
        var allocated = Storage((EngineRootNode)null);
        var deps = Basic();
        bool allocationSawNullName = false, freeCalledWithNull = false;
        deps.DuplicateName = delegate(ulong source) { node.Parent = alternate; return Normal<object>(null); };
        deps.Malloc = delegate(ulong size) {
            allocationSawNullName = node.Name == null && size == 8;
            return Normal(allocated);
        };
        deps.Free = delegate(EngineRootChildStorage old) { freeCalledWithNull = old == null; return Normal(EngineUnit.Value); };
        var result = EngineRootRegistration.Apply(EngineRootWriter.Actor, node, deps, State(), Limits);
        Need(result.Status == EngineModelStatus.Completed && result.NameStored && node.Name == null, "strdup null is stored rather than rejected");
        Need(allocationSawNullName && Object.ReferenceEquals(result.SelectedParent, alternate), "post-string parent and null name are visible to allocation");
        Need(original.Count == 0 && original.Children == null && alternate.Count == 1 &&
            Object.ReferenceEquals(alternate.Children.Slots[0], node), "only the parent selected after string return receives the append");
        Need(freeCalledWithNull && result.FreeInvoked && result.FreeCompleted, "free is invoked even for a null old-storage identity");

        var stopped = Node(original, 0, null);
        object oldName = stopped.Name;
        var stoppedDeps = Basic();
        stoppedDeps.DuplicateName = delegate(ulong source) {
            stopped.Parent = alternate;
            return EngineDependencyResult<object>.Nonlocal("own string exit");
        };
        var stoppedResult = EngineRootRegistration.Apply(EngineRootWriter.Entity, stopped, stoppedDeps, State(), Limits);
        Need(stoppedResult.Status == EngineModelStatus.NonlocalExit && !stoppedResult.NameStored, "nonlocal string outcome precedes name store");
        Need(Object.ReferenceEquals(stopped.Name, oldName) && Object.ReferenceEquals(stopped.Parent, alternate), "string dependency effects survive without invented writer stores");
        Need(stoppedResult.MallocCalls == 0 && !stoppedResult.ExitInvoked && EventIndex(stoppedResult, "ParentRead") < 0, "no later writer operation follows nonlocal string exit");
        scenarios++;
    }

    static void CopyPublicationRepeatAndExplicitChain()
    {
        var prior = Node(null, 0, null);
        var old = Storage(prior, null, prior);
        var parent = Node(null, 3, old);
        var ownChildren = Storage(prior);
        var node = Node(parent, 1, ownChildren);
        object factory = node.FactoryIdentity;
        var allocated = Storage(null, null, null, null);
        var calls = new List<string>();
        bool freeSawPrefix = false, exitSawPublication = false;
        var deps = Basic();
        deps.DuplicateName = delegate(ulong source) { calls.Add("name"); return Normal<object>(new object()); };
        deps.Malloc = delegate(ulong size) { calls.Add("malloc"); Need(size == 32, "count three requests exactly thirty-two bytes"); return Normal(allocated); };
        deps.Free = delegate(EngineRootChildStorage storage) {
            calls.Add("free");
            freeSawPrefix = Object.ReferenceEquals(storage, old) && parent.Count == 4 &&
                Object.ReferenceEquals(parent.Children, old) && Object.ReferenceEquals(allocated.Slots[3], node);
            return Normal(EngineUnit.Value);
        };
        deps.CrtAtExit = delegate(ulong token) {
            calls.Add("exit"); exitSawPublication = Object.ReferenceEquals(parent.Children, allocated); return Normal(0);
        };
        var result = EngineRootRegistration.Apply(EngineRootWriter.Actor, node, deps, State(), Limits);
        Need(result.Status == EngineModelStatus.Completed && result.CopiedChildren == 3, "normal parent copies its three slots");
        Need(Object.ReferenceEquals(allocated.Slots[0], prior) && allocated.Slots[1] == null &&
            Object.ReferenceEquals(allocated.Slots[2], prior) && Object.ReferenceEquals(allocated.Slots[3], node), "copy preserves order, null slots and duplicates before appending self");
        Need(freeSawPrefix && exitSawPublication && String.Join(",", calls.ToArray()) == "name,malloc,free,exit", "callbacks independently observe required publication order");
        Need(result.ChildAppended && result.CountIncremented && result.FreeCompleted && result.StoragePublished, "completed writer records its publication prefix");
        Need(node.Count == 1 && Object.ReferenceEquals(node.Children, ownChildren) && Object.ReferenceEquals(node.FactoryIdentity, factory), "parent registration does not reset own children or factory");
        Before(result, "AppendChild", "CountStore", "append precedes count increment");
        Before(result, "CountStore", "FreeCall", "count increment precedes free");
        Before(result, "FreeCall", "StoragePublish", "free precedes new storage publication");

        var repeatParent = Node(null, 0, null);
        var repeated = Node(repeatParent, 0, null);
        var repeatedDeps = Basic();
        var first = EngineRootRegistration.Apply(EngineRootWriter.Entity, repeated, repeatedDeps, State(), Limits);
        var firstStorage = repeatParent.Children;
        object firstName = repeated.Name;
        var second = EngineRootRegistration.Apply(EngineRootWriter.Entity, repeated, repeatedDeps, State(), Limits);
        Need(first.Status == EngineModelStatus.Completed && second.Status == EngineModelStatus.Completed && repeatParent.Count == 2, "repeat writer appends instead of deduplicating");
        Need(Object.ReferenceEquals(repeatParent.Children.Slots[0], repeated) && Object.ReferenceEquals(repeatParent.Children.Slots[1], repeated), "repeat preserves identical child identities");
        Need(Object.ReferenceEquals(second.FreeArgument, firstStorage) && !Object.ReferenceEquals(repeated.Name, firstName), "repeat replaces storage and returned name without a run-once shortcut");

        // An explicitly supplied own chain, not a claim about real CRT iteration.
        var root = Node(null, 0, null);
        var entity = Node(root, 0, null);
        var actor = Node(entity, 0, null);
        var chainDeps = Basic();
        var ar = EngineRootRegistration.Apply(EngineRootWriter.Actor, actor, chainDeps, State(), Limits);
        var er = EngineRootRegistration.Apply(EngineRootWriter.Entity, entity, chainDeps, State(), Limits);
        var objectResult = EngineRootRegistration.Apply(EngineRootWriter.Object, root, chainDeps, State(), Limits);
        Need(ar.Status == EngineModelStatus.Completed && er.Status == EngineModelStatus.Completed && objectResult.Status == EngineModelStatus.Completed, "three explicit own invocations complete");
        Need(entity.Count == 1 && Object.ReferenceEquals(entity.Children.Slots[0], actor) &&
            root.Count == 1 && Object.ReferenceEquals(root.Children.Slots[0], entity), "later own writers preserve previously registered children");
        Need(Object.ReferenceEquals(actor.Parent, entity) && Object.ReferenceEquals(entity.Parent, root) && root.Parent == null, "no root writer invents or resets parent links");
        scenarios++;
    }

    static void AllocationArithmeticGoldens()
    {
        int[] counts = { 0, 1, 0x7ffffffe, int.MaxValue, int.MinValue, -2, -1 };
        ulong[] expected = { 8UL, 16UL, 0x00000003fffffff8UL, ulong.MaxValue, ulong.MaxValue, ulong.MaxValue, 0UL };
        for (int i = 0; i < counts.Length; i++)
            Need(EngineRootRegistration.AllocationSize(counts[i]) == expected[i], "independent raw-count size golden " + i);
        // These pure values are not requests to allocate enormous arrays or
        // assertions that negative-count native memory accesses are supported.
        scenarios++;
    }

    static void AllocationRetryContracts()
    {
        var block = Storage((EngineRootNode)null);
        var deps = Basic(); int mallocs = 0, handlers = 0, throws = 0;
        deps.Malloc = delegate(ulong size) { mallocs++; Need(size == 8, "immediate allocation preserves size"); return Normal(block); };
        deps.NewHandler = delegate(ulong size) { handlers++; return Normal(1); };
        deps.Throw = delegate(EngineThrowRequest request) { throws++; return Normal(EngineUnit.Value); };
        var first = EngineRootRegistration.Allocate(8, deps, Limits);
        Need(first.Status == EngineModelStatus.Completed && Object.ReferenceEquals(first.AllocatedStorage, block), "nonzero allocation returns immediately");
        Need(mallocs == 1 && handlers == 0 && throws == 0 && first.MallocCalls == 1, "first success skips handler and throw");

        int[] rawHandler = { 1, 0x100, int.MinValue, -1 };
        for (int i = 0; i < rawHandler.Length; i++) {
            int raw = rawHandler[i], attempts = 0, handlerCalls = 0;
            var retried = Basic(); var requests = new List<ulong>();
            retried.Malloc = delegate(ulong size) { requests.Add(size); attempts++; return Normal(attempts == 1 ? null : block); };
            retried.NewHandler = delegate(ulong size) { requests.Add(size); handlerCalls++; return Normal(raw); };
            var result = EngineRootRegistration.Allocate(24, retried, Limits);
            Need(result.Status == EngineModelStatus.Completed && attempts == 2 && handlerCalls == 1, "every nonzero raw EAX retries, including sign bit and minus one");
            Need(requests.Count == 3 && requests[0] == 24 && requests[1] == 24 && requests[2] == 24, "handler and retry preserve the original complete request");
            Need(result.MallocCalls == 2 && result.NewHandlerCalls == 1 && Object.ReferenceEquals(result.AllocatedStorage, block), "allocation journal retains retry counts and returned identity");
        }

        var zeroDeps = Basic(); ulong zeroSeen = ulong.MaxValue;
        zeroDeps.Malloc = delegate(ulong size) { zeroSeen = size; return Normal(Storage()); };
        var zero = EngineRootRegistration.Allocate(0, zeroDeps, Limits);
        Need(zero.Status == EngineModelStatus.Completed && zeroSeen == 0 && zero.AllocatedStorage.Slots.Length == 0, "zero request is not silently normalized to a positive size");
        scenarios++;
    }

    static void AllocationMutationAndPartialCopy()
    {
        var x = Node(null, 0, null); var y = Node(null, 0, null);
        var old = Storage(x, y); var current = Storage(y);
        var selected = Node(null, 2, old); var alternate = Node(null, 0, null);
        var node = Node(selected, 0, null); var allocated = Storage(null, null, null);
        int attempts = 0; var sizes = new List<ulong>();
        var deps = Basic();
        deps.Malloc = delegate(ulong size) { sizes.Add(size); attempts++; return Normal(attempts == 1 ? null : allocated); };
        deps.NewHandler = delegate(ulong size) {
            sizes.Add(size); selected.Count = 1; selected.Children = current; node.Parent = alternate; return Normal(0x100);
        };
        var result = EngineRootRegistration.Apply(EngineRootWriter.Actor, node, deps, State(), Limits);
        Need(result.Status == EngineModelStatus.Completed && result.RequestedBytes == 24 && result.AllocationCount == 2, "request retains pre-handler count even after mutation");
        Need(sizes.Count == 3 && sizes[0] == 24 && sizes[1] == 24 && sizes[2] == 24, "mutating handler does not recalculate retry size");
        Need(Object.ReferenceEquals(result.SelectedParent, selected) && Object.ReferenceEquals(node.Parent, alternate), "selected parent identity is held while node parent may change");
        Need(selected.Count == 2 && alternate.Count == 0 && alternate.Children == null && result.CopiedChildren == 1, "post-allocation count is reread on the originally selected parent");
        Need(Object.ReferenceEquals(allocated.Slots[0], y) && Object.ReferenceEquals(allocated.Slots[1], node) && allocated.Slots[2] == null &&
            Object.ReferenceEquals(result.FreeArgument, current), "copy and free use updated selected-parent storage");

        var expandedOld = Storage(x, y, x); var shortNew = Storage(null, null);
        var grows = Node(null, 1, Storage(x)); var child = Node(grows, 0, null);
        var growDeps = Basic();
        growDeps.Malloc = delegate(ulong size) { grows.Count = 3; grows.Children = expandedOld; return Normal(shortNew); };
        var grow = EngineRootRegistration.Apply(EngineRootWriter.Entity, child, growDeps, State(), Limits);
        Need(grow.Status == EngineModelStatus.Unsupported && grow.NameStored && !grow.StoragePublished, "capacity mismatch stops as unsupported without publication");
        Need(grow.CopiedChildren == 2 && Object.ReferenceEquals(shortNew.Slots[0], x) && Object.ReferenceEquals(shortNew.Slots[1], y), "insufficient destination retains the completed copy prefix");
        Need(grows.Count == 3 && Object.ReferenceEquals(grows.Children, expandedOld) && !grow.ChildAppended && !grow.FreeInvoked, "model does not resize, append, free or roll back dependency mutations");

        var shortOld = Storage(x); var destination = Storage(null, null, null);
        var badParent = Node(null, 2, shortOld); var badChild = Node(badParent, 0, null);
        var shortDeps = Basic(); shortDeps.Malloc = delegate(ulong size) { return Normal(destination); };
        var shortRead = EngineRootRegistration.Apply(EngineRootWriter.Actor, badChild, shortDeps, State(), Limits);
        Need(shortRead.Status == EngineModelStatus.Unsupported && shortRead.CopiedChildren == 1 && Object.ReferenceEquals(destination.Slots[0], x), "insufficient source retains the first copied child");
        Need(!shortRead.CountIncremented && !shortRead.StoragePublished && badParent.Count == 2, "source failure cannot be promoted to a completed empty copy");
        scenarios++;
    }

    static void ThrowWrappersAndUnexpectedReturn()
    {
        ulong[] requests = { 0UL, ulong.MaxValue - 1, ulong.MaxValue };
        ulong[] wrappers = { 0x2dd5e0UL, 0x2dd5e0UL, 0x2dd600UL };
        ulong[] constructors = { 0x2dd508UL, 0x2dd508UL, 0x2dd568UL };
        ulong[] descriptors = { 0x1803af558UL, 0x1803af558UL, 0x1803af5b8UL };
        ulong[] messages = { 0x180307488UL, 0x180307488UL, 0x1803074b0UL };
        ulong[] tables = { 0x180307478UL, 0x180307478UL, 0x1803074a0UL };
        for (int i = 0; i < requests.Length; i++) {
            ulong requested = requests[i]; int mallocs = 0, handlers = 0, throws = 0;
            EngineThrowRequest observed = null; var deps = Basic();
            deps.Malloc = delegate(ulong size) { mallocs++; Need(size == requested, "failure malloc receives complete size"); return Normal<EngineRootChildStorage>(null); };
            deps.NewHandler = delegate(ulong size) { handlers++; Need(size == requested, "failure handler receives complete size"); return Normal(0); };
            deps.Throw = delegate(EngineThrowRequest request) { throws++; observed = request; return EngineDependencyResult<EngineUnit>.Nonlocal("own throw dispatch exit"); };
            var result = EngineRootRegistration.Allocate(requested, deps, Limits);
            Need(result.Status == EngineModelStatus.NonlocalExit && mallocs == 1 && handlers == 1 && throws == 1, "handler zero selects a throw outcome without a normal null return");
            Need(observed != null && Object.ReferenceEquals(observed, result.ThrowRequest) && observed.WrapperRva == wrappers[i] &&
                observed.ConstructorRva == constructors[i] && observed.DescriptorToken == descriptors[i], "only the exact all-ones request selects the second wrapper");
            int index = 0; int[] offsets = { 0x10, 0x08, 0x00 }; ulong[] values = { 0UL, messages[i], tables[i] };
            foreach (var write in observed.Writes) {
                Need(index < offsets.Length && write.Offset == offsets[index] && write.Value == values[index], "constructor field order and opaque address are fixed evidence");
                index++;
            }
            Need(index == 3 && result.ThrowFieldsStored == 3 && result.AllocatedStorage == null, "three observed writes do not invent a concrete native exception type");
        }
        var returnedDeps = Basic(); int returnAttempts = 0;
        returnedDeps.Malloc = delegate(ulong size) { returnAttempts++; return Normal<EngineRootChildStorage>(null); };
        returnedDeps.NewHandler = delegate(ulong size) { return Normal(0); };
        returnedDeps.Throw = delegate(EngineThrowRequest request) { return Normal(EngineUnit.Value); };
        var returned = EngineRootRegistration.Allocate(8, returnedDeps, Limits);
        Need(returned.Status == EngineModelStatus.TrapContinuation && returnAttempts == 1 && returned.AllocatedStorage == null, "unexpected throw return stops at INT3 instead of retrying or returning null success");
        scenarios++;
    }

    static void NonlocalPrefixesAndFreeEffects()
    {
        var old = Storage((EngineRootNode)null); var parent = Node(null, 1, old); var node = Node(parent, 0, null);
        object replacementName = new object(); var allocationDeps = Basic();
        allocationDeps.DuplicateName = delegate(ulong source) { return Normal(replacementName); };
        allocationDeps.Malloc = delegate(ulong size) { return EngineDependencyResult<EngineRootChildStorage>.Nonlocal("own allocation exit"); };
        var allocation = EngineRootRegistration.Apply(EngineRootWriter.Actor, node, allocationDeps, State(), Limits);
        Need(allocation.Status == EngineModelStatus.NonlocalExit && allocation.NameStored && Object.ReferenceEquals(node.Name, replacementName), "allocation exit retains the earlier name publication");
        Need(parent.Count == 1 && Object.ReferenceEquals(parent.Children, old) && !allocation.ChildAppended && !allocation.ExitInvoked, "allocation exit does not invent later writer effects");

        var handlerParent = Node(null, 0, null); var handlerNode = Node(handlerParent, 0, null); var handlerDeps = Basic();
        handlerDeps.Malloc = delegate(ulong size) { return Normal<EngineRootChildStorage>(null); };
        handlerDeps.NewHandler = delegate(ulong size) { handlerParent.Count = 7; return EngineDependencyResult<int>.Nonlocal("own handler exit"); };
        var handler = EngineRootRegistration.Apply(EngineRootWriter.Entity, handlerNode, handlerDeps, State(), Limits);
        Need(handler.Status == EngineModelStatus.NonlocalExit && handler.NameStored && handlerParent.Count == 7 && !handler.StoragePublished, "nonlocal handler preserves its explicit mutation and name prefix");

        for (int i = 0; i < 2; i++) {
            bool nonlocal = i != 0;
            var previous = Storage((EngineRootNode)null); var selected = Node(null, 1, previous);
            var self = Node(selected, 0, null); var allocated = Storage(null, null); var sideEffect = Storage();
            bool sawBeforePublication = false; var deps = Basic();
            deps.Malloc = delegate(ulong size) { return Normal(allocated); };
            deps.Free = delegate(EngineRootChildStorage argument) {
                sawBeforePublication = Object.ReferenceEquals(argument, previous) && selected.Count == 2 &&
                    Object.ReferenceEquals(selected.Children, previous) && Object.ReferenceEquals(allocated.Slots[1], self);
                selected.Children = sideEffect;
                return nonlocal ? EngineDependencyResult<EngineUnit>.Nonlocal("own free exit") : Normal(EngineUnit.Value);
            };
            var result = EngineRootRegistration.Apply(EngineRootWriter.Actor, self, deps, State(), Limits);
            Need(sawBeforePublication && result.NameStored && result.ChildAppended && result.CountIncremented && result.FreeInvoked, "free sees appended child and incremented count before publication");
            if (nonlocal) {
                Need(result.Status == EngineModelStatus.NonlocalExit && !result.FreeCompleted && !result.StoragePublished && !result.ExitInvoked, "nonlocal free stops publication and exit");
                Need(Object.ReferenceEquals(selected.Children, sideEffect) && selected.Count == 2, "nonlocal free preserves its own state change and the writer prefix");
            } else {
                Need(result.Status == EngineModelStatus.Completed && result.FreeCompleted && result.StoragePublished, "normally returning free permits publication");
                Need(Object.ReferenceEquals(selected.Children, allocated), "new storage publication overwrites the returning free dependency's temporary storage change");
            }
        }

        for (int i = 0; i < 2; i++) {
            bool nonlocal = i != 0; var p = Node(null, 0, null); var self = Node(p, 0, null);
            var state = State(); var deps = Basic(); bool sawPublished = false;
            deps.CrtAtExit = delegate(ulong token) {
                sawPublished = p.Count == 1 && Object.ReferenceEquals(p.Children.Slots[0], self);
                state.Encoded = 0x1234UL;
                return nonlocal ? EngineDependencyResult<int>.Nonlocal("own registration exit") : Normal(-1);
            };
            var result = EngineRootRegistration.Apply(EngineRootWriter.Entity, self, deps, state, Limits);
            Need(sawPublished && result.StoragePublished && result.ExitInvoked && state.Encoded == 0x1234UL, "registration observes publication and preserves its external state mutation");
            Need(p.Count == 1 && Object.ReferenceEquals(p.Children.Slots[0], self), "registration failure never rolls back published children");
            Need(nonlocal ? result.Status == EngineModelStatus.NonlocalExit :
                result.Status == EngineModelStatus.Completed && result.ReturnValue == -1, "raw registration failure is a completed minus-one result; nonlocal outcome remains distinct");
        }
        scenarios++;
    }

    static void ExitArithmeticParametersAndReturnWidth()
    {
        ulong[] cookies = { 0UL, 1UL, 1UL, 0x3fUL, 0x40UL, 0x41UL, 0x8000000000000040UL, 1UL };
        ulong[] encoded = { ulong.MaxValue, 0xfffffffffffffffeUL, ulong.MaxValue, 0x3eUL, 0x41UL, 0x40UL, 0x8123456789abcdafUL, 2UL };
        ulong[] decoded = { ulong.MaxValue, ulong.MaxValue, 0x7fffffffffffffffUL, 2UL, 1UL, 0x8000000000000000UL, 0x0123456789abcdefUL, 0x8000000000000001UL };
        const ulong token = 0x0000000100000000UL;
        for (int i = 0; i < cookies.Length; i++) {
            Need(EngineRootRegistration.DecodeSelector(cookies[i], encoded[i]) == decoded[i], "independent XOR/ROR/mask golden " + i);
            object identity = new object(); var state = new EngineExitState(identity, cookies[i], encoded[i]);
            var deps = Basic(); int crt = 0, onexit = 0; ulong seen = 0; EngineExitState seenCell = null;
            deps.CrtAtExit = delegate(ulong cb) { crt++; seen = cb; return Normal(0); };
            deps.RegisterOnExit = delegate(EngineExitState cell, ulong cb) { onexit++; seenCell = cell; seen = cb; return Normal(0); };
            var result = EngineRootRegistration.RegisterExit(token, state, deps, Limits);
            bool sentinel = decoded[i] == ulong.MaxValue;
            Need(result.Status == EngineModelStatus.Completed && result.DecodedExitValue == decoded[i] && result.UsesCrtAtExit == sentinel, "branch selection follows full decoded value");
            Need(sentinel ? crt == 1 && onexit == 0 : crt == 0 && onexit == 1 && Object.ReferenceEquals(seenCell, state) && Object.ReferenceEquals(seenCell.Identity, identity), "non-sentinel import receives the state-cell identity rather than decoded value");
            Need(seen == token && result.ReturnedCallback == token && result.ReturnValue == 0 && result.ReturnRax == 0, "high-only callback token remains nonzero through the pointer normalization");
        }
        int[] eax = { 0, 1, 0x100, int.MinValue, -1 };
        for (int branch = 0; branch < 2; branch++) {
            for (int i = 0; i < eax.Length; i++) {
                int raw = eax[i]; var deps = Basic(); int calls = 0;
                deps.CrtAtExit = delegate(ulong cb) { calls++; return Normal(raw); };
                deps.RegisterOnExit = delegate(EngineExitState cell, ulong cb) { calls++; return Normal(raw); };
                var state = new EngineExitState(new object(), 0UL, branch == 0 ? ulong.MaxValue : 0UL);
                var result = EngineRootRegistration.RegisterExit(token, state, deps, Limits);
                Need(result.Status == EngineModelStatus.Completed && calls == 1 && result.RawExitEax == raw, "each selected import returns its complete raw EAX");
                Need(result.ReturnedCallback == (raw == 0 ? token : 0UL) && result.ReturnValue == (raw == 0 ? 0 : -1) &&
                    result.ReturnRax == (raw == 0 ? 0UL : 0x00000000ffffffffUL), "all nonzero EAX values yield zero-extended 32-bit minus one");
            }
        }
        var nullDeps = Basic(); int nullCalls = 0;
        nullDeps.CrtAtExit = delegate(ulong cb) { nullCalls++; Need(cb == 0, "null helper fixture passes zero token unchanged"); return Normal(0); };
        var nullCallback = EngineRootRegistration.RegisterExit(0, State(), nullDeps, Limits);
        Need(nullCallback.Status == EngineModelStatus.Completed && nullCalls == 1 && nullCallback.ReturnedCallback == 0 &&
            nullCallback.ReturnValue == -1 && nullCallback.ReturnRax == 0xffffffffUL, "null callback still invokes external dependency and then yields minus one");
        scenarios++;
    }

    static void ExplicitUnknownFaultAndResourceOutcomes()
    {
        var missingDeps = Basic(); missingDeps.DuplicateName = null;
        var node = Node(null, 0, null); object originalName = node.Name;
        var missing = EngineRootRegistration.Apply(EngineRootWriter.Object, node, missingDeps, State(), Limits);
        Need(missing.Status == EngineModelStatus.Unknown && !missing.NameStored && Object.ReferenceEquals(node.Name, originalName), "missing string dependency does not supply invented initialization");

        var unknownDeps = Basic();
        unknownDeps.DuplicateName = delegate(ulong source) { return EngineDependencyResult<object>.Unknown("unprovided copy behavior"); };
        var unknown = EngineRootRegistration.Apply(EngineRootWriter.Actor, Node(null, 0, null), unknownDeps, State(), Limits);
        Need(unknown.Status == EngineModelStatus.Unknown && !unknown.NameStored && !String.IsNullOrEmpty(unknown.Reason), "explicit unknown preserves reason without being called success");

        var nullOutcomeDeps = Basic(); nullOutcomeDeps.Malloc = delegate(ulong size) { return null; };
        var nullOutcome = EngineRootRegistration.Allocate(8, nullOutcomeDeps, Limits);
        Need(nullOutcome.Status == EngineModelStatus.Unknown && nullOutcome.AllocatedStorage == null, "missing outcome object differs from normal null malloc");

        var ownFaultDeps = Basic(); ownFaultDeps.Malloc = delegate(ulong size) { throw new InvalidOperationException("own fixture fault"); };
        var ownFault = EngineRootRegistration.Allocate(8, ownFaultDeps, Limits);
        Need(ownFault.Status == EngineModelStatus.OwnFault && ownFault.AllocatedStorage == null, "host callback exception is an own fault, not a concrete native exception");

        var unknownStateDeps = Basic(); int exitCalls = 0;
        unknownStateDeps.CrtAtExit = delegate(ulong cb) { exitCalls++; return Normal(0); };
        unknownStateDeps.RegisterOnExit = delegate(EngineExitState cell, ulong cb) { exitCalls++; return Normal(0); };
        var unknownCookie = EngineRootRegistration.RegisterExit(1, new EngineExitState(new object(), null, 0UL), unknownStateDeps, Limits);
        var unknownEncoded = EngineRootRegistration.RegisterExit(1, new EngineExitState(new object(), 0UL, null), unknownStateDeps, Limits);
        Need(unknownCookie.Status == EngineModelStatus.Unknown && unknownEncoded.Status == EngineModelStatus.Unknown && exitCalls == 0, "missing runtime state cannot pick a disk-value branch or invoke an import");

        var unprovidedParent = Node(null, 0, null); var unprovidedChild = Node(unprovidedParent, 0, null);
        var unprovidedDeps = Basic();
        unprovidedDeps.Malloc = delegate(ulong size) { return Normal(new EngineRootChildStorage(null)); };
        var unprovided = EngineRootRegistration.Apply(EngineRootWriter.Actor, unprovidedChild, unprovidedDeps, State(), Limits);
        Need(unprovided.Status == EngineModelStatus.Unknown && unprovided.NameStored && !unprovided.ChildAppended &&
            unprovidedParent.Count == 0, "nonzero storage identity with an unprovided span is unknown rather than an empty valid allocation");

        var unknownOldParent = Node(null, 1, null); var unknownOldChild = Node(unknownOldParent, 0, null);
        var unknownOld = EngineRootRegistration.Apply(EngineRootWriter.Actor, unknownOldChild, Basic(), State(), Limits);
        Need(unknownOld.Status == EngineModelStatus.Unknown && unknownOld.NameStored && unknownOld.CopiedChildren == 0 &&
            !unknownOld.StoragePublished, "positive count with an unprovided old span is distinct from a known span that is too small");

        var existingChild = Node(null, 0, null); var aliasedOld = Storage(existingChild);
        var aliasParent = Node(null, 1, aliasedOld); var aliasChild = Node(aliasParent, 0, null);
        var aliasDeps = Basic();
        aliasDeps.Malloc = delegate(ulong size) { return Normal(new EngineRootChildStorage(aliasedOld.Slots)); };
        var alias = EngineRootRegistration.Apply(EngineRootWriter.Actor, aliasChild, aliasDeps, State(), Limits);
        Need(alias.Status == EngineModelStatus.Unsupported && alias.NameStored && alias.CopiedChildren == 0 &&
            aliasParent.Count == 1 && Object.ReferenceEquals(aliasParent.Children, aliasedOld), "own alias guard preserves prefix without pretending to be a native check");

        var slotOld = Storage(existingChild, null, existingChild); var slotNew = Storage(null, null, null, null);
        var slotParent = Node(null, 3, slotOld); var slotChild = Node(slotParent, 0, null);
        var slotDeps = Basic(); slotDeps.Malloc = delegate(ulong size) { return Normal(slotNew); };
        var slotLimit = EngineRootRegistration.Apply(EngineRootWriter.Entity, slotChild, slotDeps, State(), new EngineModelLimits(128, 2));
        Need(slotLimit.Status == EngineModelStatus.ResourceLimit && slotLimit.CopiedChildren == 2 &&
            Object.ReferenceEquals(slotNew.Slots[0], existingChild) && slotNew.Slots[1] == null, "own slot limit retains the completed bounded copy prefix");
        Need(!slotLimit.ChildAppended && !slotLimit.FreeInvoked && !slotLimit.StoragePublished &&
            slotParent.Count == 3 && Object.ReferenceEquals(slotParent.Children, slotOld), "slot-budget stop cannot be converted to a published shortened array");

        var fieldDeps = Basic(); int throwCalls = 0;
        fieldDeps.Malloc = delegate(ulong size) { return Normal<EngineRootChildStorage>(null); };
        fieldDeps.NewHandler = delegate(ulong size) { return Normal(0); };
        fieldDeps.Throw = delegate(EngineThrowRequest request) { throwCalls++; return EngineDependencyResult<EngineUnit>.Nonlocal("own throw"); };
        var fields = EngineRootRegistration.Allocate(8, fieldDeps, new EngineModelLimits(4, 8));
        Need(fields.Status == EngineModelStatus.ResourceLimit && fields.ThrowRequest != null && fields.ThrowFieldsStored == 2 && throwCalls == 0,
            "step budget distinguishes the planned throw object from its two completed field stores");
        Need(fields.MallocCalls == 1 && fields.NewHandlerCalls == 1 && fields.AllocatedStorage == null,
            "partial throw construction retains allocation and handler prefix without a native return claim");

        var retryDeps = Basic(); int allocations = 0, handlers = 0;
        retryDeps.Malloc = delegate(ulong size) {
            allocations++;
            if (allocations > 4) throw new Exception("independent fixture malloc retry ceiling");
            return Normal<EngineRootChildStorage>(null);
        };
        retryDeps.NewHandler = delegate(ulong size) {
            handlers++;
            if (handlers > 4) throw new Exception("independent fixture handler retry ceiling");
            return Normal(1);
        };
        var budget = EngineRootRegistration.Allocate(8, retryDeps, new EngineModelLimits(3, 8));
        Need(budget.Status == EngineModelStatus.ResourceLimit && budget.AllocatedStorage == null && allocations == 2 && handlers == 1 && budget.Trace.Count == 3,
            "finite own step budget stops repeating null allocation without a native null-success result");
        Need(budget.MallocCalls == allocations && budget.NewHandlerCalls == handlers && !String.IsNullOrEmpty(budget.Reason), "resource stop retains bounded dependency-call counts and reason");
        scenarios++;
    }

    static void BudgetPublicationPrefixes()
    {
        // These cut points belong to the explicit own semantic-event budget.
        // They are not native instruction limits, faults, or rollback rules.
        int[] budgets = { 7, 8, 9, 11 };
        foreach (int budget in budgets) {
            var parent = Node(null, 0, null); var node = Node(parent, 0, null);
            var allocated = Storage((EngineRootNode)null); var freeSideEffect = Storage();
            object name = new object(); var state = State(); var deps = Basic();
            int freeCalls = 0, exitCalls = 0;
            deps.DuplicateName = delegate(ulong source) { return Normal(name); };
            deps.Malloc = delegate(ulong size) { return Normal(allocated); };
            deps.Free = delegate(EngineRootChildStorage old) {
                freeCalls++; parent.Children = freeSideEffect; return Normal(EngineUnit.Value);
            };
            deps.CrtAtExit = delegate(ulong callback) {
                exitCalls++; state.Encoded = 0x55UL; return Normal(0);
            };
            var result = EngineRootRegistration.Apply(EngineRootWriter.Actor, node, deps, state, new EngineModelLimits(budget, 8));
            Need(result.Status == EngineModelStatus.ResourceLimit && result.Trace.Count == budget, "budget cut is incomplete even after a dependency normally returns");
            Need(result.NameStored && Object.ReferenceEquals(node.Name, name) && result.ChildAppended &&
                result.CountIncremented && parent.Count == 1 && Object.ReferenceEquals(allocated.Slots[0], node), "every selected cut retains name, appended child and count increment");
            Need(freeCalls == (budget >= 8 ? 1 : 0) && result.FreeInvoked == (budget >= 8) &&
                result.FreeCompleted == (budget >= 8), "free runs only once its semantic step is admitted");
            EngineRootChildStorage expected = budget == 7 ? null : budget == 8 ? freeSideEffect : allocated;
            Need(result.StoragePublished == (budget >= 9) && Object.ReferenceEquals(parent.Children, expected), "publication cut retains either original storage, free side effect, or completed new publication");
            Need(exitCalls == (budget == 11 ? 1 : 0) && result.ExitInvoked == (budget == 11), "only the final cut reaches the external exit dependency");
            Need(state.Encoded == (budget == 11 ? 0x55UL : ulong.MaxValue), "external exit state effect survives the final-result budget stop");
            Need(result.ReturnedCallback == null && result.ReturnValue == null && result.ReturnRax == null &&
                (budget == 11 ? result.RawExitEax == 0 : result.RawExitEax == null), "raw exit result may be captured without inventing a completed final return");
        }

        var missingParent = Node(null, 0, null); var missingNode = Node(missingParent, 0, null);
        var missingAllocated = Storage((EngineRootNode)null); var missingDeps = Basic();
        missingDeps.Malloc = delegate(ulong size) { return Normal(missingAllocated); };
        missingDeps.Free = null;
        var missing = EngineRootRegistration.Apply(EngineRootWriter.Entity, missingNode, missingDeps, State(), Limits);
        Need(missing.Status == EngineModelStatus.Unknown && missing.NameStored && missing.ChildAppended && missing.CountIncremented,
            "missing free dependency is unknown after the append/count prefix");
        Need(missingParent.Count == 1 && missingParent.Children == null && Object.ReferenceEquals(missingAllocated.Slots[0], missingNode),
            "missing free does not roll back the appended new allocation or publish it prematurely");
        Need(!missing.FreeInvoked && !missing.FreeCompleted && !missing.StoragePublished && !missing.ExitInvoked,
            "missing free cannot be replaced by a no-op followed by publication and exit");
        scenarios++;
    }

    public static int Main()
    {
        try {
            WriterIdentitiesAndNullParents();
            StringResultsAndParentSelection();
            CopyPublicationRepeatAndExplicitChain();
            AllocationArithmeticGoldens();
            AllocationRetryContracts();
            AllocationMutationAndPartialCopy();
            ThrowWrappersAndUnexpectedReturn();
            NonlocalPrefixesAndFreeEffects();
            ExitArithmeticParametersAndReturnWidth();
            ExplicitUnknownFaultAndResourceOutcomes();
            BudgetPublicationPrefixes();
            Console.WriteLine("PASS: " + assertions + " assertions, " + scenarios + " scenario groups");
            return 0;
        } catch (Exception error) {
            Console.Error.WriteLine("FAIL after " + assertions + " assertions, " + scenarios + " completed scenario groups: " + error);
            return 1;
        }
    }
}
