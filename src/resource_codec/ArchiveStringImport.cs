// Bounded logical projection of archived 283A80 / 2805B0 / 280490 / 280AB0.
using System;
using System.Collections.Generic;

namespace GrimDynasty.Resources
{
    public enum ArchiveStringImportStatus { Completed, Unsupported }

    public sealed class ArchiveStringImportResult
    {
        public readonly ArchiveStringImportStatus Status;
        public readonly string Reason;
        public readonly bool Append;
        public readonly uint BaseId;
        // -1 before a count prefix was read; otherwise its signed int32 value.
        public readonly int OriginalCount;
        public readonly int Cursor, EntriesProcessed, LengthFallbacks;
        public readonly bool SourceIdsPreserved, StateChanged;
        internal readonly ArchiveStringImportState Owner;
        private readonly List<uint> sourceIds;

        internal ArchiveStringImportResult(ArchiveStringImportState owner,
            string reason, bool append, uint baseId, int originalCount,
            int cursor, int entries, int fallbacks, bool idsPreserved,
            bool changed, List<uint> ownedSourceIds)
        {
            Owner = owner;
            Status = reason == null ? ArchiveStringImportStatus.Completed :
                ArchiveStringImportStatus.Unsupported;
            Reason = reason;
            Append = append;
            BaseId = baseId;
            OriginalCount = originalCount;
            Cursor = cursor;
            EntriesProcessed = entries;
            LengthFallbacks = fallbacks;
            SourceIdsPreserved = reason == null && idsPreserved;
            StateChanged = changed;
            // These IDs name immutable pool entries, which are never replaced.
            // The private list is transferred here and is not modified again.
            sourceIds = ownedSourceIds;
        }

        public int SourceStringCount { get { return sourceIds.Count; } }

        public byte[] CopySourceString(int index)
        {
            if (index < 0 || index >= sourceIds.Count)
                throw new ArgumentOutOfRangeException("index");
            return Owner.CopyTargetString(sourceIds[index]);
        }
    }

    public sealed class ArchiveStringImportState
    {
        public const int MaximumWireBytes = 4 * 1024 * 1024;
        public const int MaximumStringBytes = 65536;
        public const int MaximumTargetStrings = 4096;
        public const int MaximumPoolBytes = 2 * 1024 * 1024;
        public const int MaximumMappingEntries = 65536;
        public const int MaximumSelectors = 65536;
        public const int MaximumSourceEntries = 65536;

        private static readonly byte[] Empty = new byte[0];
        private readonly List<byte[]> targetStrings = new List<byte[]>();
        private readonly Dictionary<byte[], uint> lookup =
            new Dictionary<byte[], uint>(new ByteComparer());
        private readonly List<uint> selectors = new List<uint>();
        private uint[] mapping = new uint[0];
        private bool[] mappingKnown = new bool[0];
        private int mappingCount, poolBytes;
        private bool initialCompleted, requiresResolution;

        public ArchiveStringImportState() { }

        public int TargetStringCount { get { return targetStrings.Count; } }
        public int MappingCount { get { return mappingCount; } }
        public bool RequiresResolution { get { return requiresResolution; } }

        // Exports allocate independent snapshots. Repeated exports and callers
        // retaining historical handles are not a whole-process memory budget.
        public byte[][] CopyTargetStrings()
        {
            byte[][] copy = new byte[targetStrings.Count][];
            for (int i = 0; i < copy.Length; i++)
                copy[i] = (byte[])targetStrings[i].Clone();
            return copy;
        }

        public uint[] CopySelectors() { return selectors.ToArray(); }

        public bool TryGetMapping(int index, out uint value)
        {
            value = 0;
            if (index < 0 || index >= mappingCount || !mappingKnown[index])
                return false;
            value = mapping[index];
            return true;
        }

        internal byte[] CopyTargetString(uint id)
        {
            return (byte[])targetStrings[checked((int)id)].Clone();
        }

        // Historical completed handles retain their archive's base. Later
        // appends extend the complete map without changing prior target IDs.
        // Initial imports deliberately have no map, even after later appends.
        // This is a choice of this fresh self-owned logical state: 283A80 not
        // writing a map does not prove an arbitrary native archive has no map.
        // Full archive construction and native selector selection stay external.
        public RecordStringRemapContext CreateRemapContext(ArchiveStringImportResult result)
        {
            if (result == null || !Object.ReferenceEquals(result.Owner, this) ||
                result.Status != ArchiveStringImportStatus.Completed ||
                requiresResolution || !initialCompleted)
                throw new InvalidOperationException("Import is not complete for this state");
            for (int i = 0; i < mappingCount; i++)
                if (!mappingKnown[i])
                    throw new InvalidOperationException("Import mapping contains an unknown cell");
            if (!result.Append)
            {
                if (!result.SourceIdsPreserved)
                    throw new InvalidOperationException("Initial import did not preserve source IDs");
                return new RecordStringRemapContext(false, null, 0, 0);
            }
            return new RecordStringRemapContext(true, mapping, mappingCount, result.BaseId);
        }

        // borrowedWire is stable and exclusively readable for this call. Only
        // retained pool bytes are copied; no source array or native pointer is
        // retained. Full byte equality includes interior NUL and high bytes.
        //
        // Normal allocation/copy/release and valid, exclusive containers are
        // explicit contracts. This projects logical strings/IDs, not native
        // list nodes, string capacities, pointer caches, allocator hooks or EH.
        // In particular, Completed does not simulate 284080/284160 or claim a
        // native initialization/return ABI. Clear is available only implicitly
        // through this object's fresh construction, never on arbitrary state.
        //
        // Any own coverage failure is sticky. An exception (including own OOM)
        // propagates with RequiresResolution still true; it is not rewritten
        // as a native error or treated as rollback. StateChanged reports only
        // logical pool/map/selector effects, not this conservative safety flag.
        public ArchiveStringImportResult ImportChecked(byte[] borrowedWire, bool append)
        {
            int cursor = 0, originalCount = -1, entries = 0, fallbacks = 0;
            uint baseId = append ? (uint)mappingCount : 0U;
            bool changed = false, idsPreserved = true;
            List<uint> sourceIds = new List<uint>();
            if (requiresResolution)
                return Result("ArchiveStringImportRequiresResolution", append, baseId,
                    originalCount, cursor, entries, fallbacks, false, false, sourceIds);

            // Set before any further allocation or mutation so an unexpected
            // own exception cannot leave a partially published state reusable.
            requiresResolution = true;
            string failure = null;
            if (append && !initialCompleted) failure = "ArchiveStringInitialImportRequired";
            else if (!append && initialCompleted) failure = "ArchiveStringInitialImportAlreadyCompleted";
            else if (borrowedWire == null) failure = "NullArchiveStringWire";
            else if (borrowedWire.Length > MaximumWireBytes)
                failure = "ArchiveStringWireOutsideCoveredDomain";
            else if (!Readable(borrowedWire, cursor, 4))
                failure = "ArchiveStringCountPrefixUnavailable";
            if (failure != null)
                return Result(failure, append, baseId, originalCount, cursor,
                    entries, fallbacks, false, changed, sourceIds);

            originalCount = unchecked((int)ReadU32(borrowedWire, cursor));
            cursor += 4;
            if (originalCount < 0)
                return Result("ArchiveStringCountOutsideCoveredDomain", append, baseId,
                    originalCount, cursor, entries, fallbacks, false, changed, sourceIds);
            // Initial imports do not allocate mapping cells, so they need
            // their own source-ID metadata bound even when all names repeat.
            // Appends are already bounded by the cumulative mapping limit.
            if (!append && originalCount > MaximumSourceEntries)
                return Result("ArchiveStringSourceEntriesOutsideCoveredDomain", append, baseId,
                    originalCount, cursor, entries, fallbacks, false, changed, sourceIds);

            if (append)
            {
                if (originalCount > MaximumMappingEntries - mappingCount)
                    return Result("ArchiveStringMappingEntriesOutsideCoveredDomain", append,
                        baseId, originalCount, cursor, entries, fallbacks, false, changed, sourceIds);
                // Zero-count appends still consume a selector. Bound these as
                // well, so repeated empty archives cannot grow metadata forever.
                if (selectors.Count >= MaximumSelectors)
                    return Result("ArchiveStringSelectorsOutsideCoveredDomain", append,
                        baseId, originalCount, cursor, entries, fallbacks, false, changed, sourceIds);
                int newCount = checked(mappingCount + originalCount);
                uint[] nextMapping = new uint[newCount];
                bool[] nextKnown = new bool[newCount];
                Array.Copy(mapping, nextMapping, mappingCount);
                Array.Copy(mappingKnown, nextKnown, mappingCount);
                // 280490 publishes storage, appends the old count as selector,
                // then increments count. New cells remain explicitly unknown.
                mapping = nextMapping;
                mappingKnown = nextKnown;
                selectors.Add(baseId);
                mappingCount = newCount;
                changed = true;
            }

            // One temporary string per import, reused across all entries.
            byte[] temporary = Empty;
            while (entries < originalCount)
            {
                if (!Readable(borrowedWire, cursor, 4))
                    return Result("ArchiveStringLengthPrefixUnavailable", append, baseId,
                        originalCount, cursor, entries, fallbacks, false, changed, sourceIds);
                uint length = ReadU32(borrowedWire, cursor);
                cursor += 4;
                // Model the native uint end calculation, but reject wrap as an
                // own coverage boundary rather than permitting an unsafe read.
                ulong end = (ulong)(uint)cursor + length;
                if (length <= 0x100000U && end > UInt32.MaxValue)
                    return Result("ArchiveStringCursorArithmeticOutsideCoveredDomain", append,
                        baseId, originalCount, cursor, entries, fallbacks, false, changed, sourceIds);
                if (length <= 0x100000U && end <= (ulong)borrowedWire.Length)
                {
                    if (length > MaximumStringBytes)
                        return Result("ArchiveStringBytesOutsideCoveredDomain", append, baseId,
                            originalCount, cursor, entries, fallbacks, false, changed, sourceIds);
                    temporary = length == 0 ? Empty : new byte[checked((int)length)];
                    if (length != 0)
                        Buffer.BlockCopy(borrowedWire, cursor, temporary, 0, (int)length);
                    cursor += (int)length;
                }
                else
                {
                    // The native caller retains its old temporary and does
                    // not advance past this invalid payload length.
                    fallbacks++;
                }

                uint id;
                failure = GetOrAdd(temporary, ref changed, out id);
                if (failure != null)
                    return Result(failure, append, baseId, originalCount, cursor,
                        entries, fallbacks, false, changed, sourceIds);
                temporary = targetStrings[(int)id];
                if (append)
                {
                    int cell = checked((int)baseId + entries);
                    mapping[cell] = id;
                    mappingKnown[cell] = true;
                }
                if (id != (uint)entries) idsPreserved = false;
                sourceIds.Add(id);
                entries++;
            }

            // 280AB0's normal fresh-cache projection ensures a full-length
            // empty string exists without adding any selector or mapping cell.
            uint emptyId;
            failure = GetOrAdd(Empty, ref changed, out emptyId);
            if (failure != null)
                return Result(failure, append, baseId, originalCount, cursor,
                    entries, fallbacks, false, changed, sourceIds);
            if (!append) initialCompleted = true;
            ArchiveStringImportResult completed = Result(null, append, baseId,
                originalCount, cursor, entries, fallbacks, idsPreserved, changed, sourceIds);
            requiresResolution = false;
            return completed;
        }

        private string GetOrAdd(byte[] ownedBytes, ref bool changed, out uint id)
        {
            if (lookup.TryGetValue(ownedBytes, out id)) return null;
            if (targetStrings.Count >= MaximumTargetStrings)
                return "ArchiveStringTargetCountOutsideCoveredDomain";
            if (ownedBytes.Length > MaximumPoolBytes - poolBytes)
                return "ArchiveStringPoolBytesOutsideCoveredDomain";
            id = (uint)targetStrings.Count;
            targetStrings.Add(ownedBytes);
            poolBytes += ownedBytes.Length;
            changed = true;
            lookup.Add(ownedBytes, id);
            return null;
        }

        private ArchiveStringImportResult Result(string reason, bool append, uint baseId,
            int originalCount, int cursor, int entries, int fallbacks,
            bool idsPreserved, bool changed, List<uint> sourceIds)
        {
            return new ArchiveStringImportResult(this, reason, append, baseId, originalCount,
                cursor, entries, fallbacks, idsPreserved, changed, sourceIds);
        }

        private static bool Readable(byte[] bytes, int cursor, int count)
        {
            return cursor >= 0 && cursor <= bytes.Length && count <= bytes.Length - cursor;
        }

        private static uint ReadU32(byte[] bytes, int cursor)
        {
            return (uint)bytes[cursor] | ((uint)bytes[cursor + 1] << 8) |
                ((uint)bytes[cursor + 2] << 16) | ((uint)bytes[cursor + 3] << 24);
        }

        // Own dictionary acceleration; equality always compares the full bytes.
        // This is not a reconstruction of native buckets or collision order.
        private sealed class ByteComparer : IEqualityComparer<byte[]>
        {
            public bool Equals(byte[] left, byte[] right)
            {
                if (Object.ReferenceEquals(left, right)) return true;
                if (left == null || right == null || left.Length != right.Length) return false;
                for (int i = 0; i < left.Length; i++)
                    if (left[i] != right[i]) return false;
                return true;
            }

            public int GetHashCode(byte[] bytes)
            {
                ulong hash = 14695981039346656037UL;
                unchecked
                {
                    for (int i = 0; i < bytes.Length; i++)
                        hash = (hash ^ bytes[i]) * 1099511628211UL;
                    return (int)(hash ^ (hash >> 32));
                }
            }
        }
    }
}
