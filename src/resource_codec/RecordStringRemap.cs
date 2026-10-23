// Independent bounded normal-memory traversal of archived Engine 0x180280870.
using System;

namespace GrimDynasty.Resources
{
    public enum RecordStringRemapStatus
    {
        Completed,
        Unsupported
    }

    public sealed class RecordStringRemapContext
    {
        public const int MaximumMappingEntries = 65536;
        public readonly bool TablePresent;
        public readonly int SignedMappingCount;
        public readonly uint BaseId;
        private readonly uint[] mapping;

        // Presence and signed count are separate observed metadata. Null or
        // shorter supplied storage is tolerated until a lookup actually needs
        // a cell. A negative count is retained, not normalized to an array size.
        public RecordStringRemapContext(bool tablePresent, uint[] mapping,
            int signedMappingCount, uint baseId)
        {
            if (mapping != null && mapping.Length > MaximumMappingEntries)
                throw new ArgumentOutOfRangeException("mapping");
            TablePresent = tablePresent;
            SignedMappingCount = signedMappingCount;
            BaseId = baseId;
            this.mapping = mapping == null ? null : (uint[])mapping.Clone();
        }

        internal bool TryLookup(int index, out uint value)
        {
            value = 0;
            if (mapping == null || index < 0 || index >= mapping.Length) return false;
            value = mapping[index];
            return true;
        }
    }

    public sealed class RecordStringRemapResult
    {
        public readonly RecordStringRemapStatus Status;
        public readonly string Reason;
        // Native RAX-relative traversal position, including successful scalar
        // skips beyond the supplied array. Failed reads do not advance it;
        // failed mapping lookups occur after the preceding read advanced it.
        public readonly int Cursor;
        // Full field iterations only. WordsWritten counts all completed uint
        // stores, including keys and stores whose bits equal the previous bits.
        public readonly int FieldsProcessed, WordsWritten;

        internal RecordStringRemapResult(string reason, int cursor, int fields, int words)
        {
            Status = reason == null ? RecordStringRemapStatus.Completed :
                RecordStringRemapStatus.Unsupported;
            Reason = reason;
            Cursor = cursor;
            FieldsProcessed = fields;
            WordsWritten = words;
        }
    }

    public static class RecordStringRemap
    {
        public const int MaximumBodyBytes = 1024 * 1024;

        // Own stable, exclusive, writable scratch storage; modified in place.
        // No body snapshot, rollback, native bool return or error ABI is implied.
        // Completed only means this checked traversal reached its loop exit.
        //
        // The declared length controls the native loop-tail comparison, not
        // each field's extent. Header/value readability is checked against the
        // entire supplied body. Scalar skips do not dereference their payload
        // and may pass declaredLength or even body.Length without a new guard.
        public static RecordStringRemapResult ApplyChecked(byte[] borrowedBody,
            int declaredLength, RecordStringRemapContext context)
        {
            if (context == null) return Result("UnknownRemapContext", 0, 0, 0);
            if (borrowedBody == null) return Result("NullRemapBody", 0, 0, 0);
            if (borrowedBody.Length > MaximumBodyBytes)
                return Result("RemapBodyOutsideCoveredDomain", 0, 0, 0);
            if (declaredLength < 0 || declaredLength > borrowedBody.Length)
                return Result("RemapDeclaredLengthOutsideCoveredDomain", 0, 0, 0);
            if (!context.TablePresent || declaredLength == 0) return Result(null, 0, 0, 0);

            int cursor = 0, fields = 0, words = 0;
            do
            {
                if (!Readable(borrowedBody, cursor, 8))
                    return Result("RemapHeaderOutsideSuppliedBody", cursor, fields, words);
                int keyPosition = cursor + 4;
                uint key = ReadU32(borrowedBody, keyPosition);
                uint header = ReadU32(borrowedBody, cursor);
                cursor += 8;
                uint mapped;
                if (!TryMap(key, context, out mapped))
                    return Result("RemapMappingLookupOutsideSuppliedStorage", cursor, fields, words);
                WriteU32(borrowedBody, keyPosition, mapped);
                words++;

                uint tag = header & 65535U, count = header >> 16;
                if (tag == 2)
                {
                    for (uint i = 0; i < count; i++)
                    {
                        if (!Readable(borrowedBody, cursor, 4))
                            return Result("RemapValueOutsideSuppliedBody", cursor, fields, words);
                        int valuePosition = cursor;
                        uint value = ReadU32(borrowedBody, cursor);
                        cursor += 4;
                        if (!TryMap(value, context, out mapped))
                            return Result("RemapMappingLookupOutsideSuppliedStorage", cursor, fields, words);
                        WriteU32(borrowedBody, valuePosition, mapped);
                        words++;
                    }
                }
                else if (tag == 0 || tag == 1 || tag == 3)
                {
                    cursor += checked((int)count * 4);
                }
                // Other tags consume only their eight-byte header. Their
                // nominal payload can therefore become the next field header.
                fields++;
            }
            while ((uint)cursor < (uint)declaredLength);
            return Result(null, cursor, fields, words);
        }

        private static bool TryMap(uint original, RecordStringRemapContext context,
            out uint mapped)
        {
            mapped = unchecked(original + context.BaseId);
            int signed = unchecked((int)mapped);
            if (signed < 0 || signed >= context.SignedMappingCount) return true;
            // The mapped value is stored exactly once; it is neither offset
            // again nor recursively interpreted as a second mapping index.
            return context.TryLookup(signed, out mapped);
        }

        private static bool Readable(byte[] body, int position, int bytes)
        {
            return position >= 0 && position <= body.Length && bytes <= body.Length - position;
        }

        private static uint ReadU32(byte[] body, int position)
        {
            return (uint)body[position] | ((uint)body[position + 1] << 8) |
                ((uint)body[position + 2] << 16) | ((uint)body[position + 3] << 24);
        }

        private static void WriteU32(byte[] body, int position, uint value)
        {
            body[position] = unchecked((byte)value);
            body[position + 1] = unchecked((byte)(value >> 8));
            body[position + 2] = unchecked((byte)(value >> 16));
            body[position + 3] = unchecked((byte)(value >> 24));
        }

        private static RecordStringRemapResult Result(string reason, int cursor,
            int fields, int words)
        {
            return new RecordStringRemapResult(reason, cursor, fields, words);
        }
    }
}
