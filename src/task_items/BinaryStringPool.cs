using System;
using System.Collections.Generic;
using System.IO;
using GrimDynasty.Resources;

namespace GrimDynasty.TaskItems
{
    // Own byte-exact lookup and lazy storage, not a reconstruction of native
    // archive hash-table layout. IDs always refer to the original pool order.
    public sealed class BinaryStringPool
    {
        public const int MaximumExistingStrings = 4096;
        public const int MaximumExistingBytes = 2097152;
        public const int MaximumRawStrings = 131072;
        public const int MaximumRawWireBytes = 8388608;
        public const int MaximumRawPayloadBytes = 4194304;
        public const int MaximumMaterializedStrings = 1024;
        public const int MaximumMaterializedBytes = 262144;
        private readonly ArzRawRecord raw;
        private readonly int[] sortedIds;
        private readonly TableString[] memo;
        private readonly object sync = new object();
        private int materializedCount, materializedBytes;
        public readonly int EmptyId;
        public readonly bool IsUnique;
        public int Count { get { return memo.Length; } }
        public bool IsRawBacked { get { return raw != null; } }
        public int MaterializedCount { get { lock (sync) return materializedCount; } }
        // Sum of payload lengths per materialized ID. Supplied compact slots
        // may share an object; counting each slot preserves their existing budget.
        public int MaterializedBytes { get { lock (sync) return materializedBytes; } }
        // Only this pool's index and memo arrays; references estimated at x64 size.
        // Excludes TableString payloads, raw arrays, object headers and transient copies.
        public long RetainedArrayPayloadBytes { get { return 4L * sortedIds.Length + 8L * memo.Length; } }
        // The retained immutable raw record includes its selected body/name/class.
        public long RawRetainedArrayPayloadBytes { get { return raw == null ? 0 : raw.RetainedArrayPayloadBytes; } }
        public TableString this[int originalId] { get { return Get(originalId); } }

        private BinaryStringPool(ArzRawRecord rawRecord, TableString[] supplied, int count,
            int emptyId, int suppliedBytes)
        {
            raw = rawRecord;
            memo = supplied == null ? new TableString[count] : (TableString[])supplied.Clone();
            sortedIds = new int[count];
            for (int i = 0; i < count; i++) sortedIds[i] = i;
            Array.Sort(sortedIds, new IdComparer(this));
            bool unique = true;
            for (int i = 1; i < count; i++)
            {
                bool equal = raw == null ?
                    LoadTableSnapshot.Compare(memo[sortedIds[i - 1]].Bytes, memo[sortedIds[i]].Bytes) == 0 :
                    raw.CompareStringBytes((uint)sortedIds[i - 1], (uint)sortedIds[i]) == 0;
                if (equal)
                {
                    unique = false;
                    if (raw != null)
                        throw new InvalidDataException("BinaryStringPoolDuplicateRawString");
                }
            }
            IsUnique = unique;
            EmptyId = emptyId;
            // Compact input is already materialized; its old 4096/2MiB budget
            // remains separate from the new raw pool's lazy materialization budget.
            materializedCount = supplied == null ? 0 : count;
            materializedBytes = suppliedBytes;
        }

        public static BinaryStringPool FromExisting(TableString[] supplied)
        {
            if (supplied == null) throw new ArgumentNullException("supplied");
            if (supplied.Length > MaximumExistingStrings)
                throw new ArgumentOutOfRangeException("supplied", "BinaryStringPoolCompactCountOutsideCoveredDomain");
            int total = 0, empty = -1;
            for (int i = 0; i < supplied.Length; i++)
            {
                if (supplied[i] == null) throw new ArgumentException("Unknown shared string", "supplied");
                total += supplied[i].Length;
                if (total > MaximumExistingBytes)
                    throw new ArgumentOutOfRangeException("supplied", "BinaryStringPoolCompactBytesOutsideCoveredDomain");
                if (empty < 0 && supplied[i].Length == 0) empty = i;
            }
            // Existing compact snapshots may contain duplicates or no empty string.
            return new BinaryStringPool(null, supplied, supplied.Length, empty, total);
        }

        public static BinaryStringPool FromUniqueRaw(ArzRawRecord rawRecord)
        {
            if (rawRecord == null) throw new ArgumentNullException("rawRecord");
            if (rawRecord.StringCount > MaximumRawStrings)
                throw new InvalidDataException("BinaryStringPoolRawCountOutsideCoveredDomain");
            if (rawRecord.StringTableBytes > MaximumRawWireBytes)
                throw new InvalidDataException("BinaryStringPoolRawWireOutsideCoveredDomain");
            if (rawRecord.PoolPayloadBytes > MaximumRawPayloadBytes)
                throw new InvalidDataException("BinaryStringPoolRawPayloadOutsideCoveredDomain");
            int empty = -1;
            for (int i = 0; i < rawRecord.StringCount; i++)
            {
                int length = rawRecord.StringByteLength((uint)i);
                if (length > TableString.MaximumBytes)
                    throw new InvalidDataException("BinaryStringPoolStringOutsideCoveredDomain");
                if (length == 0) empty = i;
            }
            if (empty < 0) throw new InvalidDataException("BinaryStringPoolMissingRawEmptyString");
            // All size guards precede index/memo allocation. Sorting only changes
            // original-ID indices, never raw storage. Array.Sort's bounded stack
            // does not allocate per-string payloads.
            return new BinaryStringPool(rawRecord, null, rawRecord.StringCount, empty, 0);
        }

        public TableString Get(int originalId)
        {
            if (originalId < 0 || originalId >= memo.Length)
                throw new ArgumentOutOfRangeException("originalId");
            lock (sync)
            {
                if (memo[originalId] != null) return memo[originalId];
                int length = raw.StringByteLength((uint)originalId);
                if (materializedCount == MaximumMaterializedStrings)
                    throw new InvalidOperationException("BinaryStringPoolMaterializationCountOutsideCoveredDomain");
                if (length > MaximumMaterializedBytes - materializedBytes)
                    throw new InvalidOperationException("BinaryStringPoolMaterializationBytesOutsideCoveredDomain");
                // Normal managed allocation/copy success is a model precondition.
                // The temporary export is not retained. Failure publishes no memo
                // or counters; an already materialized ID always keeps its identity.
                TableString value = new TableString(raw.CopyStringBytes((uint)originalId));
                memo[originalId] = value;
                materializedCount++;
                materializedBytes += length;
                return value;
            }
        }

        // Full bytes (including interior NUL), not a C-string or culture comparison.
        // For old compact duplicate entries, return the first original ID.
        public int FindExact(byte[] fullStoredBytes)
        {
            if (fullStoredBytes == null) throw new ArgumentNullException("fullStoredBytes");
            if (fullStoredBytes.Length > TableString.MaximumBytes) return -1;
            int lo = 0, hi = sortedIds.Length;
            while (lo < hi)
            {
                int mid = lo + (hi - lo) / 2;
                if (CompareToBytes(sortedIds[mid], fullStoredBytes) < 0) lo = mid + 1;
                else hi = mid;
            }
            return lo < sortedIds.Length && CompareToBytes(sortedIds[lo], fullStoredBytes) == 0
                ? sortedIds[lo] : -1;
        }

        private int CompareToBytes(int id, byte[] bytes)
        {
            return raw == null ? LoadTableSnapshot.Compare(memo[id].Bytes, bytes) :
                raw.CompareStringToBytes((uint)id, bytes);
        }

        private sealed class IdComparer : IComparer<int>
        {
            private readonly BinaryStringPool owner;
            internal IdComparer(BinaryStringPool owner) { this.owner = owner; }
            public int Compare(int left, int right)
            {
                int order = owner.raw == null ?
                    LoadTableSnapshot.Compare(owner.memo[left].Bytes, owner.memo[right].Bytes) :
                    owner.raw.CompareStringBytes((uint)left, (uint)right);
                return order != 0 ? order : left.CompareTo(right);
            }
        }
    }
}
