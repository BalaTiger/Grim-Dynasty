using System;
using System.Collections.Generic;
using GrimDynasty.TaskItems;

// Independent own fixtures. Never open a native file or invoke an address.
// Literal evidence: offline-engine-root-names/metadata.json, SHA256
// d882b279afe354a7e8912ca3b70afd07d300a1d278b9e268520c98cdc60e5d79.
// Capture success and own factory results are not a complete native graph.
class EngineRootNameDispatchTests
{
    static int assertions, scenarios;
    static readonly EngineSnapshotLimits Limits = new EngineSnapshotLimits(16, 32, 256, 16);
    static readonly EngineModelLimits WriterLimits = new EngineModelLimits(128, 8);

    static void Need(bool value, string why)
    {
        assertions++;
        if (!value) throw new Exception(why);
    }

    static byte[] ActorName() { return new byte[] { 0x41, 0x63, 0x74, 0x6f, 0x72, 0 }; }
    static byte[] EntityName() { return new byte[] { 0x45, 0x6e, 0x74, 0x69, 0x74, 0x79, 0 }; }
    static byte[] ObjectName() { return new byte[] { 0x4f, 0x62, 0x6a, 0x65, 0x63, 0x74, 0 }; }
    static byte[] Letter(byte value) { return new byte[] { value, 0 }; }

    static bool SameBytes(byte[] actual, byte[] expected)
    {
        if (actual == null || expected == null || actual.Length != expected.Length) return false;
        for (int i = 0; i < actual.Length; i++) if (actual[i] != expected[i]) return false;
        return true;
    }

    static EngineRootChildStorage Storage(params EngineRootNode[] nodes)
    {
        return new EngineRootChildStorage(nodes);
    }

    static EngineRootNode Node(object name, int count, EngineRootChildStorage children, object factory)
    {
        return new EngineRootNode(name, null, count, children, factory);
    }

    static EngineFactoryBinding Bind(object identity, Func<object> callback)
    {
        return new EngineFactoryBinding(identity, callback);
    }

    static EngineRootDependencies WriterDependencies()
    {
        return new EngineRootDependencies {
            DuplicateName = delegate(ulong source) {
                byte[] bytes;
                if (!EngineRootObservedNames.TryCopy(source, out bytes))
                    return EngineDependencyResult<object>.Unknown("unknown own source token");
                return EngineDependencyResult<object>.Normal(bytes);
            },
            Malloc = delegate(ulong size) {
                if (size > 64 || size % 8 != 0) throw new Exception("fixture allocation ceiling");
                return EngineDependencyResult<EngineRootChildStorage>.Normal(Storage(new EngineRootNode[(int)(size / 8)]));
            },
            NewHandler = delegate(ulong size) { throw new Exception("unexpected handler in small fixture"); },
            Throw = delegate(EngineThrowRequest request) { throw new Exception("unexpected throw in small fixture"); },
            Free = delegate(EngineRootChildStorage old) { return EngineDependencyResult<EngineUnit>.Normal(EngineUnit.Value); },
            CrtAtExit = delegate(ulong callback) { return EngineDependencyResult<int>.Normal(0); },
            RegisterOnExit = delegate(EngineExitState state, ulong callback) { return EngineDependencyResult<int>.Normal(0); }
        };
    }

    static EngineExitState ExitState()
    {
        return new EngineExitState(new object(), 0UL, ulong.MaxValue);
    }

    static void ObservedNameCopies()
    {
        ulong[] tokens = { 0x180309e64UL, 0x180309ac4UL, 0x180309aecUL };
        EngineRootWriter[] writers = { EngineRootWriter.Actor, EngineRootWriter.Entity, EngineRootWriter.Object };
        byte[][] expected = { ActorName(), EntityName(), ObjectName() };
        for (int i = 0; i < tokens.Length; i++) {
            byte[] first, second;
            Need(EngineRootObservedNames.TryCopy(tokens[i], out first) && SameBytes(first, expected[i]), "fixed token returns independently observed literal bytes");
            Need(EngineRootObservedNames.TryCopy(tokens[i], out second) && SameBytes(second, expected[i]) && !Object.ReferenceEquals(first, second), "each copy has independent storage");
            first[0] ^= 0x20;
            Need(SameBytes(EngineRootObservedNames.CopyForWriter(writers[i]), expected[i]) && SameBytes(second, expected[i]), "mutating a returned copy cannot change subsequent names");
        }
        byte[] unknown = ActorName();
        Need(!EngineRootObservedNames.TryCopy(0, out unknown) && unknown == null, "unknown data token does not guess a name");
        bool rejected = false;
        try { EngineRootObservedNames.CopyForWriter((EngineRootWriter)99); }
        catch (ArgumentOutOfRangeException) { rejected = true; }
        Need(rejected, "unknown writer identity is rejected");
        scenarios++;
    }

    static void RealNamesThroughWritersAndSnapshot()
    {
        object rootId = new object(), entityId = new object(), actorId = new object();
        object rootInstance = new object(), entityInstance = new object(), actorInstance = new object();
        var root = Node(null, 0, null, rootId);
        var entity = Node(null, 0, null, entityId); entity.Parent = root;
        var actor = Node(null, 0, null, actorId); actor.Parent = entity;
        var deps = WriterDependencies();
        // Explicit own invocation order; not a claim that CRT ran this full graph.
        var ar = EngineRootRegistration.Apply(EngineRootWriter.Actor, actor, deps, ExitState(), WriterLimits);
        var er = EngineRootRegistration.Apply(EngineRootWriter.Entity, entity, deps, ExitState(), WriterLimits);
        var rr = EngineRootRegistration.Apply(EngineRootWriter.Object, root, deps, ExitState(), WriterLimits);
        Need(ar.Status == EngineModelStatus.Completed && er.Status == EngineModelStatus.Completed && rr.Status == EngineModelStatus.Completed, "explicit own writer sequence completes");
        Need(SameBytes(actor.Name as byte[], ActorName()) && SameBytes(entity.Name as byte[], EntityName()) && SameBytes(root.Name as byte[], ObjectName()), "observed bytes pass through DuplicateName into the actual writer state");
        Need(entity.Count == 1 && Object.ReferenceEquals(entity.Children.Slots[0], actor) && root.Count == 1 &&
            Object.ReferenceEquals(root.Children.Slots[0], entity), "later own writers preserve previously appended descendants");
        int rootCalls = 0, entityCalls = 0, actorCalls = 0;
        var bindings = new EngineFactoryBinding[] {
            Bind(rootId, delegate { rootCalls++; return rootInstance; }),
            Bind(entityId, delegate { entityCalls++; return entityInstance; }),
            Bind(actorId, delegate { actorCalls++; return actorInstance; })
        };
        var captured = EngineRootSnapshot.Capture(root, bindings, Limits);
        Need(captured.Status == "Captured" && captured.Root != null && !captured.HasUnknownMetadata, "fully explicit own metadata produces a snapshot");
        Need(captured.Nodes == 3 && captured.Edges == 2 && captured.NameBytes == 20, "capture accounts for three nodes, two slots and exact NUL-terminated bytes");
        Need(rootCalls == 0 && entityCalls == 0 && actorCalls == 0, "capture never invokes any factory");
        var objectQuery = ClassFactoryDispatch.Create(captured.Root, ObjectName());
        var entityQuery = ClassFactoryDispatch.Create(captured.Root, EntityName());
        var actorQuery = ClassFactoryDispatch.Create(captured.Root, ActorName());
        Need(objectQuery.Status == "Known" && Object.ReferenceEquals(objectQuery.Instance, rootInstance) && objectQuery.VisitedNodes == 1, "Object matches the supplied root before children");
        Need(entityQuery.Status == "Known" && Object.ReferenceEquals(entityQuery.Instance, entityInstance) && entityQuery.VisitedNodes == 2, "Entity dispatch follows the first supplied child");
        Need(actorQuery.Status == "Known" && Object.ReferenceEquals(actorQuery.Instance, actorInstance) && actorQuery.VisitedNodes == 3, "Actor dispatch traverses the captured three-node chain");
        Need(rootCalls == 1 && entityCalls == 1 && actorCalls == 1, "only the matching own callbacks execute");
        var lower = ClassFactoryDispatch.Create(captured.Root, new byte[] { 0x61, 0x63, 0x74, 0x6f, 0x72, 0 });
        Need(lower.Status == "Known" && lower.Instance == null && lower.CallbacksInvoked == 0, "lowercase actor does not match observed Actor bytes");

        ((byte[])actor.Name)[0] = 0x7a;
        entity.Children.Slots[0] = null; root.Count = 0;
        bindings[2] = Bind(actorId, delegate { throw new Exception("changed binding must not be used"); });
        var afterMutation = ClassFactoryDispatch.Create(captured.Root, ActorName());
        Need(afterMutation.Status == "Known" && Object.ReferenceEquals(afterMutation.Instance, actorInstance) && actorCalls == 2,
            "captured names, child slots, count and callback binding are independent of later source mutations");
        scenarios++;
    }

    static void RawBytesAndUnknownNames()
    {
        object id = new object(), instance = new object(); int calls = 0;
        var binding = new EngineFactoryBinding[] { Bind(id, delegate { calls++; return instance; }) };
        byte[] raw = { 0x80, 0xff, 0, 0x41 };
        var node = Node(raw, 0, null, id);
        var captured = EngineRootSnapshot.Capture(node, binding, Limits);
        Need(captured.Status == "Captured" && captured.NameBytes == 4 && !captured.HasUnknownMetadata, "raw high bytes and suffix are retained without encoding conversion");
        var high = ClassFactoryDispatch.Create(captured.Root, new byte[] { 0x80, 0xff, 0, 0x42 });
        Need(high.Status == "Known" && Object.ReferenceEquals(high.Instance, instance) && calls == 1, "equal first-NUL prefix ignores trailing suffix bytes");
        raw[0] = 0x7f;
        Need(Object.ReferenceEquals(ClassFactoryDispatch.Create(captured.Root, new byte[] { 0x80, 0xff, 0 }).Instance, instance), "name bytes were cloned rather than borrowed");
        var unequal = ClassFactoryDispatch.Create(captured.Root, new byte[] { 0x80, 0xfe, 0 });
        Need(unequal.Status == "Known" && unequal.Instance == null && unequal.CallbacksInvoked == 0, "unsigned bytes remain exact, with no case or encoding fallback");

        var prefix = EngineRootSnapshot.Capture(Node(new byte[] { 0x41 }, 0, null, id), binding, Limits);
        Need(prefix.Status == "Captured" && prefix.HasUnknownMetadata && prefix.NameBytes == 1, "unterminated observed prefix is retained and marked incomplete");
        var earlyMismatch = ClassFactoryDispatch.Create(prefix.Root, Letter(0x42));
        var needsMissingByte = ClassFactoryDispatch.Create(prefix.Root, Letter(0x41));
        Need(earlyMismatch.Status == "Known" && earlyMismatch.Instance == null, "early mismatch needs no invented NUL or later bytes");
        Need(needsMissingByte.Status == "Unsupported" && needsMissingByte.Reason == "ClassFactoryNameBytesIncomplete", "matching incomplete prefix cannot acquire a synthetic terminator");

        object[] unknownNames = { null, new object(), "Actor" };
        foreach (object name in unknownNames) {
            var unknown = EngineRootSnapshot.Capture(Node(name, 0, null, id), binding, Limits);
            var query = ClassFactoryDispatch.Create(unknown.Root, ActorName());
            Need(unknown.Status == "Captured" && unknown.HasUnknownMetadata && unknown.NameBytes == 0, "null and non-byte name objects remain unknown metadata");
            Need(query.Status == "Unsupported" && query.Reason == "ClassFactoryNameBytesIncomplete" && query.CallbacksInvoked == 0, "adapter does not stringify or guess an unknown name");
        }
        scenarios++;
    }

    static void SelfFirstAndNullFactoryResults()
    {
        object selfId = new object(), childId = new object(); int selfCalls = 0, childCalls = 0;
        var child = Node(Letter(0x41), 0, null, childId);
        var self = Node(Letter(0x41), 1, Storage(child), selfId);
        var ownBindings = new EngineFactoryBinding[] {
            Bind(selfId, delegate { selfCalls++; return null; }),
            Bind(childId, delegate { childCalls++; return new object(); })
        };
        var rootSnapshot = EngineRootSnapshot.Capture(self, ownBindings, Limits);
        var selfResult = ClassFactoryDispatch.Create(rootSnapshot.Root, Letter(0x41));
        Need(selfResult.Status == "Known" && selfResult.Instance == null && selfResult.VisitedNodes == 1 && selfResult.CallbacksInvoked == 1, "matching self tail result returns even when its own factory returns null");
        Need(selfCalls == 1 && childCalls == 0, "matching node never searches its own children after a null factory result");

        object topId = new object(), firstId = new object(), grandId = new object(), lastId = new object();
        object answer = new object(); int firstCalls = 0, grandCalls = 0, lastCalls = 0;
        var grand = Node(Letter(0x41), 0, null, grandId);
        var first = Node(Letter(0x41), 1, Storage(grand), firstId);
        var last = Node(Letter(0x41), 0, null, lastId);
        var top = Node(Letter(0x52), 2, Storage(first, last), topId);
        var bindings = new EngineFactoryBinding[] {
            Bind(topId, delegate { return null; }),
            Bind(firstId, delegate { firstCalls++; return null; }),
            Bind(grandId, delegate { grandCalls++; return new object(); }),
            Bind(lastId, delegate { lastCalls++; return answer; })
        };
        var captured = EngineRootSnapshot.Capture(top, bindings, Limits);
        var query = ClassFactoryDispatch.Create(captured.Root, Letter(0x41));
        Need(query.Status == "Known" && Object.ReferenceEquals(query.Instance, answer) && query.VisitedNodes == 3 && query.CallbacksInvoked == 2, "parent continues to the next sibling after a matching child factory returns null");
        Need(firstCalls == 1 && grandCalls == 0 && lastCalls == 1, "first name candidate is not necessarily the final nonnull factory result");

        bindings[1] = Bind(firstId, null); firstCalls = 0; lastCalls = 0;
        var unknown = EngineRootSnapshot.Capture(top, bindings, Limits);
        var stopped = ClassFactoryDispatch.Create(unknown.Root, Letter(0x41));
        Need(unknown.Status == "Captured" && unknown.HasUnknownMetadata, "an explicit empty factory binding is retained as unknown callback metadata");
        Need(stopped.Status == "Unsupported" && stopped.Reason == "UnknownClassFactoryCallback" && stopped.CallbacksInvoked == 0 && lastCalls == 0,
            "unknown callback is not replaced by a callable that returns null and permits sibling fallback");
        scenarios++;
    }

    static void KnownPrefixesNullSlotsAndSignedCounts()
    {
        object rootId = new object(), childId = new object(), answer = new object();
        var child = Node(Letter(0x41), 0, null, childId);
        var root = Node(Letter(0x52), int.MaxValue, Storage(child), rootId);
        var bindings = new EngineFactoryBinding[] { Bind(rootId, delegate { return null; }), Bind(childId, delegate { return answer; }) };
        var prefix = EngineRootSnapshot.Capture(root, bindings, Limits);
        Need(prefix.Status == "Captured" && prefix.HasUnknownMetadata && prefix.Nodes == 2 && prefix.Edges == 1 && prefix.Root.ChildCount == int.MaxValue,
            "huge count captures only the one known slot without allocating count-sized storage");
        var hit = ClassFactoryDispatch.Create(prefix.Root, Letter(0x41));
        Need(hit.Status == "Known" && Object.ReferenceEquals(hit.Instance, answer), "known-prefix hit succeeds before unknown tail is needed");
        var miss = ClassFactoryDispatch.Create(prefix.Root, Letter(0x5a));
        Need(miss.Status == "Unsupported" && miss.Reason == "UnknownClassFactoryChildSlot" && miss.VisitedNodes == 2, "missing tail is not padded with known null child slots");

        root.Count = 1; root.Children = Storage((EngineRootNode)null);
        var nullSlot = EngineRootSnapshot.Capture(root, bindings, Limits);
        var nullQuery = ClassFactoryDispatch.Create(nullSlot.Root, Letter(0x5a));
        Need(nullSlot.Status == "Captured" && nullSlot.HasUnknownMetadata && nullSlot.Nodes == 1 && nullSlot.Edges == 1, "known null slot consumes an edge but not a node");
        Need(nullQuery.Status == "Unsupported" && nullQuery.Reason == "UnknownClassInfoSnapshot" && nullQuery.VisitedNodes == 2, "known null pointer and absent slot remain distinct query states");

        root.Children = new EngineRootChildStorage(null);
        var missingSpan = EngineRootSnapshot.Capture(root, bindings, Limits);
        var missingQuery = ClassFactoryDispatch.Create(missingSpan.Root, Letter(0x5a));
        Need(missingSpan.Status == "Captured" && missingSpan.Edges == 0 && missingSpan.HasUnknownMetadata &&
            missingQuery.Reason == "UnknownClassFactoryChildSlot", "unprovided span stays unknown without manufacturing a child array");

        int[] noChildren = { 0, -2 };
        foreach (int count in noChildren) {
            root.Count = count; root.Children = Storage(root); root.Parent = root;
            var ignored = EngineRootSnapshot.Capture(root, bindings, Limits);
            var query = ClassFactoryDispatch.Create(ignored.Root, Letter(0x5a));
            Need(ignored.Status == "Captured" && ignored.Nodes == 1 && ignored.Edges == 0 && ignored.Root.ChildCount == count, "signed nonpositive count skips unused slots and Parent links");
            Need(query.Status == "Known" && query.Instance == null && query.VisitedNodes == 1, "nonpositive count does not invent traversal into unused cyclic slots");
        }
        scenarios++;
    }

    sealed class EqualToken
    {
        readonly int value;
        internal EqualToken(int value) { this.value = value; }
        public override bool Equals(object other) { var token = other as EqualToken; return token != null && token.value == value; }
        public override int GetHashCode() { return value; }
    }

    static void SharedNodesAndReferenceBindings()
    {
        object rootId = new object(), sharedId = new object(), otherId = new object(), answer = new object();
        var shared = Node(Letter(0x41), 0, null, sharedId);
        var other = Node(Letter(0x41), 0, null, otherId);
        var root = Node(Letter(0x52), 3, Storage(shared, shared, other), rootId);
        int sharedCalls = 0, otherCalls = 0;
        var bindings = new EngineFactoryBinding[] {
            Bind(rootId, delegate { return null; }),
            Bind(sharedId, delegate { sharedCalls++; return sharedCalls == 1 ? null : answer; }),
            Bind(otherId, delegate { otherCalls++; return new object(); })
        };
        var captured = EngineRootSnapshot.Capture(root, bindings, Limits);
        Need(captured.Status == "Captured" && captured.Nodes == 3 && captured.Edges == 3 && captured.NameBytes == 6, "completed shared node is captured once while all duplicate slots remain");
        Need(sharedCalls == 0 && otherCalls == 0, "capture resolves delegates without calling them");
        var query = ClassFactoryDispatch.Create(captured.Root, Letter(0x41));
        Need(query.Status == "Known" && Object.ReferenceEquals(query.Instance, answer) && query.VisitedNodes == 3 && query.CallbacksInvoked == 2,
            "repeated edge invokes the shared node factory twice rather than memoizing its first null result");
        Need(sharedCalls == 2 && otherCalls == 0, "second shared occurrence stops search before the later sibling");

        var firstIdentity = new EqualToken(7); var secondIdentity = new EqualToken(7);
        object firstObject = new object(), secondObject = new object();
        Need(firstIdentity.Equals(secondIdentity) && !Object.ReferenceEquals(firstIdentity, secondIdentity), "fixture identities are value-equal but distinct objects");
        var second = Node(Letter(0x41), 0, null, secondIdentity);
        var first = Node(Letter(0x52), 1, Storage(second), firstIdentity);
        var equalBindings = new EngineFactoryBinding[] {
            Bind(firstIdentity, delegate { return firstObject; }), Bind(secondIdentity, delegate { return secondObject; })
        };
        var equal = EngineRootSnapshot.Capture(first, equalBindings, Limits);
        Need(equal.Status == "Captured" && !equal.HasUnknownMetadata, "value-equal identities are accepted as separate reference bindings");
        Need(Object.ReferenceEquals(ClassFactoryDispatch.Create(equal.Root, Letter(0x52)).Instance, firstObject) &&
            Object.ReferenceEquals(ClassFactoryDispatch.Create(equal.Root, Letter(0x41)).Instance, secondObject), "factory resolution uses reference identity rather than overridden Equals");
        var duplicate = EngineRootSnapshot.Capture(first, new EngineFactoryBinding[] { equalBindings[0], Bind(firstIdentity, delegate { return secondObject; }) }, Limits);
        Need(duplicate.Status == "Unsupported" && duplicate.Reason == "CaptureDuplicateBindingIdentity" && duplicate.Root == null, "duplicate same-reference binding is rejected instead of silently choosing a factory");
        scenarios++;
    }

    static void RegistrationPartialStateAndExitFailure()
    {
        int[] cuts = { 7, 9 };
        foreach (int cut in cuts) {
            object rootId = new object(), childId = new object(), answer = new object();
            var old = Storage(); var parent = Node(ObjectName(), 0, old, rootId);
            var child = Node(null, 0, null, childId); child.Parent = parent;
            var writer = EngineRootRegistration.Apply(EngineRootWriter.Entity, child, WriterDependencies(), ExitState(), new EngineModelLimits(cut, 8));
            Need(writer.Status == EngineModelStatus.ResourceLimit && writer.NameStored && writer.ChildAppended && writer.CountIncremented && parent.Count == 1,
                "real writer cut retains initialized child name, appended allocation and incremented parent count");
            Need(SameBytes(child.Name as byte[], EntityName()) && Object.ReferenceEquals(writer.AllocatedStorage.Slots[0], child), "partial writer used the observed Entity name and kept its unpublished or published allocation");
            var capture = EngineRootSnapshot.Capture(parent, new EngineFactoryBinding[] { Bind(rootId, delegate { return null; }), Bind(childId, delegate { return answer; }) }, Limits);
            var query = ClassFactoryDispatch.Create(capture.Root, EntityName());
            if (cut == 7) {
                Need(!writer.StoragePublished && Object.ReferenceEquals(parent.Children, old) && capture.Status == "Captured" && capture.Nodes == 1 && capture.Edges == 0,
                    "pre-publication capture uses current old storage rather than repairing from AllocatedStorage");
                Need(capture.HasUnknownMetadata && query.Status == "Unsupported" && query.Reason == "UnknownClassFactoryChildSlot", "incremented count and short old span remain visible as an incomplete query");
            } else {
                Need(writer.StoragePublished && capture.Status == "Captured" && capture.Nodes == 2 && capture.Edges == 1, "post-publication cut exposes the new child despite unfinished registration");
                Need(query.Status == "Known" && Object.ReferenceEquals(query.Instance, answer) && writer.ReturnValue == null,
                    "local captured query can complete without claiming the interrupted writer completed");
            }
        }

        object pId = new object(), nId = new object(), instance = new object();
        var p = Node(ObjectName(), 0, null, pId); var n = Node(null, 0, null, nId); n.Parent = p;
        var dependencies = WriterDependencies();
        dependencies.CrtAtExit = delegate(ulong callback) { return EngineDependencyResult<int>.Normal(-1); };
        var exitFailure = EngineRootRegistration.Apply(EngineRootWriter.Entity, n, dependencies, ExitState(), WriterLimits);
        var afterExit = EngineRootSnapshot.Capture(p, new EngineFactoryBinding[] { Bind(pId, delegate { return null; }), Bind(nId, delegate { return instance; }) }, Limits);
        Need(exitFailure.Status == EngineModelStatus.Completed && exitFailure.ReturnValue == -1 && exitFailure.StoragePublished, "returned exit-registration failure keeps published children while local execution completes");
        Need(afterExit.Status == "Captured" && Object.ReferenceEquals(ClassFactoryDispatch.Create(afterExit.Root, EntityName()).Instance, instance),
            "snapshot preserves published state without promoting minus-one registration to native success");

        var nullNameParent = Node(ObjectName(), 0, null, pId); var nullNameChild = Node(new object(), 0, null, nId); nullNameChild.Parent = nullNameParent;
        var nullNameDeps = WriterDependencies(); nullNameDeps.DuplicateName = delegate(ulong source) { return EngineDependencyResult<object>.Normal(null); };
        var nullNameWriter = EngineRootRegistration.Apply(EngineRootWriter.Entity, nullNameChild, nullNameDeps, ExitState(), WriterLimits);
        var nullNameCapture = EngineRootSnapshot.Capture(nullNameParent, new EngineFactoryBinding[] { Bind(pId, delegate { return null; }), Bind(nId, delegate { return instance; }) }, Limits);
        var nullNameQuery = ClassFactoryDispatch.Create(nullNameCapture.Root, EntityName());
        Need(nullNameWriter.Status == EngineModelStatus.Completed && nullNameChild.Name == null && nullNameCapture.HasUnknownMetadata, "actual null strdup result is not repaired with the known source literal");
        Need(nullNameQuery.Status == "Unsupported" && nullNameQuery.Reason == "ClassFactoryNameBytesIncomplete", "unknown returned name remains unknown even though the writer source token is known");
        scenarios++;
    }

    static void CaptureCyclesAndIndependentBudgets()
    {
        object id = new object(); int calls = 0;
        var bindings = new EngineFactoryBinding[] { Bind(id, delegate { calls++; return null; }) };
        var cycle = Node(Letter(0x52), 1, null, id); cycle.Children = Storage(cycle);
        var cyclic = EngineRootSnapshot.Capture(cycle, bindings, Limits);
        Need(cyclic.Status == "Unsupported" && cyclic.Reason == "CaptureChildCycle" && cyclic.Root == null && calls == 0, "active children cycle fails capture without invoking a factory");

        var leaf = Node(Letter(0x4c), 0, null, id);
        var shared = Node(Letter(0x53), 1, Storage(leaf), id);
        var longer = Node(Letter(0x43), 1, Storage(shared), id);
        var root = Node(Letter(0x52), 2, Storage(shared, longer), id);
        var tooDeep = EngineRootSnapshot.Capture(root, bindings, new EngineSnapshotLimits(16, 32, 256, 3));
        Need(tooDeep.Status == "Unsupported" && tooDeep.Reason == "CaptureDepthBudget" && tooDeep.Root == null,
            "memoized shared subtree cannot bypass depth limit when reached through a later deeper path");
        var fits = EngineRootSnapshot.Capture(root, bindings, new EngineSnapshotLimits(4, 4, 8, 4));
        Need(fits.Status == "Captured" && fits.Nodes == 4 && fits.Edges == 4 && fits.NameBytes == 8, "legal shared DAG fits exact distinct-node, edge and byte budgets");

        EngineSnapshotLimits[] shortLimits = {
            new EngineSnapshotLimits(3, 4, 8, 4), new EngineSnapshotLimits(4, 3, 8, 4),
            new EngineSnapshotLimits(4, 4, 7, 4), new EngineSnapshotLimits(4, 4, 8, 0)
        };
        string[] reasons = { "CaptureNodeBudget", "CaptureEdgeBudget", "CaptureNameByteBudget", "CaptureDepthBudget" };
        for (int i = 0; i < shortLimits.Length; i++) {
            var stopped = EngineRootSnapshot.Capture(root, bindings, shortLimits[i]);
            Need(stopped.Status == "Unsupported" && stopped.Reason == reasons[i] && stopped.Root == null, "independent snapshot budget rejects incomplete capture " + i);
        }
        Need(calls == 0 && root.Count == 2 && Object.ReferenceEquals(root.Children.Slots[0], shared) && Object.ReferenceEquals(root.Children.Slots[1], longer),
            "failed and successful captures neither execute factories nor alter source edges");

        var repeatedNulls = Node(Letter(0x52), 3, Storage(null, null, null), id);
        var edgeFailure = EngineRootSnapshot.Capture(repeatedNulls, bindings, new EngineSnapshotLimits(1, 2, 2, 1));
        Need(edgeFailure.Status == "Unsupported" && edgeFailure.Reason == "CaptureEdgeBudget", "known null slots consume edge budget even though they add no node");
        var nullFits = EngineRootSnapshot.Capture(repeatedNulls, bindings, new EngineSnapshotLimits(1, 3, 2, 1));
        Need(nullFits.Status == "Captured" && nullFits.Nodes == 1 && nullFits.Edges == 3 && nullFits.HasUnknownMetadata, "null slots have zero subtree height and are retained within exact limits");
        scenarios++;
    }

    static void EmptyRootsAndInvalidInputs()
    {
        var empty = EngineRootSnapshot.Capture(null, null, new EngineSnapshotLimits(0, 0, 0, 0));
        Need(empty.Status == "Captured" && empty.Root == null && empty.HasUnknownMetadata && empty.Nodes == 0 && empty.Edges == 0 && empty.NameBytes == 0,
            "empty root is an explicit unknown snapshot, not an invented complete registry");
        Need(ClassFactoryDispatch.Create(empty.Root, ActorName()).Status == "Unsupported", "empty captured root does not become a known missing-class result");
        object id = new object(); var root = Node(Letter(0x52), 0, null, id);
        var missing = EngineRootSnapshot.Capture(root, null, Limits);
        var missingQuery = ClassFactoryDispatch.Create(missing.Root, Letter(0x52));
        Need(missing.Status == "Captured" && missing.HasUnknownMetadata && missingQuery.Reason == "UnknownClassFactoryCallback", "missing factory map remains unknown without guessing by name");
        EngineFactoryBinding[][] badBindings = {
            new EngineFactoryBinding[] { null }, new EngineFactoryBinding[] { Bind(null, delegate { return null; }) }
        };
        foreach (EngineFactoryBinding[] bindings in badBindings) {
            var bad = EngineRootSnapshot.Capture(root, bindings, Limits);
            Need(bad.Status == "Unsupported" && bad.Reason == "CaptureInvalidBinding" && bad.Root == null, "null binding or identity is rejected");
        }
        EngineSnapshotLimits[] invalid = {
            null, new EngineSnapshotLimits(-1, 0, 0, 0), new EngineSnapshotLimits(4097, 0, 0, 0),
            new EngineSnapshotLimits(0, 16385, 0, 0), new EngineSnapshotLimits(0, 0, 1048577, 0), new EngineSnapshotLimits(0, 0, 0, 129)
        };
        foreach (EngineSnapshotLimits limits in invalid) {
            var bad = EngineRootSnapshot.Capture(root, null, limits);
            Need(bad.Status == "Unsupported" && bad.Reason == "CaptureLimitsOutsideCoveredDomain" && bad.Root == null && bad.Nodes == 0, "invalid declared budget fails before capture or allocation");
        }
        scenarios++;
    }

    public static int Main()
    {
        try {
            ObservedNameCopies();
            RealNamesThroughWritersAndSnapshot();
            RawBytesAndUnknownNames();
            SelfFirstAndNullFactoryResults();
            KnownPrefixesNullSlotsAndSignedCounts();
            SharedNodesAndReferenceBindings();
            RegistrationPartialStateAndExitFailure();
            CaptureCyclesAndIndependentBudgets();
            EmptyRootsAndInvalidInputs();
            Console.WriteLine("PASS: " + assertions + " assertions, " + scenarios + " scenario groups");
            return 0;
        } catch (Exception error) {
            Console.Error.WriteLine("FAIL after " + assertions + " assertions, " + scenarios + " completed scenario groups: " + error);
            return 1;
        }
    }
}
