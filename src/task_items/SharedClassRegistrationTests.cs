using System;
using GrimDynasty.TaskItems;

// Only bounded own fixtures. No files, native pointers, native factories or CRT run.
// The five-node chain is explicitly supplied. Reentrant fixtures deliberately use
// synthetic parent topologies and callback mutations, not an inferred native graph.
class SharedClassRegistrationTests
{
    static int assertions, scenarios;
    static readonly EngineModelLimits EngineLimits = new EngineModelLimits(128, 8);
    static readonly EngineSnapshotLimits SnapshotLimits = new EngineSnapshotLimits(16, 32, 256, 16);

    static void Need(bool value, string why)
    {
        assertions++;
        if (!value) throw new Exception(why);
    }

    static byte[] ActorName() { return new byte[] { 65, 99, 116, 111, 114, 0 }; }
    static byte[] EntityName() { return new byte[] { 69, 110, 116, 105, 116, 121, 0 }; }
    static byte[] ObjectName() { return new byte[] { 79, 98, 106, 101, 99, 116, 0 }; }
    static byte[] ItemName() { return new byte[] { 73, 116, 101, 109, 0 }; }
    static byte[] QuestName() { return new byte[] { 81, 117, 101, 115, 116, 73, 116, 101, 109, 0 }; }
    static byte[] OtherName() { return new byte[] { 88, 0 }; }
    static bool Bytes(object actual, byte[] expected)
    {
        byte[] bytes = actual as byte[];
        if (bytes == null || bytes.Length != expected.Length) return false;
        for (int i = 0; i < bytes.Length; i++) if (bytes[i] != expected[i]) return false;
        return true;
    }

    static EngineRootChildStorage Storage(params EngineRootNode[] slots) { return new EngineRootChildStorage(slots); }
    static EngineRootNode Node(object name, EngineRootNode parent, int count, EngineRootChildStorage children)
    {
        return new EngineRootNode(name, parent, count, children, new object());
    }
    static EngineRootChildStorage SmallAllocation(ulong size)
    {
        if (size > 64 || size % 8 != 0) throw new Exception("independent fixture allocation ceiling");
        return Storage(new EngineRootNode[(int)(size / 8)]);
    }
    static SharedClassRegistrationCallbacks GameDependencies()
    {
        // Separate Game whole-helper contracts. Never call Engine allocation/exit helpers here.
        return new SharedClassRegistrationCallbacks(delegate(byte[] literal) { return (byte[])literal.Clone(); },
            SmallAllocation, delegate(EngineRootChildStorage old) { }, delegate(ulong callback) { return 0; });
    }
    static EngineRootDependencies EngineDependencies()
    {
        return new EngineRootDependencies {
            DuplicateName = delegate(ulong source) {
                byte[] bytes;
                if (!EngineRootObservedNames.TryCopy(source, out bytes)) return EngineDependencyResult<object>.Unknown("fixture source");
                return EngineDependencyResult<object>.Normal(bytes);
            },
            Malloc = delegate(ulong size) { return EngineDependencyResult<EngineRootChildStorage>.Normal(SmallAllocation(size)); },
            NewHandler = delegate(ulong size) { throw new Exception("unexpected fixture retry"); },
            Throw = delegate(EngineThrowRequest request) { throw new Exception("unexpected fixture throw"); },
            Free = delegate(EngineRootChildStorage old) { return EngineDependencyResult<EngineUnit>.Normal(EngineUnit.Value); },
            CrtAtExit = delegate(ulong callback) { return EngineDependencyResult<int>.Normal(0); },
            RegisterOnExit = delegate(EngineExitState state, ulong callback) { return EngineDependencyResult<int>.Normal(0); }
        };
    }
    static EngineExitState ExitState() { return new EngineExitState(new object(), 0UL, ulong.MaxValue); }
    static EngineRootResult EngineApply(EngineRootWriter writer, EngineRootNode node, EngineRootDependencies dependencies)
    {
        return EngineRootRegistration.Apply(writer, node, dependencies, ExitState(), EngineLimits);
    }
    static EngineFactoryBinding[] NullBindings(params EngineRootNode[] nodes)
    {
        var result = new EngineFactoryBinding[nodes.Length];
        for (int i = 0; i < nodes.Length; i++) result[i] = new EngineFactoryBinding(nodes[i].FactoryIdentity, delegate { return null; });
        return result;
    }

    static void ExplicitFiveNodeChain()
    {
        var root = Node(null, null, 0, null);
        var entity = Node(null, root, 0, null);
        var actor = Node(null, entity, 0, null);
        var item = Node(null, null, 0, null);
        var quest = Node(null, item, 0, null);
        object actorFactory = actor.FactoryIdentity, itemFactory = item.FactoryIdentity, questFactory = quest.FactoryIdentity;
        var game = GameDependencies(); var engine = EngineDependencies();
        var qr = SharedClassRegistration.ApplyQuestItem(quest, game);
        var itemChildren = item.Children;
        var ir = SharedClassRegistration.ApplyItem(item, actor, game);
        var actorChildren = actor.Children;
        var ar = EngineApply(EngineRootWriter.Actor, actor, engine);
        var er = EngineApply(EngineRootWriter.Entity, entity, engine);
        var rr = EngineApply(EngineRootWriter.Object, root, engine);
        Need(qr.Status == "Known" && ir.Status == "Known" && ar.Status == EngineModelStatus.Completed &&
            er.Status == EngineModelStatus.Completed && rr.Status == EngineModelStatus.Completed, "explicit own Game and Engine calls complete");
        Need(Object.ReferenceEquals(ir.Node, item) && Object.ReferenceEquals(ir.SelectedParent, actor) &&
            Object.ReferenceEquals(qr.Node, quest) && Object.ReferenceEquals(qr.SelectedParent, item), "Game journals retain the exact shared node identities");
        Need(Object.ReferenceEquals(item.Parent, actor) && Object.ReferenceEquals(quest.Parent, item) &&
            Object.ReferenceEquals(actor.Parent, entity) && Object.ReferenceEquals(entity.Parent, root), "the cross-module parent chain uses references without conversion");
        Need(root.Count == 1 && entity.Count == 1 && actor.Count == 1 && item.Count == 1 && quest.Count == 0,
            "later parent writers do not reset existing descendant counts");
        Need(Object.ReferenceEquals(actor.Children, actorChildren) && Object.ReferenceEquals(item.Children, itemChildren) &&
            Object.ReferenceEquals(root.Children.Slots[0], entity) && Object.ReferenceEquals(entity.Children.Slots[0], actor) &&
            Object.ReferenceEquals(actor.Children.Slots[0], item) && Object.ReferenceEquals(item.Children.Slots[0], quest), "all published slots and existing arrays preserve identity");
        Need(Object.ReferenceEquals(actor.FactoryIdentity, actorFactory) && Object.ReferenceEquals(item.FactoryIdentity, itemFactory) &&
            Object.ReferenceEquals(quest.FactoryIdentity, questFactory), "both writer families preserve factory identities");
        Need(Bytes(root.Name, ObjectName()) && Bytes(entity.Name, EntityName()) && Bytes(actor.Name, ActorName()) &&
            Bytes(item.Name, ItemName()) && Bytes(quest.Name, QuestName()), "each writer stores its independently specified name bytes");
        int itemCalls = 0, questCalls = 0; object itemValue = new object(), questValue = new object();
        var bindings = NullBindings(root, entity, actor, item, quest);
        bindings[3] = new EngineFactoryBinding(itemFactory, delegate { itemCalls++; return itemValue; });
        bindings[4] = new EngineFactoryBinding(questFactory, delegate { questCalls++; return questValue; });
        var snapshot = EngineRootSnapshot.Capture(root, bindings, SnapshotLimits);
        Need(snapshot.Status == "Captured" && !snapshot.HasUnknownMetadata && snapshot.Nodes == 5 && snapshot.Edges == 4 && snapshot.NameBytes == 35,
            "existing snapshot accepts the five shared records with exact small resource accounting");
        Need(itemCalls == 0 && questCalls == 0, "capture executes no constructor callback");
        var itemQuery = ClassFactoryDispatch.Create(snapshot.Root, ItemName());
        var questQuery = ClassFactoryDispatch.Create(snapshot.Root, QuestName());
        Need(itemQuery.Status == "Known" && Object.ReferenceEquals(itemQuery.Instance, itemValue) && itemQuery.VisitedNodes == 4 && itemQuery.CallbacksInvoked == 1,
            "Item dispatch reaches the bound own factory through three Engine nodes");
        Need(questQuery.Status == "Known" && Object.ReferenceEquals(questQuery.Instance, questValue) && questQuery.VisitedNodes == 5 && questQuery.CallbacksInvoked == 1,
            "QuestItem dispatch traverses the same shared Game records");
        Need(itemCalls == 1 && questCalls == 1 && ClassFactoryDispatch.Create(snapshot.Root, new byte[] { 105, 116, 101, 109, 0 }).Instance == null,
            "query remains byte-sensitive and only selected own callbacks execute");
        scenarios++;
    }

    static void NestedEngineDuringNameAndParentReload()
    {
        var actor = Node(ActorName(), null, 0, Storage());
        var alternate = Node(OtherName(), null, 0, Storage());
        var item = Node(new object(), alternate, 0, null); object beforeName = item.Name;
        var nested = Node(null, actor, 0, null); EngineRootResult nestedResult = null;
        bool sawAssigned = false; int duplicateCalls = 0; ulong requested = 0;
        var game = GameDependencies();
        var result = SharedClassRegistration.ApplyItem(item, actor, new SharedClassRegistrationCallbacks(
            delegate(byte[] literal) {
                if (++duplicateCalls > 1) throw new Exception("independent callback depth ceiling");
                sawAssigned = Object.ReferenceEquals(item.Parent, actor) && Object.ReferenceEquals(item.Name, beforeName);
                nestedResult = EngineApply(EngineRootWriter.Entity, nested, EngineDependencies());
                return literal;
            }, delegate(ulong size) { requested = size; return SmallAllocation(size); }, game.Free, game.RegisterExit));
        Need(sawAssigned && duplicateCalls == 1 && nestedResult.Status == EngineModelStatus.Completed, "Item parent store is visible before a synchronous Engine writer runs");
        Need(result.Status == "Known" && requested == 16 && result.AllocationCount == 1 && result.CopiedChildren == 1,
            "Game allocation observes the Engine append made during name duplication");
        Need(Object.ReferenceEquals(nestedResult.SelectedParent, actor) && Object.ReferenceEquals(result.SelectedParent, actor) &&
            Object.ReferenceEquals(result.FreeArgument, nestedResult.AllocatedStorage), "both journals and Game free refer to the same live Actor state");
        Need(actor.Count == 2 && Object.ReferenceEquals(actor.Children.Slots[0], nested) && Object.ReferenceEquals(actor.Children.Slots[1], item) && alternate.Count == 0,
            "the nested Engine edge precedes the Game edge without a synchronization copy");
        var redirected = Node(null, actor, 0, null); byte[] returnedName = new byte[] { 0xff, 0 };
        var qr = SharedClassRegistration.ApplyQuestItem(redirected, new SharedClassRegistrationCallbacks(
            delegate(byte[] literal) { redirected.Parent = alternate; return returnedName; }, game.Allocate, game.Free, game.RegisterExit));
        Need(qr.Status == "Known" && !qr.ParentAssigned && Object.ReferenceEquals(qr.SelectedParent, alternate) &&
            Object.ReferenceEquals(redirected.Name, returnedName), "QuestItem reloads the callback-modified parent and stores the exact returned array");
        Need(alternate.Count == 1 && Object.ReferenceEquals(alternate.Children.Slots[0], redirected) && actor.Count == 2,
            "redirected QuestItem appends only to its post-name selected parent");
        scenarios++;
    }

    static void NestedAllocationGrowthAndShrink()
    {
        var actor = Node(ActorName(), null, 0, Storage()); var item = Node(null, null, 0, null);
        var nested = Node(null, actor, 0, null); var game = GameDependencies();
        var exact = Storage((EngineRootNode)null); EngineRootResult inner = null; int allocations = 0, frees = 0, exits = 0;
        var growth = SharedClassRegistration.ApplyItem(item, actor, new SharedClassRegistrationCallbacks(game.DuplicateName,
            delegate(ulong size) {
                if (++allocations > 1 || size != 8) throw new Exception("bounded exact-capacity growth fixture");
                inner = EngineApply(EngineRootWriter.Entity, nested, EngineDependencies()); return exact;
            }, delegate(EngineRootChildStorage old) { frees++; }, delegate(ulong token) { exits++; return 0; }));
        Need(inner.Status == EngineModelStatus.Completed && growth.Status == "Unsupported" && growth.Stage == "AppendChild" &&
            growth.Reason == "ClassRegistrationAppendOutsideCoveredDomain", "post-allocation growth cannot become success by enlarging the original allocation");
        Need(growth.AllocationCount == 0 && growth.RequestedBytes == 8 && growth.CopiedChildren == 1 && Object.ReferenceEquals(exact.Slots[0], nested),
            "the original request is retained while the updated one-child source is copied");
        Need(actor.Count == 1 && Object.ReferenceEquals(actor.Children, inner.AllocatedStorage) && Object.ReferenceEquals(actor.Children.Slots[0], nested) &&
            !growth.ChildAppended && !growth.CountIncremented && !growth.StoragePublished, "failed Game append preserves the nested Engine publication");
        Need(frees == 0 && exits == 0 && growth.NameAssigned && Bytes(item.Name, ItemName()), "growth failure retains its name prefix and skips later callbacks");

        var first = Node(OtherName(), null, 0, null); var original = Storage(first, first);
        actor = Node(ActorName(), null, 2, original); item = Node(null, null, 0, null);
        var alternate = Node(OtherName(), null, 0, null); nested = Node(null, actor, 0, null);
        var wide = Storage(null, null, null); EngineRootChildStorage current = null; inner = null; allocations = 0;
        // Deliberate own callback mutation creates a smaller valid source. The
        // returned destination still has exactly the three slots requested.
        var nestedDeps = EngineDependencies();
        nestedDeps.DuplicateName = delegate(ulong token) {
            actor.Count = 0; actor.Children = Storage();
            return EngineDependencyResult<object>.Normal(EngineRootObservedNames.CopyForWriter(EngineRootWriter.Entity));
        };
        var shrink = SharedClassRegistration.ApplyItem(item, actor, new SharedClassRegistrationCallbacks(game.DuplicateName,
            delegate(ulong size) {
                if (++allocations > 1 || size != 24) throw new Exception("bounded shrink fixture");
                inner = EngineApply(EngineRootWriter.Entity, nested, nestedDeps); current = actor.Children;
                item.Parent = alternate; return wide;
            }, game.Free, game.RegisterExit));
        Need(inner.Status == EngineModelStatus.Completed && shrink.Status == "Known" && shrink.RequestedBytes == 24 && shrink.AllocationCount == 2,
            "the request uses the pre-callback count even when nested code shrinks live state");
        Need(Object.ReferenceEquals(shrink.SelectedParent, actor) && Object.ReferenceEquals(item.Parent, alternate) && alternate.Count == 0,
            "later node-parent mutation does not redirect the already selected parent");
        Need(shrink.CopiedChildren == 1 && actor.Count == 2 && Object.ReferenceEquals(wide.Slots[0], nested) &&
            Object.ReferenceEquals(wide.Slots[1], item) && wide.Slots[2] == null, "Game copies the updated storage and appends at the reread count");
        Need(Object.ReferenceEquals(shrink.FreeArgument, current) && Object.ReferenceEquals(actor.Children, wide) &&
            Object.ReferenceEquals(original.Slots[0], first) && Object.ReferenceEquals(original.Slots[1], first), "free targets the inner publication while old detached storage remains unchanged");
        scenarios++;
    }

    static void NestedGameDuringEngineAllocation()
    {
        var entity = Node(EntityName(), null, 0, Storage()); var actor = Node(null, entity, 0, null);
        var item = Node(null, null, 0, null); var quest = Node(null, item, 0, null);
        SharedClassRegistrationResult ir = null, qr = null; int mallocs = 0;
        var dependencies = EngineDependencies(); var game = GameDependencies();
        dependencies.Malloc = delegate(ulong size) {
            if (++mallocs > 1 || size != 8) throw new Exception("independent nested Game ceiling");
            ir = SharedClassRegistration.ApplyItem(item, actor, game);
            qr = SharedClassRegistration.ApplyQuestItem(quest, game);
            return EngineDependencyResult<EngineRootChildStorage>.Normal(SmallAllocation(size));
        };
        var result = EngineApply(EngineRootWriter.Actor, actor, dependencies);
        Need(result.Status == EngineModelStatus.Completed && ir.Status == "Known" && qr.Status == "Known" && mallocs == 1,
            "Engine allocation callback can complete both Game writers over the same records");
        Need(Object.ReferenceEquals(ir.SelectedParent, actor) && Object.ReferenceEquals(qr.SelectedParent, item) &&
            Object.ReferenceEquals(result.SelectedParent, entity), "the three operations retain their exact selected parent identities");
        Need(entity.Count == 1 && actor.Count == 1 && item.Count == 1 && Object.ReferenceEquals(entity.Children.Slots[0], actor) &&
            Object.ReferenceEquals(actor.Children, ir.AllocatedStorage) && Object.ReferenceEquals(item.Children, qr.AllocatedStorage),
            "outer Engine publication preserves descendant arrays written by nested Game callbacks");
        var bindings = NullBindings(entity, actor, item, quest); object answer = new object(); int calls = 0;
        bindings[3] = new EngineFactoryBinding(quest.FactoryIdentity, delegate { calls++; return answer; });
        var snapshot = EngineRootSnapshot.Capture(entity, bindings, SnapshotLimits);
        var query = ClassFactoryDispatch.Create(snapshot.Root, QuestName());
        Need(snapshot.Status == "Captured" && snapshot.Nodes == 4 && snapshot.Edges == 3 && !snapshot.HasUnknownMetadata,
            "nested Game state reaches the existing adapter without conversion");
        Need(query.Status == "Known" && Object.ReferenceEquals(query.Instance, answer) && query.VisitedNodes == 4 && calls == 1,
            "nested state participates in the existing ordered dispatcher");
        scenarios++;
    }

    static void RepeatedSharedEdgesAndFactories()
    {
        var actor = Node(ActorName(), null, 0, null); var item = Node(null, null, 0, null); var quest = Node(null, item, 0, null);
        var baseGame = GameDependencies(); int exits = 0;
        var game = new SharedClassRegistrationCallbacks(baseGame.DuplicateName, baseGame.Allocate, baseGame.Free, delegate(ulong token) { exits++; return -1; });
        var i1 = SharedClassRegistration.ApplyItem(item, actor, game); var firstItemStorage = actor.Children;
        var i2 = SharedClassRegistration.ApplyItem(item, actor, game);
        var q1 = SharedClassRegistration.ApplyQuestItem(quest, game); var firstQuestStorage = item.Children;
        var q2 = SharedClassRegistration.ApplyQuestItem(quest, game);
        Need(i1.Status == "Known" && i2.Status == "Known" && q1.Status == "Known" && q2.Status == "Known" &&
            i1.ReturnValue == -1 && i2.ReturnValue == -1 && q1.ReturnValue == -1 && q2.ReturnValue == -1 && exits == 4,
            "normal Game exit minus one leaves every repeated registration locally complete");
        Need(actor.Count == 2 && item.Count == 2 && Object.ReferenceEquals(actor.Children.Slots[0], item) &&
            Object.ReferenceEquals(actor.Children.Slots[1], item) && Object.ReferenceEquals(item.Children.Slots[0], quest) &&
            Object.ReferenceEquals(item.Children.Slots[1], quest), "both levels retain repeated references instead of deduplicating");
        Need(Object.ReferenceEquals(i2.FreeArgument, firstItemStorage) && Object.ReferenceEquals(q2.FreeArgument, firstQuestStorage),
            "each repeat frees the preceding storage identity");
        int itemCalls = 0, questCalls = 0; object itemAnswer = new object(), questAnswer = new object();
        var bindings = NullBindings(actor, item, quest);
        bindings[1] = new EngineFactoryBinding(item.FactoryIdentity, delegate { itemCalls++; return itemCalls == 2 ? itemAnswer : null; });
        bindings[2] = new EngineFactoryBinding(quest.FactoryIdentity, delegate { questCalls++; return questCalls == 4 ? questAnswer : null; });
        var snapshot = EngineRootSnapshot.Capture(actor, bindings, SnapshotLimits);
        Need(snapshot.Status == "Captured" && snapshot.Nodes == 3 && snapshot.Edges == 4 && snapshot.NameBytes == 21 && itemCalls == 0 && questCalls == 0,
            "snapshot memoizes metadata identities but preserves all duplicate slots");
        var questQuery = ClassFactoryDispatch.Create(snapshot.Root, QuestName());
        Need(questQuery.Status == "Known" && Object.ReferenceEquals(questQuery.Instance, questAnswer) &&
            questQuery.VisitedNodes == 7 && questQuery.CallbacksInvoked == 4 && questCalls == 4 && itemCalls == 0,
            "duplicate shared subtrees can invoke the same QuestItem factory four times in slot order");
        var itemQuery = ClassFactoryDispatch.Create(snapshot.Root, ItemName());
        Need(itemQuery.Status == "Known" && Object.ReferenceEquals(itemQuery.Instance, itemAnswer) && itemQuery.VisitedNodes == 3 &&
            itemQuery.CallbacksInvoked == 2 && itemCalls == 2 && questCalls == 4, "self-match null skips own children and the second Item edge calls the same factory again");
        scenarios++;
    }

    static void FreeReentryReturnAndInterruption()
    {
        foreach (bool interrupt in new bool[] { false, true }) {
            var prior = Node(OtherName(), null, 0, null); var original = Storage(prior);
            var actor = Node(ActorName(), null, 1, original); var item = Node(null, null, 0, null);
            var nested = Node(null, actor, 0, null); var outer = Storage(null, null); var game = GameDependencies();
            bool sawPrefix = false; int freeCalls = 0, exitCalls = 0; EngineRootResult inner = null; EngineRootChildStorage innerStorage = null;
            var result = SharedClassRegistration.ApplyItem(item, actor, new SharedClassRegistrationCallbacks(game.DuplicateName,
                delegate(ulong size) { if (size != 16) throw new Exception("free fixture request"); return outer; },
                delegate(EngineRootChildStorage old) {
                    if (++freeCalls > 1) throw new Exception("independent free callback ceiling");
                    sawPrefix = Object.ReferenceEquals(old, original) && Object.ReferenceEquals(actor.Children, original) &&
                        actor.Count == 2 && Object.ReferenceEquals(outer.Slots[1], item);
                    // Supply a valid complete current span before deliberate reentry.
                    actor.Children = Storage(prior, item);
                    inner = EngineApply(EngineRootWriter.Entity, nested, EngineDependencies()); innerStorage = actor.Children;
                    if (interrupt) throw new ApplicationException("own free interruption after nested publication");
                }, delegate(ulong callback) { exitCalls++; return 0; }));
            Need(sawPrefix && freeCalls == 1 && inner.Status == EngineModelStatus.Completed && actor.Count == 3,
                "free sees incremented count before outer publication; controlled reentry adds one more shared edge");
            Need(result.ChildAppended && result.CountIncremented && result.FreeInvoked && Object.ReferenceEquals(result.FreeArgument, original) &&
                Object.ReferenceEquals(outer.Slots[0], prior) && Object.ReferenceEquals(outer.Slots[1], item), "outer journal and destination retain their completed prefix");
            Need(Object.ReferenceEquals(innerStorage.Slots[0], prior) && Object.ReferenceEquals(innerStorage.Slots[1], item) &&
                Object.ReferenceEquals(innerStorage.Slots[2], nested), "inner Engine publication observes shared Game prefix identities");
            if (interrupt) {
                Need(result.Status == "Unsupported" && result.Stage == "FreeOldStorage" &&
                    result.Reason == "OwnClassRegistrationOperationThrew:System.ApplicationException" && !result.FreeCompleted &&
                    !result.StoragePublished && !result.ExitInvoked && exitCalls == 0, "own thrown free stops before outer publication and exit");
                Need(Object.ReferenceEquals(actor.Children, innerStorage) && !Object.ReferenceEquals(actor.Children, outer),
                    "interruption preserves the inner callback publication without rollback or copy-back");
            } else {
                Need(result.Status == "Known" && result.FreeCompleted && result.StoragePublished && result.ExitInvoked && exitCalls == 1,
                    "a normally returning free permits outer publication and exit");
                Need(Object.ReferenceEquals(actor.Children, outer) && actor.Count == 3 && outer.Slots.Length == 2,
                    "outer publication overwrites inner storage but does not reset the callback-modified count");
            }
            var bindings = NullBindings(actor, prior, item, nested); object answer = new object();
            bindings[3] = new EngineFactoryBinding(nested.FactoryIdentity, delegate { return answer; });
            var snapshot = EngineRootSnapshot.Capture(actor, bindings, SnapshotLimits);
            var query = ClassFactoryDispatch.Create(snapshot.Root, EntityName());
            if (interrupt) {
                Need(snapshot.Status == "Captured" && !snapshot.HasUnknownMetadata && query.Status == "Known" && Object.ReferenceEquals(query.Instance, answer),
                    "interrupted outer registration leaves the complete inner edge queryable");
            } else {
                Need(snapshot.Status == "Captured" && snapshot.HasUnknownMetadata && query.Status == "Unsupported" && query.Reason == "UnknownClassFactoryChildSlot",
                    "normal outer overwrite retains an unknown tail instead of fabricating the lost edge");
            }
        }
        scenarios++;
    }

    static void SharedFailurePrefixesAndExitContracts()
    {
        var actor = Node(ActorName(), null, 0, Storage()); object oldName = new object();
        var item = Node(oldName, null, 0, null); var game = GameDependencies();
        var missing = SharedClassRegistration.ApplyItem(item, actor, null);
        Need(missing.Status == "Unsupported" && missing.Reason == "UnknownDuplicateNameCallback" && missing.ParentAssigned &&
            Object.ReferenceEquals(item.Parent, actor) && Object.ReferenceEquals(item.Name, oldName) && missing.CallbacksInvoked == 0,
            "missing Game duplication retains only the initial shared parent assignment");
        var nullAllocation = SharedClassRegistration.ApplyItem(item, actor, new SharedClassRegistrationCallbacks(game.DuplicateName,
            delegate(ulong size) { return null; }, game.Free, game.RegisterExit));
        Need(nullAllocation.Reason == "UnsupportedAllocationContract" && nullAllocation.NameAssigned && actor.Count == 0 &&
            !nullAllocation.ChildAppended && !nullAllocation.StoragePublished, "Game whole-helper null is rejected without Engine retry semantics");
        var old = actor.Children;
        var noFree = SharedClassRegistration.ApplyItem(item, actor, new SharedClassRegistrationCallbacks(game.DuplicateName, game.Allocate, null, game.RegisterExit));
        Need(noFree.Reason == "UnknownClassRegistrationFreeCallback" && noFree.ChildAppended && actor.Count == 1 &&
            Object.ReferenceEquals(actor.Children, old) && !noFree.FreeInvoked && !noFree.StoragePublished, "missing shared free preserves append/count with old storage still visible");
        actor = Node(ActorName(), null, 0, Storage()); item = Node(null, null, 0, null); ulong callbackSeen = 0;
        var minus = SharedClassRegistration.ApplyItem(item, actor, new SharedClassRegistrationCallbacks(game.DuplicateName, game.Allocate, game.Free,
            delegate(ulong callback) { callbackSeen = callback; return -1; }));
        Need(minus.Status == "Known" && minus.ReturnValue == -1 && minus.OwnExitRawReturn == -1 && minus.StoragePublished &&
            callbackSeen == 0x180604450UL && actor.Count == 1, "Game whole exit minus one preserves published shared state and exact Item token");
        var bindings = NullBindings(actor, item); object value = new object();
        bindings[1] = new EngineFactoryBinding(item.FactoryIdentity, delegate { return value; });
        var snapshot = EngineRootSnapshot.Capture(actor, bindings, SnapshotLimits);
        var query = ClassFactoryDispatch.Create(snapshot.Root, ItemName());
        Need(query.Status == "Known" && Object.ReferenceEquals(query.Instance, value), "exit-registration failure does not undo a queryable Game edge");
        var quest = Node(null, item, 0, null);
        var odd = SharedClassRegistration.ApplyQuestItem(quest, new SharedClassRegistrationCallbacks(game.DuplicateName, game.Allocate, game.Free, delegate(ulong callback) { return 17; }));
        Need(odd.Status == "Unsupported" && odd.Reason == "UnsupportedExitRegistrationContract" && odd.OwnExitRawReturn == 17 &&
            !odd.ReturnValue.HasValue && odd.StoragePublished && Object.ReferenceEquals(item.Children.Slots[0], quest), "raw whole-helper value outside zero/minus-one is retained without undoing publication");
        var orphan = Node(oldName, null, -1, null); int exits = 0;
        var orphanResult = SharedClassRegistration.ApplyQuestItem(orphan, new SharedClassRegistrationCallbacks(
            delegate(byte[] literal) { return null; }, null, null, delegate(ulong callback) { callbackSeen = callback; exits++; return -1; }));
        Need(orphanResult.Status == "Known" && orphanResult.ReturnValue == -1 && orphan.Name == null && orphan.Count == -1 &&
            !orphanResult.ParentAssigned && !orphanResult.RequestedBytes.HasValue && callbackSeen == 0x180605530UL && exits == 1,
            "QuestItem with null parent stores null name then reaches its own exit token without allocation dependencies");
        var absent = SharedClassRegistration.ApplyItem(null, actor, game);
        Need(absent.Reason == "UnknownClassRegistrationNode" && !absent.MayHaveEffects && absent.CallbacksInvoked == 0,
            "null shared target fails without callback or actor mutation");
        scenarios++;
    }

    static void StableOwnDifferential()
    {
        // A few new paired inputs, not invocation of either old test suite.
        foreach (bool isItem in new bool[] { true, false }) {
            foreach (int exitValue in new int[] { 0, -1 }) {
                var sharedExisting = Node(OtherName(), null, 0, null);
                var sharedOld = Storage(sharedExisting, null);
                var sharedParent = Node(ActorName(), null, 2, sharedOld);
                var sharedNode = Node(null, sharedParent, 0, null);
                var typedExisting = new ClassRegistrationNode(OtherName(), null, 0, null, new object());
                var typedOld = new ClassRegistrationChildStorage(new ClassRegistrationNode[] { typedExisting, null });
                var typedParent = new ClassRegistrationNode(ActorName(), null, 2, typedOld, new object());
                var typedNode = new ClassRegistrationNode(null, typedParent, 0, null, new object());
                var baseGame = GameDependencies();
                var sharedCallbacks = new SharedClassRegistrationCallbacks(baseGame.DuplicateName, baseGame.Allocate, baseGame.Free, delegate(ulong callback) { return exitValue; });
                var typedCallbacks = new ClassRegistrationCallbacks(delegate(byte[] literal) { return (byte[])literal.Clone(); },
                    delegate(ulong size) {
                        if (size != 24) throw new Exception("bounded differential allocation");
                        return new ClassRegistrationChildStorage(new ClassRegistrationNode[3]);
                    }, delegate(ClassRegistrationChildStorage oldStorage) { }, delegate(ulong callback) { return exitValue; });
                var shared = isItem ? SharedClassRegistration.ApplyItem(sharedNode, sharedParent, sharedCallbacks) : SharedClassRegistration.ApplyQuestItem(sharedNode, sharedCallbacks);
                var typed = isItem ? ClassRegistrationInitialization.ApplyItem(typedNode, typedParent, typedCallbacks) : ClassRegistrationInitialization.ApplyQuestItem(typedNode, typedCallbacks);
                Need(shared.Status == "Known" && shared.Status == typed.Status && shared.Stage == typed.Stage && shared.Reason == typed.Reason &&
                    shared.ReturnValue == exitValue && shared.ReturnValue == typed.ReturnValue && shared.ExitCallbackToken == typed.ExitCallbackToken,
                    "stable shared and typed Game cases agree on completion and exit identity");
                Need(Bytes(sharedNode.Name, isItem ? ItemName() : QuestName()) && Bytes(sharedNode.Name, typedNode.Name) &&
                    shared.ParentAssigned == isItem && shared.ParentAssigned == typed.ParentAssigned, "stable paired writers preserve literal and parent-store differences");
                Need(shared.RequestedBytes == 24 && shared.RequestedBytes == typed.RequestedBytes && shared.AllocationCount == 2 &&
                    shared.CopiedChildren == 2 && shared.CopiedChildren == typed.CopiedChildren && shared.CallbacksInvoked == 4 && shared.CallbacksInvoked == typed.CallbacksInvoked,
                    "paired journals agree on bounded allocation and copy/callback counts");
                Need(sharedParent.Count == 3 && typedParent.Count == 3 && Object.ReferenceEquals(sharedParent.Children.Slots[0], sharedExisting) &&
                    sharedParent.Children.Slots[1] == null && Object.ReferenceEquals(sharedParent.Children.Slots[2], sharedNode) &&
                    Object.ReferenceEquals(typedParent.Children.Slots[0], typedExisting) && typedParent.Children.Slots[1] == null && Object.ReferenceEquals(typedParent.Children.Slots[2], typedNode),
                    "paired node representations retain ordered children and known null pointers");
                Need(Object.ReferenceEquals(shared.FreeArgument, sharedOld) && Object.ReferenceEquals(typed.FreeArgument, typedOld) &&
                    shared.StoragePublished && typed.StoragePublished && shared.FreeCompleted && typed.FreeCompleted && shared.ExitInvoked && typed.ExitInvoked,
                    "both representations free their original storage then publish before exit");
            }
        }
        scenarios++;
    }

    public static int Main()
    {
        try {
            ExplicitFiveNodeChain(); NestedEngineDuringNameAndParentReload(); NestedAllocationGrowthAndShrink();
            NestedGameDuringEngineAllocation(); RepeatedSharedEdgesAndFactories(); FreeReentryReturnAndInterruption();
            SharedFailurePrefixesAndExitContracts(); StableOwnDifferential();
            Console.WriteLine("PASS: " + assertions + " assertions, " + scenarios + " scenario groups"); return 0;
        } catch (Exception error) {
            Console.Error.WriteLine("FAIL after " + assertions + " assertions and " + scenarios + " completed groups: " + error); return 1;
        }
    }
}
