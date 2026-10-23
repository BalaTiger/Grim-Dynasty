using System;
using GrimDynasty.TaskItems;

// Bounded own storage fixtures only. No files, native execution or factory calls.
// This local prefix is not a complete Item/Actor/QuestItem constructor.
class ItemConstructor858PrefixTests
{
    static int assertions, scenarios;
    static void Need(bool value, string why) { assertions++; if (!value) throw new Exception(why); }
    static bool ZeroHelper(ItemConstructor858Subobject value)
    {
        return value.Raw858 == 0UL && value.Raw860 == 0UL && value.Raw868 == 0UL;
    }
    static bool ScalarGolden(ItemScalarInitializationState value)
    {
        return value.Raw870 == 0UL && value.Raw878 == 0UL && value.Raw880 == 0UL &&
            value.Raw88C == 1UL && value.Raw894 == 0UL && value.Raw89C == 0U;
    }
    static ItemScalarInitializationState Seeded(uint untouched, uint replica)
    {
        return new ItemScalarInitializationState(0xffffffffffffffffUL, 0x8000000000000001UL,
            0x1122334455667788UL, 0xffffffff00000000UL, 0x8877665544332211UL,
            0xaabbccddU, untouched, replica);
    }
    static bool StillSeeded(ItemScalarInitializationState value)
    {
        return value.Raw870 == 0xffffffffffffffffUL && value.Raw878 == 0x8000000000000001UL &&
            value.Raw880 == 0x1122334455667788UL && value.Raw88C == 0xffffffff00000000UL &&
            value.Raw894 == 0x8877665544332211UL && value.Raw89C == 0xaabbccddU;
    }

    static void UnknownStorageBecomesKnownPrefix()
    {
        Need(ItemConstructor858Prefix.EntryInstructionAddress == 0x180323702UL &&
            ItemConstructor858Prefix.HelperEntryAddress == 0x1801fa340UL &&
            ItemConstructor858Prefix.ZeroCalleeAddress == 0x180029bf0UL &&
            ItemConstructor858Prefix.NextInstructionAddress == 0x180323743UL,
            "fixed evidence addresses describe this prefix and its next unmodeled instruction");
        var subobject = new ItemConstructor858Subobject(); var scalars = new ItemScalarInitializationState();
        Need(!subobject.Raw858.HasValue && !subobject.Raw860.HasValue && !subobject.Raw868.HasValue && !subobject.Untouched850.HasValue,
            "new unknown subobject fields are not silently treated as zero");
        Need(!scalars.Raw870.HasValue && !scalars.Raw878.HasValue && !scalars.Raw880.HasValue &&
            !scalars.Raw88C.HasValue && !scalars.Raw894.HasValue && !scalars.Raw89C.HasValue,
            "scalar input begins as explicitly unknown storage");
        var result = ItemConstructor858Prefix.Apply(subobject, scalars);
        Need(result.Status == "Known" && result.Reason == null && Object.ReferenceEquals(result.Subobject, subobject) &&
            Object.ReferenceEquals(result.Scalars, scalars), "known local completion retains both exact supplied state identities");
        Need(result.HelperCompleted && Object.ReferenceEquals(result.HelperReturnedSubobject, subobject) &&
            result.HelperStoreInstructions == 6 && result.ScalarStoreInstructions == 6,
            "helper returns the same subobject after six stores, followed by six scalar stores");
        Need(ZeroHelper(subobject), "all three helper qwords become known zero");
        Need(ScalarGolden(scalars) && scalars.Gender874 == 0U && scalars.Stack88C == 1U && scalars.Word890 == 0U && scalars.Word898 == 0U,
            "independent scalar goldens include the full qword overlaps");
        Need(result.MayHaveEffects && !result.IsCompleteConstructor && result.NextInstructionAddress == 0x180323743UL,
            "local success ends at the exact continuation without claiming a complete constructor");
        Need(!subobject.Untouched850.HasValue && !scalars.Untouched888.HasValue && !scalars.ReplicaStack6B0.HasValue,
            "unknown neighboring fields remain unknown after both segments");
        scenarios++;
    }

    static void NonzeroStorageAndExactAliases()
    {
        var subobject = new ItemConstructor858Subobject(0x0123456789abcdefUL, ulong.MaxValue, 0x8000000000000000UL, 0xfeedfacecafebeefUL);
        var scalars = Seeded(0x10203040U, 0x50607080U);
        var subAlias = subobject; var scalarAlias = scalars;
        Need(subAlias.Raw858 == 0x0123456789abcdefUL && subAlias.Raw860 == ulong.MaxValue && subAlias.Raw868 == 0x8000000000000000UL &&
            StillSeeded(scalarAlias) && scalarAlias.Word890 == uint.MaxValue,
            "nonzero fixture exposes stale high bits before initialization");
        var result = ItemConstructor858Prefix.Apply(subobject, scalars);
        Need(result.Status == "Known" && Object.ReferenceEquals(result.Subobject, subAlias) &&
            Object.ReferenceEquals(result.HelperReturnedSubobject, subAlias) && Object.ReferenceEquals(result.Scalars, scalarAlias),
            "no replacement or copy-back state is substituted for existing aliases");
        Need(ZeroHelper(subAlias) && result.HelperStoreInstructions == 6, "existing subobject alias observes all six helper writes");
        Need(ScalarGolden(scalarAlias) && scalarAlias.Gender874 == 0U && scalarAlias.Stack88C == 1U && scalarAlias.Word890 == 0U && scalarAlias.Word898 == 0U,
            "scalar alias observes zeroed high halves rather than only low-dword assignment");
        Need(subAlias.Untouched850 == 0xfeedfacecafebeefUL && scalarAlias.Untouched888 == 0x10203040U && scalarAlias.ReplicaStack6B0 == 0x50607080U,
            "the exact +850, +888 and +6B0 sentinels are outside this prefix's writes");
        scenarios++;
    }

    static void MissingScalarsKeepCompletedHelper()
    {
        var subobject = new ItemConstructor858Subobject(7UL, 8UL, 9UL, 0x1111222233334444UL);
        var result = ItemConstructor858Prefix.Apply(subobject, null);
        Need(result.Status == "Unsupported" && result.Reason == "UnknownItemScalarInitializationState",
            "missing scalar state stops only after the helper stage");
        Need(Object.ReferenceEquals(result.Subobject, subobject) && result.Scalars == null,
            "failure report retains the known subobject and missing scalar identity");
        Need(result.HelperCompleted && result.HelperStoreInstructions == 6 && Object.ReferenceEquals(result.HelperReturnedSubobject, subobject),
            "normal helper return is retained when the following scalar stage cannot begin");
        Need(ZeroHelper(subobject), "completed helper writes survive the missing-scalar boundary");
        Need(result.ScalarStoreInstructions == 0 && result.NextInstructionAddress == 0x180323714UL && result.MayHaveEffects && !result.IsCompleteConstructor,
            "failure cursor stops before scalar initialization and does not claim rollback or full construction");
        Need(subobject.Untouched850 == 0x1111222233334444UL, "failure prefix also preserves the neighboring qword");
        scenarios++;
    }

    static void MissingSubobjectDoesNotTouchScalars()
    {
        var scalars = Seeded(0x12345678U, 0x87654321U);
        var result = ItemConstructor858Prefix.Apply(null, scalars);
        Need(result.Status == "Unsupported" && result.Reason == "UnknownItem858Subobject", "unknown subobject stops at the entry boundary");
        Need(result.Subobject == null && result.HelperReturnedSubobject == null && Object.ReferenceEquals(result.Scalars, scalars),
            "no helper return is fabricated while scalar identity remains available in the report");
        Need(StillSeeded(scalars), "an unknown earlier subobject cannot trigger later scalar stores");
        Need(scalars.Untouched888 == 0x12345678U && scalars.ReplicaStack6B0 == 0x87654321U,
            "unreached scalar state keeps both untouched neighboring values");
        Need(!result.HelperCompleted && result.HelperStoreInstructions == 0 && result.ScalarStoreInstructions == 0 &&
            !result.MayHaveEffects && !result.IsCompleteConstructor && result.NextInstructionAddress == 0x180323702UL,
            "early failure has zero stores, no effects and the exact entry cursor");
        var bothMissing = ItemConstructor858Prefix.Apply(null, null);
        Need(bothMissing.Reason == "UnknownItem858Subobject" && bothMissing.HelperStoreInstructions == 0 && bothMissing.ScalarStoreInstructions == 0 &&
            !bothMissing.HelperCompleted && !bothMissing.MayHaveEffects && bothMissing.NextInstructionAddress == 0x180323702UL,
            "when both inputs are missing, the first required subobject remains the failure boundary");
        scenarios++;
    }

    static void RepetitionAndIndependentObjects()
    {
        var a = new ItemConstructor858Subobject(1UL, 2UL, 3UL, 10UL); var aScalars = Seeded(11U, 12U);
        var b = new ItemConstructor858Subobject(4UL, 5UL, 6UL, 20UL); var bScalars = Seeded(21U, 22U);
        var first = ItemConstructor858Prefix.Apply(a, aScalars);
        Need(first.Status == "Known" && first.HelperStoreInstructions == 6 && first.ScalarStoreInstructions == 6 && first.NextInstructionAddress == 0x180323743UL,
            "first explicit execution completes this local prefix");
        Need(b.Raw858 == 4UL && b.Raw860 == 5UL && b.Raw868 == 6UL && b.Untouched850 == 20UL && StillSeeded(bScalars),
            "a distinct subobject and its scalars remain unchanged");
        var repeated = ItemConstructor858Prefix.Apply(a, aScalars);
        Need(repeated.Status == "Known" && repeated.HelperCompleted && repeated.HelperStoreInstructions == 6 && repeated.ScalarStoreInstructions == 6 &&
            first.HelperStoreInstructions == 6 && first.ScalarStoreInstructions == 6, "repeat executes all fixed stores again without accumulating or suppressing the count");
        Need(ZeroHelper(a) && ScalarGolden(aScalars) && a.Untouched850 == 10UL && aScalars.Untouched888 == 11U && aScalars.ReplicaStack6B0 == 12U &&
            Object.ReferenceEquals(repeated.HelperReturnedSubobject, a), "repeat preserves aliases and neighboring values while keeping the same final bytes");
        var other = ItemConstructor858Prefix.Apply(b, bScalars);
        Need(other.Status == "Known" && ZeroHelper(b) && ScalarGolden(bScalars) && Object.ReferenceEquals(other.Subobject, b) && Object.ReferenceEquals(other.Scalars, bScalars),
            "second object executes independently using its own supplied state");
        Need(ZeroHelper(a) && ScalarGolden(aScalars) && b.Untouched850 == 20UL && bScalars.Untouched888 == 21U && bScalars.ReplicaStack6B0 == 22U &&
            a.Untouched850 == 10UL && aScalars.Untouched888 == 11U && aScalars.ReplicaStack6B0 == 12U,
            "separate objects retain distinct sentinels after both prefixes run");
        scenarios++;
    }

    static void StableScalarApiContrast()
    {
        // One new paired input; the historical scalar test suite is not invoked.
        var integratedState = Seeded(123U, 456U); var standaloneState = Seeded(123U, 456U);
        var prefix = ItemConstructor858Prefix.Apply(new ItemConstructor858Subobject(9UL, 8UL, 7UL), integratedState);
        var scalar = ItemScalarInitialization.Apply(standaloneState);
        Need(prefix.Status == "Known" && scalar.Status == "Known" && prefix.ScalarStoreInstructions == scalar.CompletedStores &&
            prefix.NextInstructionAddress == scalar.NextInstructionAddress && !prefix.IsCompleteConstructor && !scalar.IsCompleteConstructor,
            "new local integration agrees with the existing scalar segment's completion boundary");
        Need(ScalarGolden(integratedState) && ScalarGolden(standaloneState) && integratedState.Raw870 == standaloneState.Raw870 &&
            integratedState.Raw878 == standaloneState.Raw878 && integratedState.Raw880 == standaloneState.Raw880 && integratedState.Raw88C == standaloneState.Raw88C &&
            integratedState.Raw894 == standaloneState.Raw894 && integratedState.Raw89C == standaloneState.Raw89C,
            "paired inputs produce the same independently specified six scalar stores");
        Need(integratedState.Gender874 == standaloneState.Gender874 && integratedState.Stack88C == standaloneState.Stack88C &&
            integratedState.Word890 == standaloneState.Word890 && integratedState.Word898 == standaloneState.Word898 && integratedState.Stack88C == 1U && integratedState.Word890 == 0U,
            "overlapping views remain consistent with the reused scalar implementation");
        Need(integratedState.Untouched888 == 123U && integratedState.ReplicaStack6B0 == 456U &&
            standaloneState.Untouched888 == 123U && standaloneState.ReplicaStack6B0 == 456U,
            "integration adds no writes to the old scalar segment's excluded neighbors");
        scenarios++;
    }

    public static int Main()
    {
        try {
            UnknownStorageBecomesKnownPrefix(); NonzeroStorageAndExactAliases(); MissingScalarsKeepCompletedHelper();
            MissingSubobjectDoesNotTouchScalars(); RepetitionAndIndependentObjects(); StableScalarApiContrast();
            Console.WriteLine("PASS: " + assertions + " assertions, " + scenarios + " scenario groups"); return 0;
        } catch (Exception error) {
            Console.Error.WriteLine("FAIL after " + assertions + " assertions and " + scenarios + " completed groups: " + error); return 1;
        }
    }
}
