using System;
using GrimDynasty.TaskItems;

// Bounded own state/callback fixtures only: no files or original/native calls.
// Saved Engine 274EC0 supplies traversal order. Fixed own factory bindings are
// an explicit covered domain, not a claim that native +28 cannot change.
class EngineDynamicFactoryDispatchTests
{
    static int assertions, scenarios;
    static readonly EngineDynamicFactoryLimits Limits = new EngineDynamicFactoryLimits(32, 16, 256);
    static readonly EngineSnapshotLimits SnapshotLimits = new EngineSnapshotLimits(16, 32, 256, 16);
    static void Need(bool value, string why) { assertions++; if (!value) throw new Exception(why); }
    static byte[] Text(byte letter) { return new byte[] { letter, 0 }; }
    static byte[] ActorName() { return new byte[] { 65, 99, 116, 111, 114, 0 }; }
    static byte[] ItemName() { return new byte[] { 73, 116, 101, 109, 0 }; }
    static EngineRootChildStorage Storage(params EngineRootNode[] nodes) { return new EngineRootChildStorage(nodes); }
    static EngineRootNode Node(object name, int count, EngineRootChildStorage children)
    {
        return new EngineRootNode(name, null, count, children, new object());
    }
    static EngineFactoryBinding Bind(EngineRootNode node, Func<object> callback) { return new EngineFactoryBinding(node.FactoryIdentity, callback); }
    static EngineDynamicFactoryResult Live(EngineRootNode root, byte[] query, params EngineFactoryBinding[] bindings)
    {
        return EngineDynamicFactoryDispatch.Create(root, query, bindings, Limits);
    }
    static SharedClassRegistrationCallbacks GameDependencies(Action<EngineRootChildStorage> free, Func<ulong, int> exit)
    {
        return new SharedClassRegistrationCallbacks(delegate(byte[] literal) { return (byte[])literal.Clone(); },
            delegate(ulong size) {
                if (size > 32 || size % 8 != 0) throw new Exception("independent small fixture allocation ceiling");
                return Storage(new EngineRootNode[(int)(size / 8)]);
            }, free, exit);
    }

    static void StableComparisonAndByteLaziness()
    {
        var child = Node(Text(65), 0, null); var root = Node(Text(82), 1, Storage(child));
        object rootValue = new object(), childValue = new object();
        var bindings = new EngineFactoryBinding[] { Bind(root, delegate { return rootValue; }), Bind(child, delegate { return childValue; }) };
        var snapshot = EngineRootSnapshot.Capture(root, bindings, SnapshotLimits);
        Need(snapshot.Status == "Captured" && !snapshot.HasUnknownMetadata, "new stable comparison starts from complete explicit own metadata");
        byte[][] queries = { Text(82), Text(65), Text(90) }; int[] pairs = { 2, 3, 2 };
        for (int i = 0; i < queries.Length; i++) {
            var live = Live(root, queries[i], bindings); var frozen = ClassFactoryDispatch.Create(snapshot.Root, queries[i]);
            Need(live.Status == "Known" && live.Status == frozen.Status && Object.ReferenceEquals(live.Instance, frozen.Instance) &&
                live.VisitedNodes == frozen.VisitedNodes && live.CallbacksInvoked == frozen.CallbacksInvoked,
                "unchanged small state agrees with the old snapshot traversal without running its suite");
            Need(live.ComparedBytes == pairs[i] && live.MayHaveCallbackSideEffects == (live.CallbacksInvoked != 0),
                "stable queries count only consumed byte pairs and started callbacks");
        }
        object rawValue = new object(); var raw = Node(new byte[] { 0x80, 0xff, 0, 0x11 }, 0, null);
        var equal = Live(raw, new byte[] { 0x80, 0xff, 0, 0x22 }, Bind(raw, delegate { return rawValue; }));
        Need(equal.Status == "Known" && Object.ReferenceEquals(equal.Instance, rawValue) && equal.ComparedBytes == 3,
            "high bytes compare exactly and an equal NUL ends before differing suffix bytes");
        var shortName = Node(new byte[] { 65 }, 0, null);
        var mismatch = Live(shortName, Text(66));
        Need(mismatch.Status == "Known" && mismatch.Instance == null && mismatch.ComparedBytes == 1 && mismatch.CallbacksInvoked == 0,
            "earlier mismatch does not require a missing NUL or an unused factory binding");
        var incomplete = Live(shortName, Text(65));
        Need(incomplete.Status == "Unsupported" && incomplete.Reason == "ClassFactoryNameBytesIncomplete" && incomplete.ComparedBytes == 1,
            "equal bytes without a required terminator are unknown rather than silently repaired");
        var shortQuery = Live(Node(Text(65), 0, null), new byte[] { 65 });
        Need(shortQuery.Reason == "ClassFactoryNameBytesIncomplete" && shortQuery.ComparedBytes == 1, "query span is checked lazily after its consumed prefix");
        var missingQuery = Live(root, null, bindings);
        Need(missingQuery.Reason == "UnknownRequestedClassName" && missingQuery.VisitedNodes == 0 && missingQuery.ComparedBytes == 0,
            "missing query fails before traversal");
        scenarios++;
    }

    static void GameAppendDuringFactoryAndSnapshotContrast()
    {
        foreach (bool dynamic in new bool[] { true, false }) {
            var first = Node(ItemName(), 0, null); var actor = Node(ActorName(), 1, Storage(first)); first.Parent = actor;
            var added = Node(null, 0, null); var original = actor.Children; object answer = new object();
            int firstCalls = 0, addedCalls = 0, frees = 0; ulong exitToken = 0; SharedClassRegistrationResult writer = null;
            var game = GameDependencies(delegate(EngineRootChildStorage old) { if (!Object.ReferenceEquals(old, original)) throw new Exception("wrong live old storage"); frees++; },
                delegate(ulong token) { exitToken = token; return -1; });
            var bindings = new EngineFactoryBinding[] {
                Bind(actor, delegate { throw new Exception("ancestor does not match Item"); }),
                Bind(first, delegate {
                    if (++firstCalls > 1) throw new Exception("independent append callback ceiling");
                    writer = SharedClassRegistration.ApplyItem(added, actor, game); return null;
                }),
                Bind(added, delegate { if (++addedCalls > 1) throw new Exception("independent added callback ceiling"); return answer; })
            };
            // The not-yet-reachable added record has an unknown name. Live lookup
            // must not prewalk it just because its future factory binding exists.
            var before = EngineRootSnapshot.Capture(actor, bindings, SnapshotLimits);
            Need(before.Status == "Captured" && before.Nodes == 2 && !before.HasUnknownMetadata && added.Name == null,
                "initial frozen view contains only the old published edge");
            EngineDynamicFactoryResult live = null; ClassFactoryResult frozen = null;
            if (dynamic) live = Live(actor, ItemName(), bindings); else frozen = ClassFactoryDispatch.Create(before.Root, ItemName());
            Need(writer.Status == "Known" && writer.ReturnValue == -1 && writer.StoragePublished && writer.FreeCompleted &&
                actor.Count == 2 && Object.ReferenceEquals(actor.Children.Slots[0], first) && Object.ReferenceEquals(actor.Children.Slots[1], added),
                "first factory publishes a real shared Game edge despite normal exit-registration minus one");
            Need(firstCalls == 1 && frees == 1 && exitToken == 0x180604450UL && Object.ReferenceEquals(writer.SelectedParent, actor),
                "the callback uses the independent Game whole-helper contract and exact parent identity");
            if (dynamic) {
                Need(live.Status == "Known" && Object.ReferenceEquals(live.Instance, answer) && live.VisitedNodes == 3 && live.CallbacksInvoked == 2 &&
                    live.ComparedBytes == 11 && addedCalls == 1, "live null continuation rereads count and reaches the newly published next child in the same query");
            } else {
                Need(frozen.Status == "Known" && frozen.Instance == null && frozen.VisitedNodes == 2 && frozen.CallbacksInvoked == 1 && addedCalls == 0,
                    "old snapshot intentionally keeps its original count/slots despite identical callback mutation");
            }
        }
        scenarios++;
    }

    static void CurrentCountStorageAndSlot()
    {
        foreach (int afterCount in new int[] { 0, -7 }) {
            var first = Node(Text(65), 0, null); var poison = Node(null, 0, null); var root = Node(Text(82), 2, Storage(first, poison));
            int calls = 0;
            var result = Live(root, Text(65), Bind(first, delegate {
                if (++calls > 1) throw new Exception("independent shrink ceiling");
                root.Count = afterCount; root.Children = null; root.Name = null; return null;
            }));
            Need(result.Status == "Known" && result.Instance == null && result.VisitedNodes == 2 && result.CallbacksInvoked == 1 && result.ComparedBytes == 3,
                "after null, zero or negative current count terminates before rereading poisoned storage/name");
            Need(calls == 1 && root.Count == afterCount && root.Children == null && root.Name == null && result.MayHaveCallbackSideEffects,
                "termination preserves the exact callback mutations");
        }
        foreach (bool replaceStorage in new bool[] { true, false }) {
            var first = Node(Text(65), 0, null); var target = Node(Text(65), 0, null); var poison = Node(null, 0, null);
            var initial = replaceStorage ? Storage(first) : Storage(first, poison);
            var root = Node(Text(82), replaceStorage ? 1 : 2, initial); object answer = new object(); int firstCalls = 0, targetCalls = 0, rootCalls = 0;
            var result = Live(root, Text(65), Bind(root, delegate { rootCalls++; throw new Exception("active ancestor must not rematch"); }),
                Bind(first, delegate {
                    if (++firstCalls > 1) throw new Exception("independent replacement ceiling");
                    root.Name = Text(65); root.Count = 2;
                    if (replaceStorage) root.Children = Storage(poison, target); else initial.Slots[1] = target;
                    return null;
                }), Bind(target, delegate { targetCalls++; return answer; }));
            Need(result.Status == "Known" && Object.ReferenceEquals(result.Instance, answer) && result.VisitedNodes == 3 && result.CallbacksInvoked == 2,
                "next iteration reloads the current storage and slot at index one");
            Need(firstCalls == 1 && targetCalls == 1 && rootCalls == 0 && result.ComparedBytes == 5,
                "index does not restart and the active parent frame does not compare its changed name again");
        }
        scenarios++;
    }

    static void QueryReferenceNameRereadsAndFixedBindings()
    {
        byte[] query = { 111, 110, 101, 0 }; byte[] sameQuery = query;
        var a = Node(new byte[] { 111, 110, 101, 0 }, 0, null);
        var b = Node(new byte[] { 111, 108, 100, 0 }, 0, null);
        var c = Node(new byte[] { 116, 119, 111, 0 }, 0, null);
        var root = Node(Text(82), 4, Storage(a, b, b, c));
        int aCalls = 0, bCalls = 0, cCalls = 0, replacementCalls = 0; object answer = new object();
        EngineFactoryBinding[] bindings = null;
        bindings = new EngineFactoryBinding[] {
            Bind(a, delegate {
                if (++aCalls > 1) throw new Exception("independent A ceiling");
                query[0] = 116; query[1] = 119; query[2] = 111;
                b.Name = new byte[] { 116, 119, 111, 0 };
                bindings[1] = Bind(b, delegate { replacementCalls++; throw new Exception("fixed binding replaced"); });
                bindings[2] = null; return null;
            }),
            Bind(b, delegate {
                if (++bCalls > 1) throw new Exception("changed repeated name must no longer match");
                b.Name = new byte[] { 122, 122, 122, 0 }; return null;
            }),
            Bind(c, delegate { cCalls++; return answer; })
        };
        var result = Live(root, query, bindings);
        Need(result.Status == "Known" && Object.ReferenceEquals(result.Instance, answer) && result.VisitedNodes == 5 && result.CallbacksInvoked == 3,
            "same query buffer changes subsequent comparisons; each repeated node visit reloads its current name");
        Need(aCalls == 1 && bCalls == 1 && cCalls == 1 && replacementCalls == 0 && result.ComparedBytes == 14,
            "only A/B/C callbacks run; the second B edge is revisited and mismatches on its first changed byte");
        Need(Object.ReferenceEquals(query, sameQuery) && query[0] == 116 && query[1] == 119 && query[2] == 111 && query[3] == 0,
            "live traversal neither clones nor repairs the supplied query bytes");
        Need(bindings[2] == null && ((byte[])b.Name)[0] == 122 && result.MayHaveCallbackSideEffects,
            "callback mutations remain visible while the per-call identity map stays fixed");
        var duplicate = Node(Text(65), 0, null); var repeatedRoot = Node(Text(82), 2, Storage(duplicate, duplicate));
        int repeatedCalls = 0; object repeatedAnswer = new object();
        var repeated = Live(repeatedRoot, Text(65), Bind(duplicate, delegate {
            if (++repeatedCalls > 2) throw new Exception("independent duplicate ceiling"); return repeatedCalls == 2 ? repeatedAnswer : null;
        }));
        Need(repeated.Status == "Known" && Object.ReferenceEquals(repeated.Instance, repeatedAnswer) && repeated.VisitedNodes == 3 &&
            repeated.CallbacksInvoked == 2 && repeatedCalls == 2 && repeated.ComparedBytes == 5, "duplicate edges call the same fixed factory twice without caching its null result");
        scenarios++;
    }

    static void SelfMatchAndImmediateUnwind()
    {
        var self = Node(Text(65), int.MaxValue, null); int selfCalls = 0;
        var ownNull = Live(self, Text(65), Bind(self, delegate { selfCalls++; return null; }));
        Need(ownNull.Status == "Known" && ownNull.Instance == null && ownNull.VisitedNodes == 1 && ownNull.CallbacksInvoked == 1 && selfCalls == 1,
            "matching self returns its null factory result without inspecting unknown own children");
        var first = Node(Text(65), 1, null); var second = Node(Text(65), 0, null);
        var root = Node(Text(82), 2, Storage(first, second)); object answer = new object(); byte[] query = Text(65); int secondCalls = 0;
        var nonnull = Live(root, query, Bind(first, delegate {
            root.Name = null; root.Count = int.MaxValue; root.Children = null;
            first.Name = null; first.Count = int.MaxValue; first.Children = Storage(first);
            query[0] = 0xff; return answer;
        }), Bind(second, delegate { secondCalls++; throw new Exception("unwound sibling visited"); }));
        Need(nonnull.Status == "Known" && Object.ReferenceEquals(nonnull.Instance, answer) && nonnull.VisitedNodes == 2 &&
            nonnull.CallbacksInvoked == 1 && nonnull.ComparedBytes == 3 && secondCalls == 0, "nonnull child result unwinds before rechecking poisoned parent or matching-node metadata");
        Need(query[0] == 0xff && root.Children == null && root.Name == null && nonnull.MayHaveCallbackSideEffects,
            "successful unwind does not roll back the factory's mutations");
        var grandchild = Node(Text(65), 0, null); first = Node(Text(65), 1, Storage(grandchild)); second = Node(Text(65), 0, null);
        root = Node(Text(82), 2, Storage(first, second)); int grandCalls = 0, firstCalls = 0;
        var nextSibling = Live(root, Text(65), Bind(first, delegate { firstCalls++; return null; }),
            Bind(grandchild, delegate { grandCalls++; throw new Exception("self-match null descended"); }), Bind(second, delegate { return answer; }));
        Need(nextSibling.Status == "Known" && Object.ReferenceEquals(nextSibling.Instance, answer) && nextSibling.VisitedNodes == 3 && nextSibling.CallbacksInvoked == 2,
            "a self-match null returns to its parent's next sibling rather than searching its own child");
        Need(firstCalls == 1 && grandCalls == 0, "self-match bypasses a known matching descendant as well as unknown descendants");
        var negative = Live(Node(Text(82), -1, null), Text(65));
        Need(negative.Status == "Known" && negative.Instance == null && negative.ComparedBytes == 1, "negative count on mismatch skips unavailable child storage");
        scenarios++;
    }

    sealed class EqualIdentity
    {
        public override bool Equals(object other) { return other is EqualIdentity; }
        public override int GetHashCode() { return 7; }
    }

    static void LazyUnknownsBindingsAndFaultPrefixes()
    {
        var matched = Node(Text(65), 0, null);
        var missing = Live(matched, Text(65));
        Need(missing.Reason == "UnknownClassFactoryCallback" && missing.VisitedNodes == 1 && missing.CallbacksInvoked == 0 && missing.ComparedBytes == 2,
            "missing binding becomes unknown only when the node name matches");
        var mismatch = Live(matched, Text(66));
        Need(mismatch.Status == "Known" && mismatch.Instance == null && mismatch.CallbacksInvoked == 0, "unmatched leaf needs no factory binding");
        var nullCallback = Live(matched, Text(65), Bind(matched, null));
        Need(nullCallback.Reason == "UnknownClassFactoryCallback" && !nullCallback.MayHaveCallbackSideEffects,
            "an explicit missing callback is not silently replaced with a callback returning null");
        var first = Node(Text(65), 0, null); var unknown = Node(new object(), 0, null); var root = Node(Text(82), 2, Storage(first, unknown));
        int effect = 0;
        var lateUnknown = Live(root, Text(65), Bind(first, delegate { effect++; return null; }));
        Need(lateUnknown.Reason == "ClassFactoryNameBytesIncomplete" && lateUnknown.VisitedNodes == 3 && lateUnknown.CallbacksInvoked == 1 &&
            lateUnknown.ComparedBytes == 3 && effect == 1 && lateUnknown.MayHaveCallbackSideEffects,
            "a later unknown name preserves the callback prefix instead of triggering an eager prewalk failure");
        root = Node(Text(82), 1, Storage((EngineRootNode)null));
        var nullChild = Live(root, Text(65));
        Need(nullChild.Reason == "UnknownClassInfoSnapshot" && nullChild.VisitedNodes == 2 && nullChild.ComparedBytes == 1,
            "known null child pointer is visited and differs from an unavailable child slot");
        root.Children = Storage();
        var absentSlot = Live(root, Text(65));
        Need(absentSlot.Reason == "UnknownClassFactoryChildSlot" && absentSlot.VisitedNodes == 1 && absentSlot.ComparedBytes == 1,
            "short storage stops before inventing a child visit");
        root = Node(Text(82), 1, Storage(first)); effect = 0;
        var thrown = Live(root, Text(65), Bind(first, delegate {
            effect++; root.Count = 0; root.Children = null; throw new ApplicationException("own callback fault");
        }));
        Need(thrown.Reason == "ClassFactoryCallbackThrew:System.ApplicationException" && thrown.VisitedNodes == 2 && thrown.CallbacksInvoked == 1 &&
            thrown.ComparedBytes == 3 && thrown.MayHaveCallbackSideEffects && effect == 1 && root.Count == 0 && root.Children == null,
            "own callback exception immediately returns with all started callback effects intact");
        object identity = matched.FactoryIdentity;
        var duplicate = Live(matched, Text(65), new EngineFactoryBinding(identity, delegate { return null; }), new EngineFactoryBinding(identity, null));
        Need(duplicate.Reason == "DuplicateClassFactoryBindingIdentity" && duplicate.VisitedNodes == 0 && duplicate.CallbacksInvoked == 0,
            "duplicate reference bindings fail before lookup rather than picking an arbitrary callback");
        var invalid = Live(matched, Text(65), new EngineFactoryBinding(null, delegate { return null; }));
        Need(invalid.Reason == "InvalidClassFactoryBinding" && invalid.VisitedNodes == 0, "null binding identity is an explicit configuration failure");
        var oneId = new EqualIdentity(); var twoId = new EqualIdentity();
        var one = new EngineRootNode(Text(82), null, 1, null, oneId); var two = new EngineRootNode(Text(65), null, 0, null, twoId); one.Children = Storage(two);
        object answer = new object(); int wrongCalls = 0;
        var references = Live(one, Text(65), new EngineFactoryBinding(oneId, delegate { wrongCalls++; return null; }), new EngineFactoryBinding(twoId, delegate { return answer; }));
        Need(oneId.Equals(twoId) && !Object.ReferenceEquals(oneId, twoId) && references.Status == "Known" && Object.ReferenceEquals(references.Instance, answer) && wrongCalls == 0,
            "equal-valued objects remain distinct fixed factory identities");
        scenarios++;
    }

    static void BudgetsCyclesAndFiniteAncestorRevisit()
    {
        var cycle = Node(Text(82), 1, null); cycle.Children = Storage(cycle);
        var visit = EngineDynamicFactoryDispatch.Create(cycle, Text(65), null, new EngineDynamicFactoryLimits(3, 8, 32));
        Need(visit.Reason == "ClassFactoryVisitBudgetOutsideCoveredDomain" && visit.VisitedNodes == 3 && visit.ComparedBytes == 3 && visit.CallbacksInvoked == 0,
            "cyclic mismatch revisits until the own visit cap rather than a cycle or deduplication rule");
        var depth = EngineDynamicFactoryDispatch.Create(cycle, Text(65), null, new EngineDynamicFactoryLimits(8, 2, 32));
        Need(depth.Reason == "ClassFactoryDepthOutsideCoveredDomain" && depth.VisitedNodes == 2 && depth.ComparedBytes == 2,
            "independent own depth cap stops before the next node visit");
        var first = Node(Text(65), 0, null); var ancestor = Node(Text(82), 2, null); ancestor.Children = Storage(first, ancestor);
        int firstCalls = 0, ancestorCalls = 0; object answer = new object();
        var finite = Live(ancestor, Text(65), Bind(first, delegate {
            if (++firstCalls > 1) throw new Exception("independent ancestor mutation ceiling"); ancestor.Name = Text(65); return null;
        }), Bind(ancestor, delegate { if (++ancestorCalls > 1) throw new Exception("independent ancestor return ceiling"); return answer; }));
        Need(finite.Status == "Known" && Object.ReferenceEquals(finite.Instance, answer) && finite.VisitedNodes == 3 && finite.CallbacksInvoked == 2 &&
            finite.ComparedBytes == 5 && firstCalls == 1 && ancestorCalls == 1, "a later edge may revisit the active ancestor and match its changed name with finite success");
        var name = Node(new byte[] { 65, 66, 67, 0 }, 0, null); int calls = 0;
        var bindings = new EngineFactoryBinding[] { Bind(name, delegate { calls++; return answer; }) };
        var byteStop = EngineDynamicFactoryDispatch.Create(name, new byte[] { 65, 66, 67, 0 }, bindings, new EngineDynamicFactoryLimits(4, 4, 3));
        Need(byteStop.Reason == "ClassFactoryByteComparisonBudgetOutsideCoveredDomain" && byteStop.ComparedBytes == 3 && byteStop.VisitedNodes == 1 && calls == 0,
            "byte budget stops before consuming a fourth required pair or invoking the matching factory");
        var byteExact = EngineDynamicFactoryDispatch.Create(name, new byte[] { 65, 66, 67, 0 }, bindings, new EngineDynamicFactoryLimits(4, 4, 4));
        Need(byteExact.Status == "Known" && Object.ReferenceEquals(byteExact.Instance, answer) && byteExact.ComparedBytes == 4 && calls == 1,
            "exact pair budget includes the equal terminating NUL");
        var onePair = EngineDynamicFactoryDispatch.Create(name, Text(90), null, new EngineDynamicFactoryLimits(1, 1, 1));
        Need(onePair.Status == "Known" && onePair.ComparedBytes == 1 && onePair.Instance == null, "first mismatch needs no byte beyond the one-pair budget");
        var zeroBytes = EngineDynamicFactoryDispatch.Create(name, Text(65), null, new EngineDynamicFactoryLimits(1, 1, 0));
        Need(zeroBytes.Reason == "ClassFactoryByteComparisonBudgetOutsideCoveredDomain" && zeroBytes.VisitedNodes == 1 && zeroBytes.ComparedBytes == 0,
            "zero byte budget admits a visit but no byte pair");
        var zeroVisits = EngineDynamicFactoryDispatch.Create(name, Text(65), null, new EngineDynamicFactoryLimits(0, 1, 8));
        var zeroDepth = EngineDynamicFactoryDispatch.Create(name, Text(65), null, new EngineDynamicFactoryLimits(1, 0, 8));
        Need(zeroVisits.Reason == "ClassFactoryVisitBudgetOutsideCoveredDomain" && zeroVisits.VisitedNodes == 0 &&
            zeroDepth.Reason == "ClassFactoryDepthOutsideCoveredDomain" && zeroDepth.VisitedNodes == 0,
            "zero visit/depth budgets are valid limits with no admitted root visit");
        first = Node(Text(65), 0, null); var root = Node(Text(82), 2, Storage(first, first)); int effects = 0;
        var prefix = EngineDynamicFactoryDispatch.Create(root, Text(65), new EngineFactoryBinding[] { Bind(first, delegate {
            if (++effects > 3) throw new Exception("independent budget callback ceiling"); first.Parent = root; return null;
        }) }, new EngineDynamicFactoryLimits(2, 8, 32));
        Need(prefix.Reason == "ClassFactoryVisitBudgetOutsideCoveredDomain" && prefix.VisitedNodes == 2 && prefix.CallbacksInvoked == 1 &&
            prefix.ComparedBytes == 3 && effects == 1 && Object.ReferenceEquals(first.Parent, root) && prefix.MayHaveCallbackSideEffects,
            "visit cut retains the earlier null callback's effects and does not invoke the duplicate edge");
        var invalid = EngineDynamicFactoryDispatch.Create(name, Text(65), null, new EngineDynamicFactoryLimits(4097, 1, 1));
        Need(invalid.Reason == "ClassFactoryTraversalLimitsOutsideCoveredDomain" && invalid.VisitedNodes == 0 && invalid.ComparedBytes == 0,
            "invalid own limits are rejected before traversal");
        scenarios++;
    }

    static void ReentrantGameFreeFailureAndSnapshotContrast()
    {
        // mode 0 live/null; mode 1 frozen/null; mode 2 live/non-null.
        for (int mode = 0; mode < 3; mode++) {
            var first = Node(ItemName(), 0, null); var actor = Node(ActorName(), 1, Storage(first)); first.Parent = actor;
            var added = Node(null, 0, null); var original = actor.Children; object answer = new object();
            int factoryCalls = 0, freeCalls = 0, exitCalls = 0; bool freeSawPrefix = false; SharedClassRegistrationResult writer = null;
            var game = GameDependencies(delegate(EngineRootChildStorage old) {
                if (++freeCalls > 1) throw new Exception("independent free ceiling");
                freeSawPrefix = Object.ReferenceEquals(old, original) && Object.ReferenceEquals(actor.Children, original) && actor.Count == 2;
                throw new ApplicationException("own interrupted Game free");
            }, delegate(ulong token) { exitCalls++; return 0; });
            var bindings = new EngineFactoryBinding[] { Bind(first, delegate {
                if (++factoryCalls > 1) throw new Exception("independent Game failure callback ceiling");
                writer = SharedClassRegistration.ApplyItem(added, actor, game); return mode == 2 ? answer : null;
            }) };
            var before = EngineRootSnapshot.Capture(actor, bindings, SnapshotLimits);
            EngineDynamicFactoryResult live = null; ClassFactoryResult frozen = null;
            if (mode == 1) frozen = ClassFactoryDispatch.Create(before.Root, ItemName()); else live = Live(actor, ItemName(), bindings);
            Need(writer.Status == "Unsupported" && writer.Reason == "OwnClassRegistrationOperationThrew:System.ApplicationException" &&
                writer.ChildAppended && writer.CountIncremented && writer.FreeInvoked && !writer.FreeCompleted && !writer.StoragePublished && !writer.ExitInvoked,
                "reentrant Game free interruption retains its precise append/count prefix");
            Need(freeSawPrefix && factoryCalls == 1 && freeCalls == 1 && exitCalls == 0 && actor.Count == 2 && Object.ReferenceEquals(actor.Children, original) && original.Slots.Length == 1,
                "shared Actor exposes incremented count with old one-slot storage after the callback");
            Need(Object.ReferenceEquals(writer.AllocatedStorage.Slots[0], first) && Object.ReferenceEquals(writer.AllocatedStorage.Slots[1], added) &&
                !Object.ReferenceEquals(actor.Children, writer.AllocatedStorage), "unpublished destination is preserved as evidence but is not traversal storage");
            if (mode == 0) {
                Need(live.Status == "Unsupported" && live.Reason == "UnknownClassFactoryChildSlot" && live.VisitedNodes == 2 &&
                    live.CallbacksInvoked == 1 && live.ComparedBytes == 6 && live.MayHaveCallbackSideEffects,
                    "live null continuation observes the count/storage mismatch and stops at the unknown next slot");
            } else if (mode == 1) {
                Need(frozen.Status == "Known" && frozen.Instance == null && frozen.VisitedNodes == 2 && frozen.CallbacksInvoked == 1,
                    "frozen contrast stops at its original count and does not pretend to observe the live failure prefix");
            } else {
                Need(live.Status == "Known" && Object.ReferenceEquals(live.Instance, answer) && live.VisitedNodes == 2 && live.CallbacksInvoked == 1 && live.ComparedBytes == 6,
                    "nonnull factory answer unwinds immediately even though its nested writer left an incomplete parent span");
            }
        }
        scenarios++;
    }

    public static int Main()
    {
        try {
            StableComparisonAndByteLaziness(); GameAppendDuringFactoryAndSnapshotContrast(); CurrentCountStorageAndSlot();
            QueryReferenceNameRereadsAndFixedBindings(); SelfMatchAndImmediateUnwind(); LazyUnknownsBindingsAndFaultPrefixes();
            BudgetsCyclesAndFiniteAncestorRevisit(); ReentrantGameFreeFailureAndSnapshotContrast();
            Console.WriteLine("PASS: " + assertions + " assertions, " + scenarios + " scenario groups"); return 0;
        } catch (Exception error) {
            Console.Error.WriteLine("FAIL after " + assertions + " assertions and " + scenarios + " completed groups: " + error); return 1;
        }
    }
}
