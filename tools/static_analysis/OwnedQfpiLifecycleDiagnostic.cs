using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace GrimDynasty.StaticAnalysis.Diagnostics
{
    // Standalone observations, never a sampler replacement or identity fallback.
    // There is no arbitrary executable, PID, output, retry or fault-injection API.
    public static class OwnedQfpiLifecycleDiagnostic
    {
        private const string Workspace = @"C:\Users\zhuzi\Documents\Grim-Dynasty";
        private const string Output = Workspace + @"\.tooling\r0-qfpi-lifecycle-diagnostic-20261022";
        private const string Node = @"D:\NodeJS\node.exe";
        private const string NodeSha = "63c259c81e5d472b5f11c8d506070130cb04a1ecf84b80377a34ed6ec9048088";
        private const uint Timeout = 258, Failed = 0xffffffff;
        private static readonly int[] Delays = { 0, 20, 80, 160, 5000, 5000, 5000, 5000 };
        private static StreamWriter log;
        private static int launched, cleaned, observations, imageErrors, error5Unsignaled, error5ThenSignaled;
        private static ulong runStarted;

        public static int Main(string[] args)
        {
            if (args.Length != 0) return 2;
            int exit = 1;
            bool outputValidated = false;
            string stage = "guards", failure = null;
            try
            {
                ValidateLayout();
                Normal(Output, true); outputValidated = true; Normal(Node, false);
                string executable = System.Reflection.Assembly.GetExecutingAssembly().Location;
                Require(String.Equals(executable, Output + @"\OwnedQfpiLifecycleDiagnostic.exe", StringComparison.OrdinalIgnoreCase), "Fixed fixture executable required");
                Require(Hash(Node) == NodeSha, "Node fingerprint changed");
                log = new StreamWriter(new FileStream(Output + @"\observations.ndjson", FileMode.CreateNew, FileAccess.Write, FileShare.Read), new UTF8Encoding(false));
                log.AutoFlush = true;
                runStarted = Native.GetTickCount64();
                for (int i = 0; i < 8; ++i)
                {
                    Require(Native.GetTickCount64() - runStarted < 20000, "Total observation deadline");
                    stage = "child-" + i.ToString(CultureInfo.InvariantCulture);
                    RunChild(i);
                }
                stage = "final-fingerprints";
                Require(Hash(Node) == NodeSha, "Node final fingerprint changed");
                Require(launched == 8 && cleaned == 8, "All exact created processes must exit");
                exit = 0;
            }
            catch (Exception error)
            {
                NativeFailure native = error as NativeFailure;
                failure = "{\"type\":" + Q(error.GetType().FullName) + ",\"operation\":" + Q(native == null ? null : native.Operation) +
                    ",\"native_error\":" + (native == null ? "null" : N(native.Error)) +
                    ",\"guard_reason\":" + Q(error is InvalidOperationException ? error.Message : null) + "}";
            }
            finally
            {
                if (log != null) { log.Dispose(); log = null; }
            }
            // CreateNew preserves an earlier attempt, including its failures.
            if (!outputValidated) { Console.Error.WriteLine("Diagnostic output guard failed; no file written."); return exit; }
            Normal(Output, true);
            string result = "{\"schema\":\"owned-qfpi-lifecycle-v1\",\"status\":" + Q(exit == 0 ? "completed-observations" : "failed") +
                ",\"stage\":" + Q(stage) + ",\"failure\":" + (failure ?? "null") +
                ",\"children_launched\":" + N(launched) + ",\"children_exit_confirmed\":" + N(cleaned) +
                ",\"observations\":" + N(observations) + ",\"image_query_failures\":" + N(imageErrors) +
                ",\"error5_then_unsignaled_observations\":" + N(error5Unsignaled) +
                ",\"children_with_error5_unsignaled_then_signaled\":" + N(error5ThenSignaled) +
                ",\"node_sha256\":" + Q(NodeSha) + ",\"production_guard_modified\":false,\"identity_fallback\":false," +
                "\"original_pid_identity_inferred\":false,\"root_cause_proven\":false," +
                "\"limitations\":[\"Only fixed self-created Node processes; no Java, Rea, Ghidra or game.\"," +
                "\"First observation is after CreateProcess returns, so the earlier creation window is not covered.\"," +
                "\"Four natural exits and four deliberate owned-Job terminations are labeled separately.\"," +
                "\"Each repeated observation remains raw; later success never replaces an earlier failure.\"," +
                "\"An error5/unsignaled/signaled sequence would show one possible lifecycle sequence, not identify the original PID or prove the R0 cause.\"]}";
            using (StreamWriter writer = new StreamWriter(new FileStream(Output + @"\result.json", FileMode.CreateNew, FileAccess.Write, FileShare.Read), new UTF8Encoding(false)))
                writer.WriteLine(result);
            Console.WriteLine("Owned QFPI lifecycle diagnostic: " + (exit == 0 ? "completed observations" : "failed") + "; children=" + launched + "; observations=" + observations);
            return exit;
        }

        private static void RunChild(int index)
        {
            Handle job = null, created = null, thread = null, query = null;
            IntPtr attributes = IntPtr.Zero, jobList = IntPtr.Zero, environment = IntPtr.Zero;
            bool initialized = false, sawError5Unsignaled = false, signaled = false, forced = false, cleanupConfirmed = false;
            uint pid = 0;
            long creation = 0;
            try
            {
                job = Native.CreateJobObjectW(IntPtr.Zero, null);
                if (job.IsInvalid) ThrowLast("CreateJobObjectW");
                Limits limits = new Limits(); limits.Basic.Flags = 0x2000;
                uint size = (uint)Marshal.SizeOf(typeof(Limits));
                if (!Native.SetInformationJobObject(job, 9, ref limits, size)) ThrowLast("SetInformationJobObject");
                Limits readback; uint returned;
                if (!Native.QueryLimits(job, 9, out readback, size, out returned)) ThrowLast("QueryInformationJobObject(limits)");
                Require(readback.Basic.Flags == 0x2000, "Only KILL_ON_JOB_CLOSE is allowed");
                UIntPtr bytes = UIntPtr.Zero;
                bool sized = Native.InitializeProcThreadAttributeList(IntPtr.Zero, 1, 0, ref bytes);
                int sizingError = Marshal.GetLastWin32Error();
                Require(!sized && sizingError == 122 && bytes.ToUInt64() > 0 && bytes.ToUInt64() <= 65536, "Attribute sizing");
                attributes = Marshal.AllocHGlobal((int)bytes.ToUInt64());
                if (!Native.InitializeProcThreadAttributeList(attributes, 1, 0, ref bytes)) ThrowLast("InitializeProcThreadAttributeList");
                initialized = true;
                jobList = Marshal.AllocHGlobal(IntPtr.Size); Marshal.WriteIntPtr(jobList, job.DangerousGetHandle());
                if (!Native.UpdateProcThreadAttribute(attributes, 0, new IntPtr(0x2000d), jobList,
                    new UIntPtr((uint)IntPtr.Size), IntPtr.Zero, IntPtr.Zero)) ThrowLast("UpdateProcThreadAttribute(JOB_LIST)");
                StartupEx startup = new StartupEx(); startup.Info.Size = (uint)Marshal.SizeOf(typeof(StartupEx)); startup.Attributes = attributes;
                // Isolated child environment: no NODE_OPTIONS/NODE_PATH/preload inheritance.
                environment = Marshal.StringToHGlobalUni("PATH=D:\\NodeJS;C:\\Windows\\System32\0SystemRoot=C:\\Windows\0TEMP=" + Output +
                    "\0TMP=" + Output + "\0WINDIR=C:\\Windows\0\0");
                StringBuilder command = new StringBuilder("\"" + Node + "\" -e \"setTimeout(()=>{}," + N(Delays[index]) + ")\"");
                ProcessInfo process;
                if (!Native.CreateProcessW(Node, command, IntPtr.Zero, IntPtr.Zero, false,
                    0x08080404, environment, Output, ref startup, out process)) ThrowLast("CreateProcessW");
                created = new Handle(process.Process); thread = new Handle(process.Thread); pid = process.Pid; launched++;
                Require(!created.IsInvalid && !thread.IsInvalid && pid > 0 && pid <= Int32.MaxValue, "Created process handles");
                creation = Creation(created);
                // Exactly one limited-rights OpenProcess, while the returned creation
                // handle remains retained. No PID is reopened after any observation.
                query = Native.OpenProcess(0x00101000, false, (int)pid);
                if (query.IsInvalid) ThrowLast("OpenProcess(query)");
                Require(Creation(query) == creation, "Created/query handle identity mismatch");
                bool belongs;
                if (!Native.IsProcessInJob(query, job, out belongs)) ThrowLast("IsProcessInJob(initial)");
                Require(belongs, "Atomic Job assignment failed");
                Require(Native.WaitForSingleObject(created, 0) == Timeout, "Suspended child already exited");
                Observe(index, pid, creation, query, job, "suspended", ref sawError5Unsignaled);
                uint suspend = Native.ResumeThread(thread);
                if (suspend == Failed) ThrowLast("ResumeThread");
                Require(suspend == 1, "Unexpected suspend count");
                ulong resumed = Native.GetTickCount64();
                for (int sample = 0; sample < 128; ++sample)
                {
                    if (index >= 4 && !forced && Native.GetTickCount64() - resumed >= 40)
                    {
                        if (!Native.TerminateJobObject(job, 94)) ThrowLast("TerminateJobObject(controlled)");
                        forced = true;
                    }
                    signaled = Observe(index, pid, creation, query, job, forced ? "after-controlled-termination" : "after-resume", ref sawError5Unsignaled);
                    if (signaled) break;
                    Require(Native.GetTickCount64() - resumed <= 1500, "Child observation deadline");
                    Native.Sleep(5);
                }
                Require(signaled, "No exit signal within bounded observation window");
                if (index >= 4) Require(forced, "Controlled child exited before scheduled termination");
                if (sawError5Unsignaled) error5ThenSignaled++;
                // One final observation keeps failures separate even after exit.
                Require(Observe(index, pid, creation, query, job, "exit-confirmed", ref sawError5Unsignaled), "Signaled process reverted");
            }
            finally
            {
                // The atomic JOB_LIST avoids an unassigned-child leak if owner dies.
                // Close only this Job; never terminate a process by PID or touch others.
                if (job != null) { job.Dispose(); job = null; }
                try
                {
                    if (created != null && !created.IsInvalid)
                    {
                        uint wait = Native.WaitForSingleObject(created, 2000);
                        int waitError = wait == Failed ? Marshal.GetLastWin32Error() : 0;
                        cleanupConfirmed = wait == 0;
                        if (cleanupConfirmed) cleaned++;
                        log.WriteLine("{\"event\":\"cleanup\",\"child_index\":" + N(index) + ",\"pid\":" + N(pid) +
                            ",\"creation_ticks\":" + Q(creation == 0 ? null : DateTime.FromFileTimeUtc(creation).Ticks.ToString(CultureInfo.InvariantCulture)) +
                            ",\"created_handle_wait\":" + N(wait) + ",\"wait_error\":" + N(waitError) +
                            ",\"exit_confirmed\":" + (cleanupConfirmed ? "true" : "false") + "}");
                        Require(cleanupConfirmed, "Owned child cleanup unconfirmed");
                    }
                }
                finally
                {
                    if (query != null) query.Dispose();
                    if (thread != null) thread.Dispose();
                    if (created != null) created.Dispose();
                    if (initialized) Native.DeleteProcThreadAttributeList(attributes);
                    if (attributes != IntPtr.Zero) Marshal.FreeHGlobal(attributes);
                    if (jobList != IntPtr.Zero) Marshal.FreeHGlobal(jobList);
                    if (environment != IntPtr.Zero) Marshal.FreeHGlobal(environment);
                }
            }
        }

        private static bool Observe(int index, uint pid, long creation, Handle query, Handle job, string phase, ref bool sawError5)
        {
            Require(observations < 1040, "Observation count budget");
            ulong started = Native.GetTickCount64();
            Require(Creation(query) == creation, "Retained handle creation changed");
            bool member;
            if (!Native.IsProcessInJob(query, job, out member)) ThrowLast("IsProcessInJob(observation)");
            uint before = Native.WaitForSingleObject(query, 0); int beforeError = before == Failed ? Marshal.GetLastWin32Error() : 0;
            StringBuilder image = new StringBuilder(32768); uint chars = (uint)image.Capacity;
            ulong queryStarted = Native.GetTickCount64();
            bool ok = Native.QueryFullProcessImageNameW(query, 0, image, ref chars);
            int imageError = ok ? 0 : Marshal.GetLastWin32Error();
            ulong imageCompleted = Native.GetTickCount64();
            uint after = Native.WaitForSingleObject(query, 0); int afterError = after == Failed ? Marshal.GetLastWin32Error() : 0;
            if (!ok) imageErrors++;
            if (!ok && imageError == 5 && after == Timeout) { error5Unsignaled++; sawError5 = true; }
            observations++;
            log.WriteLine("{\"event\":\"observation\",\"child_index\":" + N(index) + ",\"kind\":" + Q(index < 4 ? "natural-exit" : "controlled-job-termination") +
                ",\"phase\":" + Q(phase) + ",\"pid\":" + N(pid) + ",\"creation_utc\":" + Q(DateTime.FromFileTimeUtc(creation).ToString("o")) +
                ",\"creation_ticks\":" + Q(DateTime.FromFileTimeUtc(creation).Ticks.ToString(CultureInfo.InvariantCulture)) +
                ",\"utc_after\":" + Q(DateTime.UtcNow.ToString("o")) + ",\"in_job\":" + (member ? "true" : "false") +
                ",\"relative_start_ms\":" + N(started - runStarted) + ",\"qfpi_elapsed_ms\":" + N(imageCompleted - queryStarted) +
                ",\"wait_before\":" + N(before) + ",\"wait_before_error\":" + N(beforeError) +
                ",\"qfpi_success\":" + (ok ? "true" : "false") + ",\"qfpi_error\":" + N(imageError) +
                ",\"reported_characters\":" + N(chars) + ",\"image_path\":" + Q(ok ? image.ToString() : null) +
                ",\"wait_after\":" + N(after) + ",\"wait_after_error\":" + N(afterError) + ",\"same_retained_query_handle\":true}");
            Require(before == 0 || before == Timeout, "Unexpected initial wait result");
            Require(after == 0 || after == Timeout, "Unexpected final wait result");
            Require(member || after == 0, "Unsignaled process lost owned Job membership");
            if (ok) Require(chars > 0 && String.Equals(image.ToString(), Node, StringComparison.OrdinalIgnoreCase), "Unexpected created executable identity");
            // A QFPI failure is kept as raw diagnostic data only. It is never a
            // successful probe, admitted role, counter sample, or production retry.
            return after == 0;
        }

        private static long Creation(Handle process)
        {
            FileTime create, exit, kernel, user;
            if (!Native.GetProcessTimes(process, out create, out exit, out kernel, out user)) ThrowLast("GetProcessTimes");
            return checked((long)(((ulong)create.High << 32) | create.Low));
        }
        private static void Normal(string p, bool directory)
        {
            p = Path.GetFullPath(p); bool first = true;
            while (p != null)
            {
                FileAttributes a = File.GetAttributes(p);
                Require((a & FileAttributes.ReparsePoint) == 0, "Redirected path");
                Require(((a & FileAttributes.Directory) != 0) == (first ? directory : true), "Path type");
                DirectoryInfo parent = Directory.GetParent(p); p = parent == null ? null : parent.FullName; first = false;
            }
        }
        private static string Hash(string p)
        {
            using (FileStream stream = new FileStream(p, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (SHA256 sha = SHA256.Create())
            { return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant(); }
        }
        private static string N(object n) { return Convert.ToString(n, CultureInfo.InvariantCulture); }
        private static string Q(string s)
        {
            if (s == null) return "null";
            StringBuilder b = new StringBuilder("\"");
            foreach (char c in s)
            {
                if (c == '\\' || c == '"') b.Append('\\').Append(c);
                else if (c < 32) b.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                else b.Append(c);
            }
            return b.Append('"').ToString();
        }
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        private static void ThrowLast(string operation) { throw new NativeFailure(operation, Marshal.GetLastWin32Error()); }
        private sealed class NativeFailure : Exception
        { internal readonly string Operation; internal readonly int Error; internal NativeFailure(string op, int error) { Operation = op; Error = error; } }
        private static void ValidateLayout()
        {
            Require(IntPtr.Size == 8 && Environment.OSVersion.Platform == PlatformID.Win32NT, "Windows x64 only");
            Require(Marshal.SizeOf(typeof(Startup)) == 104 && Marshal.SizeOf(typeof(StartupEx)) == 112 &&
                Marshal.SizeOf(typeof(ProcessInfo)) == 24 && Marshal.SizeOf(typeof(Limits)) == 144, "Native layout mismatch");
        }
        private sealed class Handle : SafeHandleZeroOrMinusOneIsInvalid
        { public Handle() : base(true) { } internal Handle(IntPtr h) : base(true) { SetHandle(h); } protected override bool ReleaseHandle() { return Native.CloseHandle(handle); } }
        [StructLayout(LayoutKind.Sequential)] private struct FileTime { public uint Low, High; }
        [StructLayout(LayoutKind.Sequential)] private struct BasicLimits
        { public long PerProcess, PerJob; public uint Flags; public UIntPtr Minimum, Maximum; public uint Active; public UIntPtr Affinity; public uint Priority, Scheduling; }
        [StructLayout(LayoutKind.Sequential)] private struct Io
        { public ulong ReadOps, WriteOps, OtherOps, ReadBytes, WriteBytes, OtherBytes; }
        [StructLayout(LayoutKind.Sequential)] private struct Limits
        { public BasicLimits Basic; public Io Counters; public UIntPtr ProcessMemory, JobMemory, PeakProcess, PeakJob; }
        [StructLayout(LayoutKind.Sequential)] private struct Startup
        { public uint Size; public IntPtr Reserved, Desktop, Title; public uint X, Y, XSize, YSize, XChars, YChars, Fill, Flags; public ushort Show, ReservedSize; public IntPtr Reserved2, Input, Output, Error; }
        [StructLayout(LayoutKind.Sequential)] private struct StartupEx { public Startup Info; public IntPtr Attributes; }
        [StructLayout(LayoutKind.Sequential)] private struct ProcessInfo { public IntPtr Process, Thread; public uint Pid, Tid; }
        private static class Native
        {
            [DllImport("kernel32.dll", CharSet=CharSet.Unicode, ExactSpelling=true, SetLastError=true)] internal static extern Handle CreateJobObjectW(IntPtr sa, string name);
            [DllImport("kernel32.dll", SetLastError=true)] [return:MarshalAs(UnmanagedType.Bool)] internal static extern bool SetInformationJobObject(Handle job, int infoClass, ref Limits info, uint size);
            [DllImport("kernel32.dll", EntryPoint="QueryInformationJobObject", SetLastError=true)] [return:MarshalAs(UnmanagedType.Bool)] internal static extern bool QueryLimits(Handle job, int infoClass, out Limits info, uint size, out uint returned);
            [DllImport("kernel32.dll", SetLastError=true)] [return:MarshalAs(UnmanagedType.Bool)] internal static extern bool InitializeProcThreadAttributeList(IntPtr list, uint count, uint flags, ref UIntPtr size);
            [DllImport("kernel32.dll", SetLastError=true)] [return:MarshalAs(UnmanagedType.Bool)] internal static extern bool UpdateProcThreadAttribute(IntPtr list, uint flags, IntPtr attr, IntPtr value, UIntPtr size, IntPtr old, IntPtr returned);
            [DllImport("kernel32.dll")] internal static extern void DeleteProcThreadAttributeList(IntPtr list);
            [DllImport("kernel32.dll", CharSet=CharSet.Unicode, ExactSpelling=true, SetLastError=true)] [return:MarshalAs(UnmanagedType.Bool)] internal static extern bool CreateProcessW(string app, StringBuilder cmd, IntPtr sa, IntPtr ta, [MarshalAs(UnmanagedType.Bool)] bool inherit, uint flags, IntPtr env, string cwd, ref StartupEx startup, out ProcessInfo process);
            [DllImport("kernel32.dll", SetLastError=true)] internal static extern Handle OpenProcess(uint rights, [MarshalAs(UnmanagedType.Bool)] bool inherit, int pid);
            [DllImport("kernel32.dll", SetLastError=true)] [return:MarshalAs(UnmanagedType.Bool)] internal static extern bool IsProcessInJob(Handle process, Handle job, [MarshalAs(UnmanagedType.Bool)] out bool member);
            [DllImport("kernel32.dll", SetLastError=true)] [return:MarshalAs(UnmanagedType.Bool)] internal static extern bool GetProcessTimes(Handle process, out FileTime created, out FileTime exit, out FileTime kernel, out FileTime user);
            [DllImport("kernel32.dll", CharSet=CharSet.Unicode, ExactSpelling=true, SetLastError=true)] [return:MarshalAs(UnmanagedType.Bool)] internal static extern bool QueryFullProcessImageNameW(Handle process, uint flags, StringBuilder name, ref uint chars);
            [DllImport("kernel32.dll", SetLastError=true)] internal static extern uint ResumeThread(Handle thread);
            [DllImport("kernel32.dll", SetLastError=true)] internal static extern uint WaitForSingleObject(Handle handle, uint milliseconds);
            [DllImport("kernel32.dll", SetLastError=true)] [return:MarshalAs(UnmanagedType.Bool)] internal static extern bool TerminateJobObject(Handle job, uint code);
            [DllImport("kernel32.dll")] internal static extern ulong GetTickCount64();
            [DllImport("kernel32.dll")] internal static extern void Sleep(uint milliseconds);
            [DllImport("kernel32.dll", SetLastError=true)] [return:MarshalAs(UnmanagedType.Bool)] internal static extern bool CloseHandle(IntPtr handle);
        }
    }
}
