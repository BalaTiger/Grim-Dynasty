using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace GrimDynasty.StaticAnalysis.DiagnosticRune13b268184764304bd0a6fe1bc63f3de.Image87
{
    public sealed class ReaProcessProbe
    {
        public int ProcessId { get; internal set; }
        public DateTime CreationUtc { get; internal set; }
        // DateTime UTC ticks (100 ns since 0001-01-01), not Unix milliseconds.
        public long CreationTicks { get { return CreationUtc.Ticks; } }
        public bool InJob { get; internal set; }
        public bool HasExited { get; internal set; }
        public string ExecutablePath { get; internal set; }
        // Missing image identity is allowed only after this same handle proves
        // exit. It must never authorize role classification or live adoption.
        public int? ExecutablePathErrorCode { get; internal set; }
        public string ExecutablePathDiagnostic { get; internal set; }
    }

    public sealed class ReaJobMembers
    {
        public int[] NativeProcessIds { get; internal set; }
        public ReaProcessProbe[] Members { get; internal set; }
        public int[] GoneProcessIds { get; internal set; }
        // A PID from the list now resolves to a process outside this exact Job.
        public int[] NonMemberProcessIds { get; internal set; }
        public uint NativeAssignedCount { get; internal set; }
        public uint TotalProcesses { get; internal set; }
        public uint ActiveProcesses { get; internal set; }
        public uint TotalTerminatedProcesses { get; internal set; }
    }

    // Owns one unnamed, non-inheritable Job. There is no API to adopt another Job,
    // add later arbitrary processes, or terminate a process by PID.
    //
    // Before calling the constructor, the sampler MUST validate the newly spawned
    // IPC-gated wrapper's full argv, nonce, executable and controller parent PID.
    // The wrapper must remain gated until assignment and the first valid sample.
    public sealed class ReaOwnedJob : IDisposable
    {
        private const uint QueryLimitedInformation = 0x1000;
        private const uint Synchronize = 0x100000;
        private const uint SetQuota = 0x0100;
        private const uint TerminateProcessAccess = 0x0001;
        private const uint KillOnJobClose = 0x2000;
        private const uint WaitObject0 = 0;
        private const uint WaitTimeout = 258;
        private const int ErrorInvalidParameter = 87;
        private const int ErrorMoreData = 234;
        private const int MaximumMembers = 65536;
        private NativeHandle job;
        private NativeHandle root;
        private readonly object lifecycle = new object();
        private NativeHandle controller;
        private EventWaitHandle controllerWatchStop;
        private Thread controllerWatcher;
        private volatile bool controllerExitObserved;
        private volatile string controllerTerminationError;

        public int RootProcessId { get; private set; }
        public DateTime RootCreationUtc { get; private set; }
        public string RootExecutablePath { get; private set; }
        public uint LimitFlags { get; private set; }
        public bool ControllerExitObserved { get { return controllerExitObserved; } }
        public string ControllerTerminationError { get { return controllerTerminationError; } }

        public ReaOwnedJob(int expectedRootPid, DateTime expectedCreationUtc, string expectedExe)
        {
            ValidateLayout();
            if (expectedRootPid <= 0 || expectedRootPid == Native.GetCurrentProcessId())
                throw new ArgumentException("Root must be the separate newly spawned gated wrapper.", "expectedRootPid");
            if (expectedCreationUtc.Kind != DateTimeKind.Utc)
                throw new ArgumentException("Expected creation time must explicitly be UTC.", "expectedCreationUtc");
            if (String.IsNullOrWhiteSpace(expectedExe) || !Path.IsPathRooted(expectedExe))
                throw new ArgumentException("Expected executable must be an absolute path.", "expectedExe");
            string expectedPath = Path.GetFullPath(expectedExe);
            NativeHandle openedRoot = null;
            NativeHandle createdJob = null;
            try
            {
                // AssignProcessToJobObject requires SET_QUOTA and TERMINATE.
                // This same handle pins the verified process through assignment.
                openedRoot = Open(expectedRootPid, QueryLimitedInformation | Synchronize | SetQuota | TerminateProcessAccess);
                DateTime actualCreation = ReadCreation(openedRoot);
                if (Math.Abs(actualCreation.Ticks - expectedCreationUtc.Ticks) > 10)
                    throw new InvalidOperationException("Root creation identity differs by more than one microsecond.");
                string actualPath = ReadExecutable(openedRoot);
                if (!String.Equals(Path.GetFullPath(actualPath), expectedPath, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Root executable identity differs.");
                if (ReadExited(openedRoot))
                    throw new InvalidOperationException("The gated root has already exited.");

                createdJob = Native.CreateJobObject(IntPtr.Zero, null);
                if (createdJob.IsInvalid) throw LastError("CreateJobObject");
                ExtendedLimitInformation limits = new ExtendedLimitInformation();
                limits.BasicLimitInformation.LimitFlags = KillOnJobClose;
                if (!Native.SetInformationJobObject(createdJob, 9, ref limits, (uint)Marshal.SizeOf(typeof(ExtendedLimitInformation))))
                    throw LastError("SetInformationJobObject");
                ExtendedLimitInformation readback;
                uint returned;
                if (!Native.QueryExtendedLimits(createdJob, 9, out readback, (uint)Marshal.SizeOf(typeof(ExtendedLimitInformation)), out returned))
                    throw LastError("QueryInformationJobObject(limits)");
                if (readback.BasicLimitInformation.LimitFlags != KillOnJobClose)
                    throw new InvalidOperationException("Job must have only KILL_ON_JOB_CLOSE, with no breakaway flags.");
                if (!Native.AssignProcessToJobObject(createdJob, openedRoot))
                    throw LastError("AssignProcessToJobObject");
                bool belongs;
                if (!Native.IsProcessInJob(openedRoot, createdJob, out belongs))
                    throw LastError("IsProcessInJob(root)");
                if (!belongs || ReadExited(openedRoot))
                    throw new InvalidOperationException("Root did not remain live in this Job.");

                RootProcessId = expectedRootPid;
                RootCreationUtc = actualCreation;
                RootExecutablePath = actualPath;
                LimitFlags = readback.BasicLimitInformation.LimitFlags;
                job = createdJob;
                root = openedRoot;
                createdJob = null;
                openedRoot = null;
            }
            finally
            {
                // If setup failed after assignment, close only our own Job.
                if (createdJob != null) createdJob.Dispose();
                if (openedRoot != null) openedRoot.Dispose();
            }
        }

        public ReaProcessProbe Probe(int processId)
        {
            lock (lifecycle)
            {
                CheckOpen();
                DateTime? creation = null;
                bool? belongs = null;
                int? imageErrorCode = null;
                bool? exitAfterImageFailure = null;
                int? exitCheckErrorCode = null;
                string step = "OpenProcess";
                try
                {
                    using (NativeHandle process = Open(processId, QueryLimitedInformation | Synchronize))
                    {
                        step = "GetProcessTimes";
                        creation = ReadCreation(process);
                        step = "IsProcessInJob";
                        bool member;
                        if (!Native.IsProcessInJob(process, job, out member)) throw LastError("IsProcessInJob");
                        belongs = member;
                        string executablePath = null;
                        int? executablePathErrorCode = null;
                        string executablePathDiagnostic = null;
                        step = "QueryFullProcessImageName";
                        try { executablePath = OwnedJobDiagnosticFixture.FailImage87(); }
                        catch (Win32Exception error)
                        {
                            // Image lookup can race with exit after membership was
                            // observed. Keep the same handle; never re-open this PID.
                            // A live process or an unqueryable exit state still fails.
                            imageErrorCode = error.NativeErrorCode;
                            step = "WaitForSingleObject(after image failure)";
                            try { exitAfterImageFailure = ReadExited(process); }
                            catch (Win32Exception exitError)
                            {
                                exitCheckErrorCode = exitError.NativeErrorCode;
                                throw;
                            }
                            if (!exitAfterImageFailure.Value)
                            {
                                step = "QueryFullProcessImageName";
                                throw;
                            }
                            executablePathErrorCode = error.NativeErrorCode;
                            executablePathDiagnostic = "Image query failed; the same process handle subsequently confirmed exit.";
                        }
                        step = "WaitForSingleObject(final)";
                        bool hasExited;
                        try { hasExited = ReadExited(process); }
                        catch (Win32Exception exitError)
                        {
                            exitCheckErrorCode = exitError.NativeErrorCode;
                            throw;
                        }
                        return new ReaProcessProbe {
                            ProcessId = processId,
                            CreationUtc = creation.Value,
                            InJob = belongs.Value,
                            HasExited = hasExited,
                            ExecutablePath = executablePath,
                            ExecutablePathErrorCode = executablePathErrorCode,
                            ExecutablePathDiagnostic = executablePathDiagnostic
                        };
                    }
                }
                catch (Win32Exception error)
                {
                    // Diagnostics add only facts already read on the same handle.
                    // Keep the original exception type/code and fail-closed path.
                    // No command line, environment, arbitrary error text or retry.
                    error.Data["ReaProbeProcessId"] = processId;
                    if (creation.HasValue) error.Data["ReaProbeCreationUtc"] = creation.Value;
                    if (belongs.HasValue) error.Data["ReaProbeInJob"] = belongs.Value;
                    error.Data["ReaProbeStep"] = step;
                    if (imageErrorCode.HasValue) error.Data["ReaProbeImageErrorCode"] = imageErrorCode.Value;
                    if (exitAfterImageFailure.HasValue) error.Data["ReaProbeExitAfterImageFailure"] = exitAfterImageFailure.Value;
                    if (exitCheckErrorCode.HasValue) error.Data["ReaProbeExitCheckErrorCode"] = exitCheckErrorCode.Value;
                    throw;
                }
            }
        }

        public ReaJobMembers QueryMembers()
        {
            lock (lifecycle)
            {
            CheckOpen();
            uint assigned;
            int[] ids = ReadMemberIds(out assigned);
            List<ReaProcessProbe> members = new List<ReaProcessProbe>();
            List<int> gone = new List<int>();
            List<int> nonMembers = new List<int>();
            for (int memberIndex = 0; memberIndex < ids.Length; memberIndex++)
            {
                int id = ids[memberIndex];
                ReaProcessProbe probe;
                try { probe = Probe(id); }
                catch (Win32Exception error)
                {
                    error.Data["ReaProbeFromQueryMembers"] = true;
                    error.Data["ReaProbeMemberIndex"] = memberIndex;
                    error.Data["ReaProbeMemberCount"] = ids.Length;
                    // Only OpenProcess's no-longer-valid PID result is a normal
                    // enumeration race. Access denied and all other errors fail.
                    if (error.NativeErrorCode == ErrorInvalidParameter && error.Data.Contains("OpenProcess"))
                    { gone.Add(id); continue; }
                    throw;
                }
                if (probe.InJob) members.Add(probe);
                else nonMembers.Add(id);
            }
            BasicAccountingInformation accounting;
            uint returned;
            if (!Native.QueryAccounting(job, 1, out accounting, (uint)Marshal.SizeOf(typeof(BasicAccountingInformation)), out returned))
                throw LastError("QueryInformationJobObject(accounting)");
            // List, individual probes, and accounting are separate observations.
            // Do not interpret them as an atomic snapshot or sum lifetime peaks.
            return new ReaJobMembers {
                NativeProcessIds = ids, Members = members.ToArray(),
                GoneProcessIds = gone.ToArray(), NonMemberProcessIds = nonMembers.ToArray(),
                NativeAssignedCount = assigned,
                TotalProcesses = accounting.TotalProcesses,
                ActiveProcesses = accounting.ActiveProcesses,
                TotalTerminatedProcesses = accounting.TotalTerminatedProcesses
            };
            }
        }

        public void Terminate(uint exitCode)
        {
            lock (lifecycle)
            {
                CheckOpen();
                if (!Native.TerminateJobObject(job, exitCode)) throw LastError("TerminateJobObject");
            }
        }

        public void WatchController(int processId, DateTime expectedCreationUtc)
        {
            lock (lifecycle)
            {
                CheckOpen();
                if (controllerWatcher != null) throw new InvalidOperationException("Controller watch is already configured.");
                if (processId <= 0 || processId == RootProcessId || processId == Native.GetCurrentProcessId())
                    throw new ArgumentException("Controller must be separate from the wrapper and sampler.", "processId");
                if (expectedCreationUtc.Kind != DateTimeKind.Utc)
                    throw new ArgumentException("Expected controller creation time must explicitly be UTC.", "expectedCreationUtc");
                NativeHandle openedController = Open(processId, QueryLimitedInformation | Synchronize);
                try
                {
                    if (Math.Abs(ReadCreation(openedController).Ticks - expectedCreationUtc.Ticks) > 10)
                        throw new InvalidOperationException("Controller creation identity differs by more than one microsecond.");
                    controller = openedController;
                    controllerWatchStop = new EventWaitHandle(false, EventResetMode.ManualReset);
                    controllerWatcher = new Thread(WatchControllerExit);
                    controllerWatcher.IsBackground = true;
                    controllerWatcher.Name = "ReaOwnedJob.ControllerWatch";
                    controllerWatcher.Start();
                    openedController = null;
                }
                catch
                {
                    controllerWatcher = null;
                    if (controllerWatchStop != null) { controllerWatchStop.Dispose(); controllerWatchStop = null; }
                    controller = null;
                    throw;
                }
                finally { if (openedController != null) openedController.Dispose(); }
            }
        }

        private void WatchControllerExit()
        {
            // Dispose signals and joins this thread before closing any of these
            // handles. This wait does not depend on PowerShell/CIM making progress.
            try
            {
                IntPtr[] handles = new IntPtr[] {
                    controller.DangerousGetHandle(), controllerWatchStop.SafeWaitHandle.DangerousGetHandle()
                };
                uint result = Native.WaitForMultipleObjects(2, handles, false, UInt32.MaxValue);
                if (result == WaitObject0 + 1) return;
                if (result == WaitObject0) controllerExitObserved = true;
                else if (result == UInt32.MaxValue)
                    controllerTerminationError = LastError("WaitForMultipleObjects(controller)").ToString();
                else controllerTerminationError = "Unexpected controller wait result: " + result;
                // A wait failure also fails closed, affecting only our own Job.
                if (!Native.TerminateJobObject(job, 1))
                    AppendControllerError(LastError("TerminateJobObject(controller exit)").ToString());
            }
            catch (Exception error)
            {
                AppendControllerError(error.ToString());
                try
                {
                    if (!Native.TerminateJobObject(job, 1))
                        AppendControllerError(LastError("TerminateJobObject(watch failure)").ToString());
                }
                catch (Exception terminationError) { AppendControllerError(terminationError.ToString()); }
            }
        }

        private void AppendControllerError(string error)
        {
            controllerTerminationError = controllerTerminationError == null ? error : controllerTerminationError + "; " + error;
        }

        public void Dispose()
        {
            lock (lifecycle)
            {
                if (controllerWatcher != null)
                {
                    controllerWatchStop.Set();
                    controllerWatcher.Join();
                    controllerWatcher = null;
                }
                if (controllerWatchStop != null) { controllerWatchStop.Dispose(); controllerWatchStop = null; }
                if (controller != null) { controller.Dispose(); controller = null; }
                // No PID termination or parent-tree enumeration.
                if (job != null) { job.Dispose(); job = null; }
                if (root != null) { root.Dispose(); root = null; }
            }
        }

        public static string[] SplitCommandLine(string commandLine)
        {
            // An empty string has special "current executable" semantics in the
            // Win32 function; it is not an acceptable identity-check input.
            if (String.IsNullOrWhiteSpace(commandLine) || commandLine.IndexOf('\0') >= 0)
                throw new ArgumentException("A complete, nonempty command line is required.", "commandLine");
            int count;
            IntPtr argv = Native.CommandLineToArgvW(commandLine, out count);
            if (argv == IntPtr.Zero) throw LastError("CommandLineToArgvW");
            try
            {
                if (count < 1) throw new InvalidOperationException("Command line returned no arguments.");
                string[] result = new string[count];
                for (int i = 0; i < count; ++i)
                    result[i] = Marshal.PtrToStringUni(Marshal.ReadIntPtr(argv, checked(i * IntPtr.Size)));
                return result;
            }
            finally
            {
                if (Native.LocalFree(argv) != IntPtr.Zero) throw LastError("LocalFree");
            }
        }

        private int[] ReadMemberIds(out uint assigned)
        {
            int capacity = 64;
            for (int attempt = 0; attempt < 12; ++attempt)
            {
                int size = checked(8 + capacity * IntPtr.Size);
                IntPtr buffer = Marshal.AllocHGlobal(size);
                try
                {
                    Marshal.WriteInt32(buffer, 0, 0);
                    Marshal.WriteInt32(buffer, 4, 0);
                    uint returned;
                    bool success = Native.QueryProcessIds(job, 3, buffer, (uint)size, out returned);
                    int error = success ? 0 : Marshal.GetLastWin32Error();
                    if (!success && error != ErrorMoreData) throw new Win32Exception(error, "QueryInformationJobObject(process IDs)");
                    uint required = unchecked((uint)Marshal.ReadInt32(buffer, 0));
                    uint count = unchecked((uint)Marshal.ReadInt32(buffer, 4));
                    if (required > MaximumMembers || count > MaximumMembers)
                        throw new InvalidOperationException("Job membership exceeds the bounded sampler inventory.");
                    if (!success || count < required)
                    {
                        if (capacity == MaximumMembers) throw new InvalidOperationException("Job membership list remained incomplete.");
                        capacity = Math.Min(MaximumMembers, Math.Max(capacity * 2, (int)required));
                        continue;
                    }
                    if (count > capacity) throw new InvalidOperationException("Invalid native Job process list length.");
                    int[] result = new int[count];
                    HashSet<int> seen = new HashSet<int>();
                    for (int i = 0; i < result.Length; ++i)
                    {
                        long nativePid = Marshal.ReadIntPtr(buffer, checked(8 + i * IntPtr.Size)).ToInt64();
                        if (nativePid <= 0 || nativePid > Int32.MaxValue || !seen.Add((int)nativePid))
                            throw new InvalidOperationException("Invalid or duplicate native Job process ID.");
                        result[i] = (int)nativePid;
                    }
                    assigned = required;
                    return result;
                }
                finally { Marshal.FreeHGlobal(buffer); }
            }
            throw new InvalidOperationException("Job membership changed too quickly to obtain a complete bounded list.");
        }

        private void CheckOpen()
        {
            if (job == null || job.IsClosed || job.IsInvalid) throw new ObjectDisposedException("ReaOwnedJob");
        }

        private static NativeHandle Open(int processId, uint rights)
        {
            if (processId <= 0) throw new ArgumentOutOfRangeException("processId");
            NativeHandle process = Native.OpenProcess(rights, false, processId);
            if (!process.IsInvalid) return process;
            Win32Exception error = LastError("OpenProcess");
            error.Data["OpenProcess"] = true;
            process.Dispose();
            throw error;
        }

        private static DateTime ReadCreation(NativeHandle process)
        {
            NativeFileTime creation, exit, kernel, user;
            if (!Native.GetProcessTimes(process, out creation, out exit, out kernel, out user))
                throw LastError("GetProcessTimes");
            long fileTime = unchecked((long)(((ulong)creation.High << 32) | creation.Low));
            return DateTime.FromFileTimeUtc(fileTime);
        }

        private static string ReadExecutable(NativeHandle process)
        {
            StringBuilder path = new StringBuilder(32768);
            uint length = (uint)path.Capacity;
            if (!Native.QueryFullProcessImageName(process, 0, path, ref length))
                throw LastError("QueryFullProcessImageName");
            if (length == 0) throw new InvalidOperationException("Process executable identity is empty.");
            return path.ToString();
        }

        private static bool ReadExited(NativeHandle process)
        {
            uint result = Native.WaitForSingleObject(process, 0);
            if (result == WaitObject0) return true;
            if (result == WaitTimeout) return false;
            if (result == UInt32.MaxValue) throw LastError("WaitForSingleObject");
            throw new InvalidOperationException("Unexpected process wait result.");
        }

        private static Win32Exception LastError(string operation)
        {
            return new Win32Exception(Marshal.GetLastWin32Error(), operation);
        }

        private static void ValidateLayout()
        {
            // This sampler is deliberately bound to the Windows x64 workspace.
            if (Environment.OSVersion.Platform != PlatformID.Win32NT || IntPtr.Size != 8)
                throw new PlatformNotSupportedException("Rea owned-job sampling requires Windows x64.");
            if (Marshal.SizeOf(typeof(BasicLimitInformation)) != 64 ||
                Marshal.SizeOf(typeof(ExtendedLimitInformation)) != 144 ||
                Marshal.SizeOf(typeof(BasicAccountingInformation)) != 48 ||
                Marshal.OffsetOf(typeof(BasicLimitInformation), "Affinity").ToInt32() != 48 ||
                Marshal.OffsetOf(typeof(ExtendedLimitInformation), "IoInfo").ToInt32() != 64)
                throw new InvalidOperationException("Windows x64 Job structure layout mismatch.");
        }

        private sealed class NativeHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            public NativeHandle() : base(true) { }
            protected override bool ReleaseHandle() { return Native.CloseHandle(handle); }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeFileTime { public uint Low, High; }
        [StructLayout(LayoutKind.Sequential)]
        private struct BasicLimitInformation
        {
            public long PerProcessUserTimeLimit, PerJobUserTimeLimit;
            public uint LimitFlags;
            public UIntPtr MinimumWorkingSetSize, MaximumWorkingSetSize;
            public uint ActiveProcessLimit;
            public UIntPtr Affinity;
            public uint PriorityClass, SchedulingClass;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct IoCounters
        {
            public ulong ReadOperationCount, WriteOperationCount, OtherOperationCount;
            public ulong ReadTransferCount, WriteTransferCount, OtherTransferCount;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct ExtendedLimitInformation
        {
            public BasicLimitInformation BasicLimitInformation;
            public IoCounters IoInfo;
            public UIntPtr ProcessMemoryLimit, JobMemoryLimit, PeakProcessMemoryUsed, PeakJobMemoryUsed;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct BasicAccountingInformation
        {
            public long TotalUserTime, TotalKernelTime, ThisPeriodTotalUserTime, ThisPeriodTotalKernelTime;
            public uint TotalPageFaultCount, TotalProcesses, ActiveProcesses, TotalTerminatedProcesses;
        }

        private static class Native
        {
            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            internal static extern NativeHandle CreateJobObject(IntPtr securityAttributes, string name);
            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool SetInformationJobObject(NativeHandle job, int infoClass, ref ExtendedLimitInformation info, uint length);
            [DllImport("kernel32.dll", EntryPoint = "QueryInformationJobObject", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool QueryExtendedLimits(NativeHandle job, int infoClass, out ExtendedLimitInformation info, uint length, out uint returned);
            [DllImport("kernel32.dll", EntryPoint = "QueryInformationJobObject", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool QueryProcessIds(NativeHandle job, int infoClass, IntPtr info, uint length, out uint returned);
            [DllImport("kernel32.dll", EntryPoint = "QueryInformationJobObject", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool QueryAccounting(NativeHandle job, int infoClass, out BasicAccountingInformation info, uint length, out uint returned);
            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool AssignProcessToJobObject(NativeHandle job, NativeHandle process);
            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool IsProcessInJob(NativeHandle process, NativeHandle job, [MarshalAs(UnmanagedType.Bool)] out bool result);
            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool TerminateJobObject(NativeHandle job, uint exitCode);
            [DllImport("kernel32.dll", SetLastError = true)]
            internal static extern NativeHandle OpenProcess(uint rights, [MarshalAs(UnmanagedType.Bool)] bool inherit, int processId);
            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool GetProcessTimes(NativeHandle process, out NativeFileTime creation, out NativeFileTime exit, out NativeFileTime kernel, out NativeFileTime user);
            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool QueryFullProcessImageName(NativeHandle process, uint flags, StringBuilder name, ref uint size);
            [DllImport("kernel32.dll", SetLastError = true)]
            internal static extern uint WaitForSingleObject(NativeHandle process, uint milliseconds);
            [DllImport("kernel32.dll", SetLastError = true)]
            internal static extern uint WaitForMultipleObjects(uint count, IntPtr[] handles, [MarshalAs(UnmanagedType.Bool)] bool waitAll, uint milliseconds);
            [DllImport("kernel32.dll")]
            internal static extern int GetCurrentProcessId();
            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool CloseHandle(IntPtr handle);
            [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            internal static extern IntPtr CommandLineToArgvW(string commandLine, out int argumentCount);
            [DllImport("kernel32.dll", SetLastError = true)]
            internal static extern IntPtr LocalFree(IntPtr memory);
        }
    }

    // Console.In.ReadLineAsync is synchronous on the Windows PowerShell host.
    // Keep the control pipe off its sampling thread. Only the fixed command from
    // this run's controller is accepted; this is not a general JSON command API.
    public sealed class ReaJobControl : IDisposable
    {
        private readonly ReaOwnedJob job;
        private readonly string expectedLine;
        private readonly object controlState = new object();
        private readonly Thread readerThread;
        private volatile bool closing;
        private volatile bool stopRequested;
        private volatile string failure;

        public bool StopRequested { get { return stopRequested; } }
        public string Failure { get { return failure; } }

        public ReaJobControl(ReaOwnedJob ownedJob, string runNonce)
        {
            if (ownedJob == null) throw new ArgumentNullException("ownedJob");
            Guid nonce;
            if (!Guid.TryParseExact(runNonce, "D", out nonce) ||
                !String.Equals(nonce.ToString("D"), runNonce, StringComparison.Ordinal))
                throw new ArgumentException("A canonical lowercase run UUID is required.", "runNonce");
            job = ownedJob;
            expectedLine = "{\"command\":\"stop-owned-job\",\"nonce\":\"" + runNonce + "\"}";
            readerThread = new Thread(ReadControl);
            readerThread.IsBackground = true;
            readerThread.Name = "ReaOwnedJob.ControlPipe";
            readerThread.Start();
        }

        private void ReadControl()
        {
            try
            {
                // Do not close global stdin or synchronously join a blocked
                // console read. The dedicated reader dies with the sampler.
                StreamReader reader = new StreamReader(Console.OpenStandardInput(),
                    new UTF8Encoding(false, true), false, 256);
                StringBuilder line = new StringBuilder();
                while (!closing)
                {
                    int value = reader.Read();
                    if (value < 0) { Fail("Controller control pipe closed."); return; }
                    if (value == '\n')
                    {
                        if (line.Length > 0 && line[line.Length - 1] == '\r') line.Length--;
                        if (!String.Equals(line.ToString(), expectedLine, StringComparison.Ordinal))
                        { Fail("Invalid control command or run identity."); return; }
                        line.Length = 0;
                        lock (controlState)
                        {
                            if (closing) return;
                            stopRequested = true;
                            try { job.Terminate(92); }
                            catch (Exception error)
                            { failure = "Owned Job stop failed: " + error.Message; return; }
                        }
                    }
                    else
                    {
                        if (line.Length >= expectedLine.Length + 1)
                        { Fail("Control command exceeds the fixed message length."); return; }
                        line.Append((char)value);
                    }
                }
            }
            catch (Exception error) { Fail("Control pipe read failed: " + error.Message); }
        }

        private void Fail(string reason)
        {
            lock (controlState)
            {
                if (closing) return;
                failure = reason;
                try { job.Terminate(93); }
                catch (Exception error) { failure += " Owned Job cleanup failed: " + error.Message; }
            }
        }

        public void Dispose()
        {
            // Call this before ReaOwnedJob.Dispose. Serialize with an active
            // dispatch, but never close stdin or wait for its pending read.
            lock (controlState) { closing = true; }
        }
    }
}
