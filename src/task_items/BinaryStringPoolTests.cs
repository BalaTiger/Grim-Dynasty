using System;
using System.IO;
using System.Text;
using GrimDynasty.Resources;
using GrimDynasty.TaskItems;

// Only synthetic ARZv3 MemoryStream fixtures. No original resources or files.
class BinaryStringPoolTests
{
    static int assertions, scenarios;
    static void Need(bool value, string why)
    {
        assertions++;
        if (!value) throw new Exception(why);
    }
    static byte[] B(string value) { return Encoding.UTF8.GetBytes(value); }
    static bool Same(byte[] left, byte[] right)
    {
        if (left.Length != right.Length) return false;
        for (int i = 0; i < left.Length; i++) if (left[i] != right[i]) return false;
        return true;
    }
    static bool Throws<T>(Action action, string reason) where T : Exception
    {
        try { action(); }
        catch (T ex) { return reason == null || ex.Message.IndexOf(reason, StringComparison.Ordinal) >= 0; }
        return false;
    }
    static ArzRawLimits Limits()
    {
        return new ArzRawLimits(1, 131073, 8388608, 256, 65537, 4194305,
            32, 16, ArzRawLimits.MaximumWorkingBytes);
    }
    static ArzRawRecord Read(byte[][] strings)
    {
        var fixture = ArzRawFixture.Build(strings, new ArzRawFixtureRecord(0, new byte[0]));
        using (var input = new ResourceInput(new MemoryStream(fixture.Bytes, false)))
            return ArzRawRecordReader.ReadExact(input, strings[0], Limits());
    }
    static byte[] Large(int length, byte first)
    {
        var bytes = new byte[length];
        for (int i = 0; i < length; i++) bytes[i] = 120;
        if (length != 0) bytes[0] = first;
        return bytes;
    }

    static void RawIdentityAndBytes()
    {
        byte[][] strings = { B("z-last"), B("Class"), B(""), B("A\0B"),
            B("\u00e9"), B("e\u0301"), B("class") };
        var fixture = ArzRawFixture.Build(strings, new ArzRawFixtureRecord(0, new byte[0]));
        ArzRawRecord raw;
        var memory = new MemoryStream(fixture.Bytes, false);
        using (var input = new ResourceInput(memory))
            raw = ArzRawRecordReader.ReadExact(input, strings[0], Limits());
        Need(!memory.CanRead, "borrowed input closed by its owner before lazy pool creation");
        var pool = BinaryStringPool.FromUniqueRaw(raw);
        Need(pool.IsRawBacked && pool.IsUnique && pool.Count == 7 && pool.EmptyId == 2,
            "raw ID order and actual nonzero empty ID retained");
        Need(pool.MaterializedCount == 0 && pool.MaterializedBytes == 0,
            "uniqueness, sorting and empty lookup do not materialize strings");
        Need(pool.FindExact(B("Class")) == 1 && pool.FindExact(B("class")) == 6 &&
            pool.FindExact(B("CLASS")) == -1, "case-sensitive complete bytes");
        Need(pool.FindExact(B("A\0B")) == 3 && pool.FindExact(B("A")) == -1,
            "interior NUL does not shorten stored key");
        Need(pool.FindExact(B("\u00e9")) == 4 && pool.FindExact(B("e\u0301")) == 5,
            "UTF8 byte representations remain distinct original IDs");
        Need(pool.MaterializedCount == 0, "FindExact never turns absent memo into absent string");
        Need(raw.StringByteLength(3) == 3 && raw.CompareStringBytes(1, 6) < 0 &&
            raw.CompareStringToBytes(3, B("A\0B")) == 0 &&
            raw.CompareStringToBytes(3, B("A")) > 0 &&
            raw.CompareStringBytes(4, 5) > 0, "raw unsigned full-byte accessors");
        long payload = 0;
        for (int i = 0; i < strings.Length; i++) payload += strings[i].Length;
        Need(raw.PoolPayloadBytes == payload &&
            raw.RetainedArrayPayloadBytes == raw.StringTableBytes + 8L * strings.Length +
                strings[0].Length + 4L, "raw retained array accounting excludes dead input and packed arrays");
        Need(pool.RetainedArrayPayloadBytes == 12L * strings.Length &&
            pool.RawRetainedArrayPayloadBytes == raw.RetainedArrayPayloadBytes,
            "pool own x64 index/reference arrays and retained raw arrays reported separately");
        Array.Clear(fixture.Bytes, 0, fixture.Bytes.Length);
        strings[1][0] = 90;
        var exported = raw.CopyStringBytes(1); exported[0] = 90;
        TableString first = pool.Get(1);
        Need(Same(first.CopyCStringBytes(), B("Class\0")) &&
            Object.ReferenceEquals(first, pool[1]), "closed detached storage and stable lazy identity");
        var cstring = first.CopyCStringBytes(); cstring[0] = 90;
        Need(Same(pool.Get(1).CopyCStringBytes(), B("Class\0")) &&
            pool.MaterializedCount == 1 && pool.MaterializedBytes == 5,
            "exports cannot alter memo; repeated Get does not charge again");
        Need(pool.Get(2).Length == 0 && pool.MaterializedCount == 2 &&
            pool.MaterializedBytes == 5, "empty payload is a real ID and zero-byte materialization");
        Need(Throws<ArgumentOutOfRangeException>(delegate { pool.Get(-1); }, null) &&
            Throws<ArgumentOutOfRangeException>(delegate { pool.Get(7); }, null) &&
            Throws<ArgumentOutOfRangeException>(delegate { raw.StringByteLength(7); }, null) &&
            Throws<ArgumentOutOfRangeException>(delegate { raw.CompareStringBytes(0, 7); }, null),
            "original IDs validated without storage access");
        Need(Throws<ArgumentNullException>(delegate { pool.FindExact(null); }, null) &&
            Throws<ArgumentNullException>(delegate { raw.CompareStringToBytes(0, null); }, null) &&
            pool.FindExact(new byte[65537]) == -1, "invalid queries cannot change pool or fabricate match");
        scenarios++;
    }

    static void CompactCompatibility()
    {
        var first = new TableString(B("same"));
        var second = new TableString(B("same"));
        var supplied = new[] { first, second, new TableString(new byte[] { 255, 0, 128 }) };
        var pool = BinaryStringPool.FromExisting(supplied);
        supplied[0] = new TableString(B("changed"));
        Need(!pool.IsRawBacked && !pool.IsUnique && pool.EmptyId == -1 && pool.Count == 3,
            "legacy compact profile permits duplicate bytes and no empty string");
        Need(Object.ReferenceEquals(pool.Get(0), first) && Object.ReferenceEquals(pool.Get(1), second) &&
            pool.FindExact(B("same")) == 0, "cloned slots preserve distinct supplied identities and first original duplicate");
        Need(pool.FindExact(new byte[] { 255, 0, 128 }) == 2 &&
            pool.MaterializedCount == 3 && pool.MaterializedBytes == 11 &&
            pool.RawRetainedArrayPayloadBytes == 0, "compact profile accepts original arbitrary payload bytes");
        var empty = BinaryStringPool.FromExisting(new TableString[0]);
        Need(empty.IsUnique && empty.Count == 0 && empty.EmptyId == -1 &&
            empty.FindExact(new byte[0]) == -1, "legacy empty pool remains known and empty");
        var slots = new TableString[4096];
        var zero = new TableString(new byte[0]);
        for (int i = 0; i < slots.Length; i++) slots[i] = zero;
        var full = BinaryStringPool.FromExisting(slots);
        Need(full.Count == 4096 && full.EmptyId == 0 &&
            Object.ReferenceEquals(full.Get(4095), zero), "old maximum compact count remains usable above lazy count budget");
        Need(Throws<ArgumentOutOfRangeException>(delegate {
            BinaryStringPool.FromExisting(new TableString[4097]); }, "CompactCount"),
            "compact count preflight");
        var large = new TableString(new byte[65536]);
        var atByteLimit = new TableString[32];
        for (int i = 0; i < atByteLimit.Length; i++) atByteLimit[i] = large;
        Need(BinaryStringPool.FromExisting(atByteLimit).MaterializedBytes == 2097152,
            "old compact byte budget counts each supplied slot");
        var beyond = new TableString[33];
        for (int i = 0; i < beyond.Length; i++) beyond[i] = large;
        Need(Throws<ArgumentOutOfRangeException>(delegate {
            BinaryStringPool.FromExisting(beyond); }, "CompactBytes"),
            "compact byte budget still enforced");
        Need(Throws<ArgumentNullException>(delegate { BinaryStringPool.FromExisting(null); }, null) &&
            Throws<ArgumentException>(delegate { BinaryStringPool.FromExisting(new TableString[] { null }); }, null) &&
            Throws<ArgumentNullException>(delegate { BinaryStringPool.FromUniqueRaw(null); }, null),
            "unknown input remains unknown");
    }

    static void RawCoverage()
    {
        Need(Throws<InvalidDataException>(delegate {
            BinaryStringPool.FromUniqueRaw(Read(new[] { B("record"), B("record"), B("") })); }, "DuplicateRawString"),
            "duplicate nonempty raw strings cannot silently remap IDs");
        Need(Throws<InvalidDataException>(delegate {
            BinaryStringPool.FromUniqueRaw(Read(new[] { B(""), B("") })); }, "DuplicateRawString"),
            "duplicate empty IDs also rejected");
        Need(Throws<InvalidDataException>(delegate {
            BinaryStringPool.FromUniqueRaw(Read(new[] { B("record"), B("other") })); }, "MissingRawEmptyString"),
            "missing empty string cannot be fabricated at ID zero");
        Need(Throws<InvalidDataException>(delegate {
            BinaryStringPool.FromUniqueRaw(Read(new[] { B(""), Large(65537, 65) })); }, "StringOutside"),
            "single raw payload size checked before materialization");
        var overCount = new byte[131073][];
        for (int i = 0; i < overCount.Length; i++) overCount[i] = new byte[0];
        Need(Throws<InvalidDataException>(delegate {
            BinaryStringPool.FromUniqueRaw(Read(overCount)); }, "RawCount"),
            "raw string-count bound takes priority before sorting duplicate input");
        var atPayloadLimit = new byte[65][];
        atPayloadLimit[0] = new byte[0];
        for (int i = 1; i < atPayloadLimit.Length; i++)
            atPayloadLimit[i] = Large(65536, checked((byte)i));
        var pool = BinaryStringPool.FromUniqueRaw(Read(atPayloadLimit));
        Need(pool.Count == 65 && pool.MaterializedCount == 0 &&
            pool.FindExact(atPayloadLimit[64]) == 64, "four-MiB raw payload is indexed without eager copies");
        TableString saved = null;
        for (int i = 1; i <= 4; i++) { var value = pool.Get(i); if (i == 1) saved = value; }
        Need(pool.MaterializedCount == 4 && pool.MaterializedBytes == 262144,
            "four maximum strings meet lazy byte budget exactly");
        Need(Throws<InvalidOperationException>(delegate { pool.Get(5); }, "MaterializationBytes") &&
            pool.MaterializedCount == 4 && pool.MaterializedBytes == 262144 &&
            Object.ReferenceEquals(saved, pool.Get(1)) && pool.FindExact(atPayloadLimit[5]) == 5,
            "lazy byte rejection preserves existing identities and complete lookup coverage");
        Need(pool.Get(0).Length == 0 && pool.MaterializedCount == 5 && pool.MaterializedBytes == 262144,
            "zero-byte empty ID can materialize at exact payload budget");
        var overPayload = new byte[66][];
        Array.Copy(atPayloadLimit, overPayload, atPayloadLimit.Length);
        overPayload[65] = B("q");
        Need(Throws<InvalidDataException>(delegate {
            BinaryStringPool.FromUniqueRaw(Read(overPayload)); }, "RawPayload"),
            "raw payload one byte above four MiB rejected before memo/index allocation");
    }

    static void LazyCountAndLargeIdDomain()
    {
        var strings = new byte[5002][];
        strings[0] = new byte[0];
        for (int i = 1; i < strings.Length; i++) strings[i] = B("id-" + i.ToString("D5"));
        var pool = BinaryStringPool.FromUniqueRaw(Read(strings));
        Need(pool.Count == 5002 && pool.FindExact(strings[5001]) == 5001 &&
            pool.MaterializedCount == 0, "raw pool preserves IDs beyond legacy 4096 count");
        TableString first = null;
        for (int i = 1; i <= 1024; i++)
        {
            var value = pool.Get(i);
            if (i == 1) first = value;
        }
        Need(pool.MaterializedCount == 1024 && pool.MaterializedBytes == 8192,
            "lazy count exactly 1024 with small payloads");
        Need(Throws<InvalidOperationException>(delegate { pool.Get(1025); }, "MaterializationCount") &&
            pool.MaterializedCount == 1024 && pool.MaterializedBytes == 8192,
            "1025th new string rejected without changing counters");
        Need(Object.ReferenceEquals(first, pool.Get(1)) && pool.FindExact(strings[5001]) == 5001 &&
            pool.FindExact(new byte[0]) == 0, "budget exhaustion does not evict existing strings or hide unmaterialized IDs");
        scenarios++;
    }

    static void Main()
    {
        RawIdentityAndBytes(); CompactCompatibility(); RawCoverage(); LazyCountAndLargeIdDomain();
        Console.WriteLine("{\"suite\":\"BinaryStringPoolTests\",\"status\":\"passed\",\"fixed_assertions\":" +
            assertions + ",\"scenarios\":" + scenarios +
            ",\"scope\":\"own synthetic ARZv3 original-ID pool and bounded lazy storage; no native memory or game data\"}");
    }
}
