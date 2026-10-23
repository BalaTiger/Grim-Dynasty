// Independent ARZ v3 raw-record access. No native fetch, name-ID remap or cache.
using System;
using System.IO;
using System.Text;

namespace GrimDynasty.Resources
{
    // Explicit contracts. The observed output-size profile is a safe covered
    // subset, not a binding to Engine code or a complete native archive loader.
    public enum ArzRawDecodeProfile { BoundedInput, ObservedEngineOutputSize }

    // Explicit model budgets, not native format rejection rules or process-memory limits.
    public sealed class ArzRawLimits
    {
        public const long MaximumWorkingBytes = 64L * 1024 * 1024;
        public readonly int MaxRecords, MaxStrings;
        public readonly int MaxStringTableBytes, MaxRecordTableBytes;
        public readonly int MaxStringBytes, MaxPoolBytes, MaxPackedBytes, MaxBodyBytes;
        public readonly long MaxWorkingBytes;

        public ArzRawLimits(int maxRecords, int maxStrings, int maxStringTableBytes,
            int maxRecordTableBytes, int maxStringBytes, int maxPoolBytes,
            int maxPackedBytes, int maxBodyBytes, long maxWorkingBytes)
        {
            if (maxRecords < 0 || maxRecords > 1000000)
                throw new ArgumentOutOfRangeException("maxRecords");
            if (maxStrings < 0 || maxStrings > 2000000)
                throw new ArgumentOutOfRangeException("maxStrings");
            CheckByteLimit(maxStringTableBytes, "maxStringTableBytes");
            CheckByteLimit(maxRecordTableBytes, "maxRecordTableBytes");
            CheckByteLimit(maxStringBytes, "maxStringBytes");
            CheckByteLimit(maxPoolBytes, "maxPoolBytes");
            CheckByteLimit(maxPackedBytes, "maxPackedBytes");
            CheckByteLimit(maxBodyBytes, "maxBodyBytes");
            if (maxWorkingBytes <= 0 || maxWorkingBytes > MaximumWorkingBytes)
                throw new ArgumentOutOfRangeException("maxWorkingBytes");
            MaxRecords = maxRecords;
            MaxStrings = maxStrings;
            MaxStringTableBytes = maxStringTableBytes;
            MaxRecordTableBytes = maxRecordTableBytes;
            MaxStringBytes = maxStringBytes;
            MaxPoolBytes = maxPoolBytes;
            MaxPackedBytes = maxPackedBytes;
            MaxBodyBytes = maxBodyBytes;
            MaxWorkingBytes = maxWorkingBytes;
        }

        private static void CheckByteLimit(int value, string parameter)
        {
            if (value < 0 || value > Lz4Block.MaxOutput)
                throw new ArgumentOutOfRangeException(parameter);
        }
    }

    // Detached immutable bytes. Duplicate/empty pool strings retain their original IDs.
    // Export methods allocate caller-owned copies; repeated exports are outside read budgets.
    public sealed class ArzRawRecord
    {
        private readonly byte[] recordName, recordClass, body, pool;
        private readonly int[] stringOffsets, stringLengths;
        public readonly uint RecordNameStringId;
        public readonly long DataOffset;
        public readonly uint PackedBytes, BodyBytes, TimestampLow, TimestampHigh;
        public readonly ArzRawDecodeProfile DecodeProfile;
        public readonly int PackedBytesConsumed;
        public int StringCount { get { return stringOffsets.Length; } }
        public int StringTableBytes { get { return pool.Length; } }
        public long PoolPayloadBytes
        {
            get
            {
                long total = 0;
                for (int i = 0; i < stringLengths.Length; i++) total += stringLengths[i];
                return total;
            }
        }
        // Array payload only: no object headers, CLR overhead or exported copies.
        public long RetainedArrayPayloadBytes
        {
            get
            {
                return pool.LongLength + 8L * stringOffsets.Length +
                    recordName.LongLength + recordClass.LongLength + body.LongLength;
            }
        }

        public int StringByteLength(uint originalId)
        {
            if (originalId >= (uint)stringLengths.Length)
                throw new ArgumentOutOfRangeException("originalId");
            return stringLengths[(int)originalId];
        }

        // Unsigned lexicographic byte comparison of complete stored payloads.
        // These read-only operations do not decode, copy, normalize or truncate at NUL.
        public int CompareStringBytes(uint leftId, uint rightId)
        {
            int leftLength = StringByteLength(leftId), rightLength = StringByteLength(rightId);
            int left = stringOffsets[(int)leftId], right = stringOffsets[(int)rightId];
            int end = Math.Min(leftLength, rightLength);
            for (int i = 0; i < end; i++)
                if (pool[left + i] != pool[right + i])
                    return pool[left + i] < pool[right + i] ? -1 : 1;
            return leftLength < rightLength ? -1 : (leftLength > rightLength ? 1 : 0);
        }

        public int CompareStringToBytes(uint originalId, byte[] fullStoredBytes)
        {
            if (fullStoredBytes == null) throw new ArgumentNullException("fullStoredBytes");
            int length = StringByteLength(originalId), start = stringOffsets[(int)originalId];
            int end = Math.Min(length, fullStoredBytes.Length);
            for (int i = 0; i < end; i++)
                if (pool[start + i] != fullStoredBytes[i])
                    return pool[start + i] < fullStoredBytes[i] ? -1 : 1;
            return length < fullStoredBytes.Length ? -1 : (length > fullStoredBytes.Length ? 1 : 0);
        }

        internal ArzRawRecord(byte[] ownedName, byte[] ownedClass, byte[] ownedBody,
            byte[] ownedPool, int[] ownedOffsets, int[] ownedLengths, uint nameId,
            long dataOffset, uint packedBytes, uint bodyBytes, uint timestampLow,
            uint timestampHigh, ArzRawDecodeProfile decodeProfile, int packedBytesConsumed)
        {
            recordName = ownedName;
            recordClass = ownedClass;
            body = ownedBody;
            pool = ownedPool;
            stringOffsets = ownedOffsets;
            stringLengths = ownedLengths;
            RecordNameStringId = nameId;
            DataOffset = dataOffset;
            PackedBytes = packedBytes;
            BodyBytes = bodyBytes;
            TimestampLow = timestampLow;
            TimestampHigh = timestampHigh;
            DecodeProfile = decodeProfile;
            PackedBytesConsumed = packedBytesConsumed;
        }

        public byte[] CopyRecordNameBytes() { return (byte[])recordName.Clone(); }
        public byte[] CopyClassBytes() { return (byte[])recordClass.Clone(); }
        public byte[] CopyBodyBytes() { return (byte[])body.Clone(); }
        // Original count/length prefixes and bytes, with no reencoding or ID
        // changes. Callers check their own import budget before taking a copy.
        public byte[] CopyStringTableBytes() { return (byte[])pool.Clone(); }
        public byte[] CopyStringBytes(uint originalId)
        {
            if (originalId >= (uint)stringOffsets.Length)
                throw new ArgumentOutOfRangeException("originalId");
            int id = (int)originalId;
            byte[] result = new byte[stringLengths[id]];
            Buffer.BlockCopy(pool, stringOffsets[id], result, 0, result.Length);
            return result;
        }
    }

    public static class ArzRawRecordReader
    {
        private static readonly Encoding StrictUtf8 = new UTF8Encoding(false, true);

        // Counts every array payload allocated during this read, including temporary arrays.
        // Deliberately does not subtract dead temporaries or claim to bound CLR/GC overhead.
        private sealed class Budget
        {
            private long remaining;
            internal Budget(long maximum) { remaining = maximum; }
            internal void Reserve(long bytes)
            {
                if (bytes < 0 || bytes > remaining)
                    throw Invalid("ArzRawWorkingBudget");
                remaining -= bytes;
            }
        }

        // Borrowed input must remain stable and exclusively accessed for this call.
        // This method changes its stream position, never disposes it and retains no handle.
        // Complete metadata is validated, but only the selected payload is read/decompressed.
        // This is byte-exact selection, not native normalized-name lookup or archive merging.
        public static ArzRawRecord ReadExact(ResourceInput borrowedInput,
            byte[] exactStoredRecordName, ArzRawLimits limits)
        {
            return ReadExact(borrowedInput, exactStoredRecordName, limits,
                ArzRawDecodeProfile.BoundedInput);
        }

        public static ArzRawRecord ReadExact(ResourceInput borrowedInput,
            byte[] exactStoredRecordName, ArzRawLimits limits, ArzRawDecodeProfile profile)
        {
            if (profile != ArzRawDecodeProfile.BoundedInput &&
                profile != ArzRawDecodeProfile.ObservedEngineOutputSize)
                throw new ArgumentOutOfRangeException("profile");
            if (borrowedInput == null) throw new ArgumentNullException("borrowedInput");
            if (exactStoredRecordName == null)
                throw new ArgumentNullException("exactStoredRecordName");
            if (limits == null) throw new ArgumentNullException("limits");
            if (exactStoredRecordName.Length > limits.MaxStringBytes)
                throw Invalid("ArzRawStringBytesBudget");
            Budget budget = new Budget(limits.MaxWorkingBytes);
            budget.Reserve(exactStoredRecordName.Length);
            byte[] key = (byte[])exactStoredRecordName.Clone();
            long fileLength = borrowedInput.Length;
            byte[] header = Read(borrowedInput, fileLength, 0, 24, budget);
            int at = 0;
            uint signature = U32(header, ref at);
            if (signature != 0x00030002U)
                throw Invalid("ArzRawUnsupportedMarkerVersion");
            uint recordOffset = U32(header, ref at);
            uint recordSize = U32(header, ref at);
            uint recordCount = U32(header, ref at);
            uint stringOffset = U32(header, ref at);
            uint stringSize = U32(header, ref at);
            if (recordCount > (uint)limits.MaxRecords)
                throw Invalid("ArzRawRecordCountBudget");
            if (recordSize > (uint)limits.MaxRecordTableBytes)
                throw Invalid("ArzRawRecordTableBytesBudget");
            if (stringSize > (uint)limits.MaxStringTableBytes)
                throw Invalid("ArzRawStringTableBytesBudget");
            if (recordOffset < 24 || stringOffset < 24 || stringSize < 4)
                throw Invalid("ArzRawTableExtent");
            Extent(fileLength, recordOffset, recordSize);
            Extent(fileLength, stringOffset, stringSize);
            if (Overlaps(recordOffset, recordSize, stringOffset, stringSize))
                throw Invalid("ArzRawTableOverlap");
            if ((long)recordCount * 28 > recordSize)
                throw Invalid("ArzRawRecordTableLength");

            // Count and aggregate payload bounds precede the full table allocation/read.
            byte[] countBytes = Read(borrowedInput, fileLength, stringOffset, 4, budget);
            at = 0;
            uint stringCount = U32(countBytes, ref at);
            if (stringCount > (uint)limits.MaxStrings)
                throw Invalid("ArzRawStringCountBudget");
            long poolBytes = (long)stringSize - 4 - (long)stringCount * 4;
            if (poolBytes < 0) throw Invalid("ArzRawStringTableLength");
            if (poolBytes > limits.MaxPoolBytes)
                throw Invalid("ArzRawPoolBytesBudget");
            // Aggregate and per-array charges are checked before any of these allocations.
            budget.Reserve((long)stringCount * 8);
            byte[] pool = Read(borrowedInput, fileLength, stringOffset, stringSize, budget);
            int[] offsets = new int[(int)stringCount];
            int[] lengths = new int[(int)stringCount];
            at = 0;
            if (U32(pool, ref at) != stringCount)
                throw Invalid("ArzRawInputChanged");
            long observedPoolBytes = 0;
            for (int i = 0; i < offsets.Length; i++)
            {
                uint size = U32(pool, ref at);
                if (size > (uint)limits.MaxStringBytes)
                    throw Invalid("ArzRawStringBytesBudget");
                Need(pool, at, size);
                offsets[i] = at;
                lengths[i] = (int)size;
                // Validation only: no decoding/reencoding, trimming or deduplication.
                try { StrictUtf8.GetCharCount(pool, at, (int)size); }
                catch (DecoderFallbackException error)
                {
                    throw new InvalidDataException("ArzRawInvalidUtf8", error);
                }
                at += (int)size;
                observedPoolBytes += size;
            }
            if (at != pool.Length || observedPoolBytes != poolBytes)
                throw Invalid("ArzRawStringTableLength");

            byte[] records = Read(borrowedInput, fileLength, recordOffset, recordSize, budget);
            at = 0;
            bool found = false;
            uint selectedId = 0, selectedPacked = 0, selectedBody = 0;
            uint selectedLow = 0, selectedHigh = 0;
            int selectedClassOffset = 0, selectedClassLength = 0;
            long selectedOffset = 0;
            for (uint i = 0; i < recordCount; i++)
            {
                uint nameId = U32(records, ref at);
                if (nameId >= stringCount) throw Invalid("ArzRawStringIndex");
                uint classLength = U32(records, ref at);
                if (classLength > (uint)limits.MaxStringBytes)
                    throw Invalid("ArzRawStringBytesBudget");
                Need(records, at, classLength);
                int classOffset = at;
                for (int j = 0; j < (int)classLength; j++)
                    if (records[at + j] > 127) throw Invalid("ArzRawNonAsciiClass");
                at += (int)classLength;
                // The file stores offsets relative to the end of its 24-byte header.
                long dataOffset = (long)U32(records, ref at) + 24;
                uint packed = U32(records, ref at);
                uint unpacked = U32(records, ref at);
                uint low = U32(records, ref at), high = U32(records, ref at);
                if (unpacked > Lz4Block.MaxOutput) throw Invalid("ArzRawFormatBodyLimit");
                Extent(fileLength, dataOffset, packed);
                if (dataOffset > recordOffset || packed > (long)recordOffset - dataOffset ||
                    Overlaps(dataOffset, packed, stringOffset, stringSize))
                    throw Invalid("ArzRawPayloadTableOverlap");
                int id = (int)nameId;
                if (!Equal(key, pool, offsets[id], lengths[id])) continue;
                if (found) throw Invalid("ArzRawDuplicateExactRecordName");
                found = true;
                selectedId = nameId;
                selectedOffset = dataOffset;
                selectedPacked = packed;
                selectedBody = unpacked;
                selectedLow = low;
                selectedHigh = high;
                selectedClassOffset = classOffset;
                selectedClassLength = (int)classLength;
            }
            if (at != records.Length) throw Invalid("ArzRawRecordTableLength");
            if (!found) throw Invalid("ArzRawRecordNotFound");
            if (selectedPacked > (uint)limits.MaxPackedBytes)
                throw Invalid("ArzRawPackedBytesBudget");
            if (selectedBody > (uint)limits.MaxBodyBytes)
                throw Invalid("ArzRawBodyBytesBudget");
            bool outputProfile = profile == ArzRawDecodeProfile.ObservedEngineOutputSize;
            if (outputProfile && (selectedPacked > Lz4OutputSizeProfile.MaxInputBytes ||
                selectedBody > Lz4OutputSizeProfile.MaxOutputBytes))
                throw Invalid("ArzRawOutputProfileBudget");
            // Reserve output and class before reading any selected compressed bytes.
            // Output-profile result owns one output array; CopyOutput makes the
            // detached record's second copy. Both count against this read budget.
            budget.Reserve((long)selectedBody * (outputProfile ? 2 : 1) + selectedClassLength);
            byte[] packedBody = Read(borrowedInput, fileLength,
                selectedOffset, selectedPacked, budget);
            byte[] body;
            int consumed = packedBody.Length;
            try
            {
                if (outputProfile)
                {
                    Lz4OutputSizeResult decoded = Lz4OutputSizeProfile.DecodeChecked(packedBody, (int)selectedBody);
                    if (decoded.Status == Lz4OutputSizeStatus.Unsupported)
                        throw Invalid("ArzRawOutputProfileUnsupported:" + decoded.Reason);
                    if (decoded.Status == Lz4OutputSizeStatus.NativeLengthRejected)
                        throw Invalid("ArzRawOutputProfileLengthRejected:" + decoded.NativeReturn.Value);
                    body = decoded.CopyOutput();
                    consumed = decoded.NativeReturn.Value;
                }
                else body = Lz4Block.Decode(packedBody, (int)selectedBody);
            }
            catch (OverflowException error)
            {
                throw new InvalidDataException("ArzRawInvalidLz4", error);
            }
            byte[] recordClass = new byte[selectedClassLength];
            Buffer.BlockCopy(records, selectedClassOffset, recordClass, 0, recordClass.Length);
            return new ArzRawRecord(key, recordClass, body, pool, offsets, lengths,
                selectedId, selectedOffset, selectedPacked, selectedBody, selectedLow, selectedHigh,
                profile, consumed);
        }

        private static InvalidDataException Invalid(string reason)
        {
            return new InvalidDataException(reason);
        }

        private static void Extent(long length, long offset, long count)
        {
            if (offset < 0 || count < 0 || offset > length || count > length - offset)
                throw Invalid("ArzRawExtentOutsideFile");
        }

        private static bool Overlaps(long first, long firstSize, long second, long secondSize)
        {
            return firstSize != 0 && secondSize != 0 &&
                first < second + secondSize && second < first + firstSize;
        }

        private static byte[] Read(ResourceInput input, long length, long offset,
            long count, Budget budget)
        {
            Extent(length, offset, count);
            budget.Reserve(count);
            try { return input.Read(offset, count); }
            catch (EndOfStreamException error)
            {
                throw new InvalidDataException("ArzRawTruncatedInput", error);
            }
        }

        private static void Need(byte[] bytes, int position, uint count)
        {
            if (position < 0 || position > bytes.Length || count > (long)bytes.Length - position)
                throw Invalid("ArzRawTruncatedMetadata");
        }

        private static uint U32(byte[] bytes, ref int position)
        {
            Need(bytes, position, 4);
            uint value = (uint)bytes[position] | ((uint)bytes[position + 1] << 8) |
                ((uint)bytes[position + 2] << 16) | ((uint)bytes[position + 3] << 24);
            position += 4;
            return value;
        }

        private static bool Equal(byte[] key, byte[] pool, int offset, int length)
        {
            if (key.Length != length) return false;
            for (int i = 0; i < length; i++)
                if (key[i] != pool[offset + i]) return false;
            return true;
        }
    }
}
