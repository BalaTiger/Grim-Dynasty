// Bounded normal-memory profile of the archived Engine 0x180278D20 branches.
// This is separate from Lz4Block's input-bounded format decoder.
using System;

namespace GrimDynasty.Resources
{
    public enum Lz4OutputSizeStatus
    {
        Success,
        NativeLengthRejected,
        Unsupported
    }

    public sealed class Lz4OutputSizeResult
    {
        public readonly Lz4OutputSizeStatus Status;
        public readonly string Reason;
        // Logical source cursor: bytes consumed, not bytes merely checked/read
        // by a wild copy. On rejection this is the cursor used for -p-1.
        public readonly int InputPosition;
        public readonly int? NativeReturn;
        private readonly byte[] output;

        // completedOutput is private newly allocated storage transferred by
        // DecodeChecked; no caller-owned or input array is retained here.
        internal Lz4OutputSizeResult(Lz4OutputSizeStatus status, string reason,
            int inputPosition, int? nativeReturn, byte[] completedOutput)
        {
            Status = status;
            Reason = reason;
            InputPosition = inputPosition;
            NativeReturn = nativeReturn;
            output = completedOutput;
        }

        public byte[] CopyOutput()
        {
            if (Status != Lz4OutputSizeStatus.Success)
                throw new InvalidOperationException("No successful output is available");
            return (byte[])output.Clone();
        }
    }

    public static class Lz4OutputSizeProfile
    {
        public const int MaxInputBytes = 2 * 1024 * 1024;
        public const int MaxOutputBytes = 1024 * 1024;

        // Own coverage conditions: stable source bytes, private nonaliasing
        // output, successful managed allocation and normal successful copying.
        // There is no native packed-end argument. Additional source bytes are
        // allowed, and a successful NativeReturn reports consumed input only.
        //
        // Archived 278D20 entry/body and copy tables close the normal copying
        // branches. The small-distance tables are inc32=[0,1,2,1,4,4,4,4] and
        // dec64=[0,0,0,-1,0,1,2,3]. For valid positive backward distances their
        // logical result is forward byte-wise repetition. We do not reproduce
        // speculative output writes, memory faults, native allocator/EH or ABI.
        //
        // A failed result deliberately hides all private output. Native code
        // may already have written output before rejecting; this is not rollback.
        public static Lz4OutputSizeResult DecodeChecked(byte[] source, int expected)
        {
            if (source == null) return Unsupported("NullSource", 0);
            if (source.Length > MaxInputBytes)
                return Unsupported("InputBytesOutsideCoveredDomain", 0);
            if (expected < 0 || expected > MaxOutputBytes)
                return Unsupported("OutputSizeOutsideCoveredDomain", 0);

            // 278D95..DA3: byte zero yields 1; another byte yields -1.
            // The failure path compares without advancing the native cursor.
            if (expected == 0)
            {
                if (source.Length == 0) return Unsupported("MissingZeroOutputToken", 0);
                if (source[0] != 0)
                    return Rejected("NonzeroTokenForZeroOutput", 0);
                return Success(new byte[0], 1);
            }

            byte[] output = new byte[expected];
            int p = 0, q = 0;
            while (true)
            {
                if (p == source.Length) return Unsupported("MissingToken", p);
                int token = source[p++];
                int literal;
                if (!ReadLength(source, ref p, token >> 4, out literal))
                    return Unsupported("TruncatedLiteralLength", p);
                int literalEnd = checked(q + literal);

                // 278DF0..DF7 / 278F59..F71: this end-8 branch either exactly
                // completes the output with memcpy, or returns the native error.
                // The length rejection occurs before any literal-source read.
                if (literalEnd > expected - 8)
                {
                    if (literalEnd != expected)
                        return Rejected("LiteralEndDoesNotEqualOutputEnd", p);
                    if (literal > source.Length - p)
                        return Unsupported("TruncatedFinalLiteral", p);
                    Buffer.BlockCopy(source, p, output, q, literal);
                    p += literal;
                    return Success(output, p);
                }

                // 278E10 is a do/while qword copy: even zero logical literals
                // require eight readable source bytes. Only this safety guard
                // depends on the supplied array length, not a native end check.
                int readable = Math.Max(8, checked((literal + 7) / 8 * 8));
                if (readable > source.Length - p)
                    return Unsupported("InsufficientLiteralWildCopySource", p);
                Buffer.BlockCopy(source, p, output, q, literal);
                p += literal;
                q = literalEnd;

                if (source.Length - p < 2) return Unsupported("TruncatedOffset", p);
                int distance = source[p] | (source[p + 1] << 8);
                p += 2;
                int match;
                // 278E41..E52 consumes the full extension before match reads
                // and the later length-rejection branch. Do not clamp early.
                if (!ReadLength(source, ref p, token & 15, out match))
                    return Unsupported("TruncatedMatchLength", p);
                match = checked(match + 4);
                if (distance == 0 || distance > q)
                    return Unsupported("InvalidMatchDistance", p);
                int matchEnd = checked(q + match);

                // 278EAC..EB7: end-5 is the actual length rejection. The
                // separate end-12 comparison selects a copying strategy only.
                // Native initial match writes can precede this error return.
                if (matchEnd > expected - 5)
                    return Rejected("MatchEndExceedsLastLiteralsBoundary", p);
                for (int i = 0; i < match; i++)
                {
                    output[q] = output[q - distance];
                    q++;
                }
            }
        }

        // With <=2MiB input, even a stream of 255 extension bytes sums to
        // less than 535 million. Length/end arithmetic therefore fits Int32.
        // Keep consuming until the first non-255 byte or the safety boundary;
        // comparing with expected here would change the observed error cursor.
        private static bool ReadLength(byte[] source, ref int p, int initial,
            out int length)
        {
            length = initial;
            if (initial != 15) return true;
            int extension;
            do
            {
                if (p == source.Length) return false;
                extension = source[p++];
                length = checked(length + extension);
            }
            while (extension == 255);
            return true;
        }

        private static Lz4OutputSizeResult Unsupported(string reason, int p)
        {
            return new Lz4OutputSizeResult(Lz4OutputSizeStatus.Unsupported,
                reason, p, null, null);
        }

        private static Lz4OutputSizeResult Rejected(string reason, int p)
        {
            return new Lz4OutputSizeResult(Lz4OutputSizeStatus.NativeLengthRejected,
                reason, p, checked(-p - 1), null);
        }

        private static Lz4OutputSizeResult Success(byte[] ownedOutput, int p)
        {
            return new Lz4OutputSizeResult(Lz4OutputSizeStatus.Success,
                null, p, p, ownedOutput);
        }
    }
}
