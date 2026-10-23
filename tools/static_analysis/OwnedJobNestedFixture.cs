using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace GrimDynasty.StaticAnalysis
{
    // Test fixture only: no vendor code and no arbitrary executable/argument API.
    // The worker calling this constructor must already have passed its release gate.
    public sealed class OwnedJobNestedFixture : IDisposable
    {
        private const string WorkspacePath = @"C:\Users\zhuzi\Documents\Grim-Dynasty";
        private const string FixedNodePath = @"D:\NodeJS\node.exe";
        private const string FixedFixturePath =
            WorkspacePath + @"\tools\static_analysis\owned_job_fixture.mjs";
        private const uint KillOnJobClose = 0x2000;
        private const uint CreateSuspended = 0x00000004;
        private const uint CreateUnicodeEnvironment = 0x00000400;
        private const uint ExtendedStartupInfoPresent = 0x00080000;
        private const uint CreateNoWindow = 0x08000000;
        private const uint WaitObject0 = 0;
        private const uint WaitTimeout = 258;
        private const uint WaitFailed = 0xffffffff;
        private const int ErrorInsufficientBuffer = 122;
        private static readonly IntPtr JobListAttribute = new IntPtr(0x0002000d);
        private NativeHandle job;
        private NativeHandle child;

        public int ChildPid { get; private set; }
        public DateTime CreationUtc { get; private set; }
        // Preserve FILETIME precision when the JSON consumer is JavaScript.
        public string CreationFileTime { get; private set; }
        public bool InnerMembership { get; private set; }

        public OwnedJobNestedFixture(string nodePath, string fixturePath, string nonce)
        {
            ValidateLayout();
            ValidateFixedPath(nodePath, FixedNodePath, "nodePath");
            ValidateFixedPath(fixturePath, FixedFixturePath, "fixturePath");
            ValidateNonce(nonce);
            if (!File.Exists(FixedNodePath) || !File.Exists(FixedFixturePath))
                throw new FileNotFoundException("The fixed Node executable and fixture must already exist.");

            NativeHandle createdJob = null;
            NativeHandle createdChild = null;
            NativeHandle createdThread = null;
            IntPtr attributeList = IntPtr.Zero;
            IntPtr jobList = IntPtr.Zero;
            IntPtr environment = IntPtr.Zero;
            bool attributesInitialized = false;
            try
            {
                // NULL security attributes: the Job handle is not inheritable.
                createdJob = Native.CreateJobObjectW(IntPtr.Zero, null);
                if (createdJob.IsInvalid) throw LastError("CreateJobObjectW");
                ExtendedLimitInformation limits = new ExtendedLimitInformation();
                limits.BasicLimitInformation.LimitFlags = KillOnJobClose;
                uint limitsSize = checked((uint)Marshal.SizeOf(typeof(ExtendedLimitInformation)));
                if (!Native.SetInformationJobObject(createdJob, 9, ref limits, limitsSize))
                    throw LastError("SetInformationJobObject");
                ExtendedLimitInformation readback;
                uint returned;
                if (!Native.QueryInformationJobObject(createdJob, 9, out readback, limitsSize, out returned))
                    throw LastError("QueryInformationJobObject");
                if (readback.BasicLimitInformation.LimitFlags != KillOnJobClose)
                    throw new InvalidOperationException("Inner Job must have only KILL_ON_JOB_CLOSE.");

                UIntPtr attributeBytes = UIntPtr.Zero;
                bool sized = Native.InitializeProcThreadAttributeList(IntPtr.Zero, 1, 0, ref attributeBytes);
                int sizeError = Marshal.GetLastWin32Error();
                ulong bytes = attributeBytes.ToUInt64();
                if (sized || sizeError != ErrorInsufficientBuffer || bytes == 0 || bytes > 65536)
                    throw new InvalidOperationException("Unexpected process attribute-list sizing result.");
                attributeList = Marshal.AllocHGlobal(checked((int)bytes));
                if (!Native.InitializeProcThreadAttributeList(attributeList, 1, 0, ref attributeBytes))
                    throw LastError("InitializeProcThreadAttributeList");
                attributesInitialized = true;
                jobList = Marshal.AllocHGlobal(IntPtr.Size);
                Marshal.WriteIntPtr(jobList, createdJob.DangerousGetHandle());
                if (!Native.UpdateProcThreadAttribute(attributeList, 0, JobListAttribute,
                    jobList, new UIntPtr((uint)IntPtr.Size), IntPtr.Zero, IntPtr.Zero))
                    throw LastError("UpdateProcThreadAttribute(JOB_LIST)");

                StartupInfoEx startup = new StartupInfoEx();
                startup.StartupInfo.cb = checked((uint)Marshal.SizeOf(typeof(StartupInfoEx)));
                startup.AttributeList = attributeList;
                // Do not inherit NODE_OPTIONS, NODE_PATH, or a caller's preload settings.
                environment = CreateFixtureEnvironment();
                StringBuilder commandLine = new StringBuilder(
                    "\"" + FixedNodePath + "\" \"" + FixedFixturePath + "\" leaf " + nonce);
                ProcessInformation process;
                if (!Native.CreateProcessW(FixedNodePath, commandLine, IntPtr.Zero, IntPtr.Zero,
                    false, CreateSuspended | CreateUnicodeEnvironment |
                        ExtendedStartupInfoPresent | CreateNoWindow,
                    environment, WorkspacePath, ref startup, out process))
                    throw LastError("CreateProcessW");
                // These handles come from this creation, never a later PID lookup.
                createdChild = new NativeHandle(process.Process);
                createdThread = new NativeHandle(process.Thread);
                if (createdChild.IsInvalid || createdThread.IsInvalid || process.ProcessId == 0)
                    throw new InvalidOperationException("CreateProcessW returned an invalid process identity.");
                bool belongs;
                if (!Native.IsProcessInJob(createdChild, createdJob, out belongs))
                    throw LastError("IsProcessInJob(inner)");
                if (!belongs)
                    throw new InvalidOperationException("Suspended child is not a member of the exact inner Job.");
                FileTime creation, exit, kernel, user;
                if (!Native.GetProcessTimes(createdChild, out creation, out exit, out kernel, out user))
                    throw LastError("GetProcessTimes");
                long creationValue = checked((long)(((ulong)creation.High << 32) | creation.Low));
                DateTime createdAt = DateTime.FromFileTimeUtc(creationValue);
                uint initialWait = Native.WaitForSingleObject(createdChild, 0);
                if (initialWait == WaitFailed) throw LastError("WaitForSingleObject(before resume)");
                if (initialWait != WaitTimeout)
                    throw new InvalidOperationException("Suspended fixture child exited before resume.");
                uint previousSuspendCount = Native.ResumeThread(createdThread);
                if (previousSuspendCount == WaitFailed) throw LastError("ResumeThread");
                if (previousSuspendCount != 1)
                    throw new InvalidOperationException("The fixture child had an unexpected suspend count.");

                ChildPid = checked((int)process.ProcessId);
                CreationUtc = createdAt;
                CreationFileTime = creationValue.ToString(CultureInfo.InvariantCulture);
                InnerMembership = true;
                job = createdJob;
                child = createdChild;
                createdJob = null;
                createdChild = null;
            }
            finally
            {
                // JOB_LIST attached the child at creation. Any subsequent failure
                // closes this Job and kills only its members, even before resume.
                if (createdJob != null) createdJob.Dispose();
                if (createdThread != null) createdThread.Dispose();
                if (createdChild != null) createdChild.Dispose();
                if (attributesInitialized) Native.DeleteProcThreadAttributeList(attributeList);
                if (attributeList != IntPtr.Zero) Marshal.FreeHGlobal(attributeList);
                if (jobList != IntPtr.Zero) Marshal.FreeHGlobal(jobList);
                if (environment != IntPtr.Zero) Marshal.FreeHGlobal(environment);
            }
        }

        public void Dispose()
        {
            if (job != null) { job.Dispose(); job = null; }
            if (child != null)
            {
                NativeHandle exiting = child;
                child = null;
                try
                {
                    uint wait = Native.WaitForSingleObject(exiting, 5000);
                    if (wait == WaitFailed) throw LastError("WaitForSingleObject(dispose)");
                    if (wait != WaitObject0)
                        throw new TimeoutException("Inner Job was closed but the leaf did not exit within five seconds.");
                }
                finally { exiting.Dispose(); }
            }
        }

        private static void ValidateFixedPath(string actual, string expected, string argument)
        {
            if (String.IsNullOrWhiteSpace(actual) || actual.IndexOf('\0') >= 0 ||
                !Path.IsPathRooted(actual) ||
                !String.Equals(Path.GetFullPath(actual), expected, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Only the fixed local fixture path is allowed.", argument);
        }

        private static void ValidateNonce(string nonce)
        {
            if (String.IsNullOrEmpty(nonce) || nonce.Length != 32)
                throw new ArgumentException("Nonce must contain exactly 32 lowercase hexadecimal characters.", "nonce");
            foreach (char value in nonce)
                if (!((value >= 'a' && value <= 'f') || (value >= '0' && value <= '9')))
                    throw new ArgumentException("Nonce contains an unsupported character.", "nonce");
        }

        private static IntPtr CreateFixtureEnvironment()
        {
            SortedDictionary<string, string> entries =
                new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string name in new string[] { "SystemRoot", "WINDIR", "TEMP", "TMP" })
            {
                string value = Environment.GetEnvironmentVariable(name);
                if (!String.IsNullOrEmpty(value))
                {
                    if (value.IndexOf('\0') >= 0) throw new InvalidOperationException("Invalid environment value.");
                    entries.Add(name, value);
                }
            }
            if (!entries.ContainsKey("SystemRoot"))
                throw new InvalidOperationException("SystemRoot is required for the fixed Windows fixture.");
            StringBuilder block = new StringBuilder();
            foreach (KeyValuePair<string, string> entry in entries)
                block.Append(entry.Key).Append('=').Append(entry.Value).Append('\0');
            block.Append('\0');
            return Marshal.StringToHGlobalUni(block.ToString());
        }

        private static void ValidateLayout()
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT || IntPtr.Size != 8 ||
                Marshal.SizeOf(typeof(StartupInfo)) != 104 ||
                Marshal.SizeOf(typeof(StartupInfoEx)) != 112 ||
                Marshal.SizeOf(typeof(ProcessInformation)) != 24 ||
                Marshal.SizeOf(typeof(ExtendedLimitInformation)) != 144)
                throw new PlatformNotSupportedException("This fixture requires the declared Windows x64 layouts.");
        }

        private static Win32Exception LastError(string operation)
        {
            return new Win32Exception(Marshal.GetLastWin32Error(), operation + " failed.");
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct FileTime { public uint Low; public uint High; }

        [StructLayout(LayoutKind.Sequential)]
        private struct BasicLimitInformation
        {
            public long PerProcessUserTimeLimit;
            public long PerJobUserTimeLimit;
            public uint LimitFlags;
            public UIntPtr MinimumWorkingSetSize;
            public UIntPtr MaximumWorkingSetSize;
            public uint ActiveProcessLimit;
            public UIntPtr Affinity;
            public uint PriorityClass;
            public uint SchedulingClass;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IoCounters
        {
            public ulong ReadOperationCount;
            public ulong WriteOperationCount;
            public ulong OtherOperationCount;
            public ulong ReadTransferCount;
            public ulong WriteTransferCount;
            public ulong OtherTransferCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ExtendedLimitInformation
        {
            public BasicLimitInformation BasicLimitInformation;
            public IoCounters IoInfo;
            public UIntPtr ProcessMemoryLimit;
            public UIntPtr JobMemoryLimit;
            public UIntPtr PeakProcessMemoryUsed;
            public UIntPtr PeakJobMemoryUsed;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct StartupInfo
        {
            public uint cb;
            public IntPtr Reserved;
            public IntPtr Desktop;
            public IntPtr Title;
            public uint X;
            public uint Y;
            public uint XSize;
            public uint YSize;
            public uint XCountChars;
            public uint YCountChars;
            public uint FillAttribute;
            public uint Flags;
            public ushort ShowWindow;
            public ushort Reserved2Bytes;
            public IntPtr Reserved2;
            public IntPtr StandardInput;
            public IntPtr StandardOutput;
            public IntPtr StandardError;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct StartupInfoEx
        {
            public StartupInfo StartupInfo;
            public IntPtr AttributeList;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ProcessInformation
        {
            public IntPtr Process;
            public IntPtr Thread;
            public uint ProcessId;
            public uint ThreadId;
        }

        private sealed class NativeHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            public NativeHandle() : base(true) { }
            public NativeHandle(IntPtr value) : base(true) { SetHandle(value); }
            protected override bool ReleaseHandle() { return Native.CloseHandle(handle); }
        }

        private static class Native
        {
            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
            public static extern NativeHandle CreateJobObjectW(IntPtr attributes, string name);
            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool SetInformationJobObject(NativeHandle job, int informationClass,
                ref ExtendedLimitInformation information, uint length);
            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool QueryInformationJobObject(NativeHandle job, int informationClass,
                out ExtendedLimitInformation information, uint length, out uint returned);
            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool InitializeProcThreadAttributeList(IntPtr attributes,
                uint count, uint flags, ref UIntPtr size);
            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool UpdateProcThreadAttribute(IntPtr attributes, uint flags,
                IntPtr attribute, IntPtr value, UIntPtr size, IntPtr previousValue, IntPtr returnedSize);
            [DllImport("kernel32.dll")]
            public static extern void DeleteProcThreadAttributeList(IntPtr attributes);
            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool CreateProcessW(string applicationName, StringBuilder commandLine,
                IntPtr processAttributes, IntPtr threadAttributes,
                [MarshalAs(UnmanagedType.Bool)] bool inheritHandles, uint flags, IntPtr environment,
                string currentDirectory, ref StartupInfoEx startupInfo, out ProcessInformation process);
            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool IsProcessInJob(NativeHandle process, NativeHandle job,
                [MarshalAs(UnmanagedType.Bool)] out bool result);
            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool GetProcessTimes(NativeHandle process, out FileTime creation,
                out FileTime exit, out FileTime kernel, out FileTime user);
            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern uint ResumeThread(NativeHandle thread);
            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern uint WaitForSingleObject(NativeHandle handle, uint milliseconds);
            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool CloseHandle(IntPtr handle);
        }
    }
}
