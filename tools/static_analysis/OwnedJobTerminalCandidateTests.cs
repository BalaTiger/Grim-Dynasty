using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using GrimDynasty.StaticAnalysis.TerminalCandidate;

// Test-only instrumentation is inserted into a derived source in the fresh test
// output directory. Neither this class nor its switches occur in the candidate
// source or the production monitor. Injected failures are NOT an R0 reproduction.
namespace GrimDynasty.StaticAnalysis.TerminalCandidate
{
    internal static class TerminalFault
    {
        internal static int ImageError;
        internal static bool FailTimedWait;
        internal static bool EndFirstTimedProcess;
        internal static int ImageCalls, TimedCalls;
        internal static uint RequestedMilliseconds;
        internal static int FirstEndedPid;
        internal static IntPtr ImageHandle, TimedHandle;
        internal static readonly Dictionary<int, Process> Owned = new Dictionary<int, Process>();

        internal static void Reset(int imageError)
        {
            ImageError = imageError;
            FailTimedWait = false;
            EndFirstTimedProcess = false;
            ImageCalls = TimedCalls = FirstEndedPid = 0;
            RequestedMilliseconds = 0;
            ImageHandle = TimedHandle = IntPtr.Zero;
        }

        internal static string Image(IntPtr process, Func<string> actual)
        {
            ImageCalls++;
            ImageHandle = process;
            if (ImageError != 0) throw new Win32Exception(ImageError, "QueryFullProcessImageName");
            return actual();
        }

        internal static uint Wait(IntPtr process, uint milliseconds, Func<uint> actual)
        {
            if (milliseconds == 0) return actual();
            TimedCalls++;
            RequestedMilliseconds = checked(RequestedMilliseconds + milliseconds);
            TimedHandle = process;
            if (FailTimedWait) throw new Win32Exception(6, "WaitForSingleObject");
            if (EndFirstTimedProcess && FirstEndedPid == 0)
            {
                int pid = checked((int)GetProcessId(process));
                Process owned;
                if (!Owned.TryGetValue(pid, out owned)) throw new InvalidOperationException("Unknown test process; refusing termination.");
                // Deterministic transition injection: initial zero-wait was real
                // and unsignaled. Terminate only an exact retained test process,
                // then let the candidate's real native 100 ms wait confirm exit.
                // This setup delay is NOT measured as candidate grace latency.
                owned.Kill();
                if (!owned.WaitForExit(2000)) throw new InvalidOperationException("Owned transition did not exit.");
                FirstEndedPid = pid;
            }
            return actual();
        }

        [DllImport("kernel32.dll", SetLastError=true)]
        private static extern uint GetProcessId(IntPtr process);
    }
}

internal static class OwnedJobTerminalCandidateTests
{
    private const string Workspace = @"C:\Users\zhuzi\Documents\Grim-Dynasty";
    private const string Output = Workspace + @"\.tooling\r0-owned-job-terminal-candidate-20261022-attempt2";
    private const string Node = @"D:\NodeJS\node.exe";
    // Node v24.14.0 libuv otherwise adds the child to its own kill-on-parent-exit
    // Job. Detached skips that nested Job; it does NOT request Job breakaway.
    // Our exact Job membership/creation proof below remains mandatory.
    private const string NodeCode = "process.stdin.resume();process.stdout.write('ready\\n');require('readline').createInterface({input:process.stdin}).on('line',s=>{if(s==='spawn'){const c=require('child_process').spawn(process.execPath,['-e','setInterval(()=>{},1000);setTimeout(()=>process.exit(93),12000)'],{stdio:'ignore',windowsHide:true,detached:true});c.once('spawn',()=>process.stdout.write('child:'+c.pid+'\\n'));}else if(s==='exit')process.exit(0);else process.exit(94);});process.stdin.on('end',()=>process.exit(0));setTimeout(()=>process.exit(95),12000);";
    private static readonly List<Process> Processes = new List<Process>();
    private static readonly List<ReaOwnedJob> Jobs = new List<ReaOwnedJob>();
    private static int assertions, groups, exited;

    private static void Check(bool value, string label)
    {
        if (!value) throw new InvalidOperationException("Assertion: " + label);
        assertions++;
        Console.WriteLine("PASS " + label);
    }

    private static void Normal(string path, bool directory)
    {
        string current = Path.GetFullPath(path);
        bool first = true;
        while (current != null)
        {
            FileAttributes attrs = File.GetAttributes(current);
            if ((attrs & FileAttributes.ReparsePoint) != 0) throw new InvalidOperationException("Redirected path refused.");
            if (((attrs & FileAttributes.Directory) != 0) != (!first || directory)) throw new InvalidOperationException("Unexpected path type.");
            DirectoryInfo parent = Directory.GetParent(current);
            current = parent == null ? null : parent.FullName;
            first = false;
        }
    }

    private static Process Start()
    {
        if (Processes.Count >= 4) throw new InvalidOperationException("Own child budget exceeded.");
        Normal(Node, false);
        ProcessStartInfo info = new ProcessStartInfo(Node, "-e \"" + NodeCode + "\"");
        info.UseShellExecute = false;
        info.CreateNoWindow = true;
        info.WorkingDirectory = Output;
        info.RedirectStandardInput = true;
        info.RedirectStandardOutput = true;
        info.RedirectStandardError = true;
        info.EnvironmentVariables.Clear();
        info.EnvironmentVariables["SystemRoot"] = @"C:\Windows";
        info.EnvironmentVariables["WINDIR"] = @"C:\Windows";
        Process process = Process.Start(info);
        Processes.Add(process);
        IntPtr retained = process.Handle;
        TerminalFault.Owned.Add(process.Id, process);
        if (process.StandardOutput.ReadLine() != "ready") throw new InvalidOperationException("Own Node gate not ready.");
        return process;
    }

    private static ReaOwnedJob Assign(Process process)
    {
        ReaOwnedJob job = new ReaOwnedJob(process.Id, process.StartTime.ToUniversalTime(), Node);
        Jobs.Add(job);
        return job;
    }

    private static Win32Exception Failure(Action action, int expected, string label)
    {
        Win32Exception found = null;
        try { action(); } catch (Win32Exception error) { found = error; }
        Console.WriteLine("DIAGNOSTIC expected_error=" + expected + " actual_error=" + (found == null ? "null" : found.NativeErrorCode.ToString(System.Globalization.CultureInfo.InvariantCulture)) +
            " image_calls=" + TerminalFault.ImageCalls + " timed_calls=" + TerminalFault.TimedCalls + " requested_ms=" + TerminalFault.RequestedMilliseconds + " first_ended_pid=" + TerminalFault.FirstEndedPid);
        if (found != null)
        {
            foreach (string key in new string[] { "ReaProbeProcessId", "ReaProbeCreationUtc", "ReaProbeInJob", "ReaProbeStep", "ReaProbeImageErrorCode", "ReaProbeExitAfterImageFailure", "ReaProbeExitCheckErrorCode", "ReaProbeFromQueryMembers", "ReaProbeMemberIndex", "ReaProbeMemberCount" })
                if (found.Data.Contains(key)) Console.WriteLine("DIAGNOSTIC " + key + "=" + found.Data[key]);
        }
        Check(found != null && found.NativeErrorCode == expected, label);
        return found;
    }

    private static bool Data(Win32Exception error, string key, object expected)
    {
        return error.Data.Contains(key) && Object.Equals(error.Data[key], expected);
    }

    private static void Run()
    {
        Process owned = Start();
        ReaOwnedJob job = Assign(owned);
        TerminalFault.Reset(0);
        ReaProcessProbe normal = job.Probe(owned.Id);
        Check(normal.InJob && !normal.HasExited && String.Equals(normal.ExecutablePath, Node, StringComparison.OrdinalIgnoreCase), "normal live identity unchanged");
        Check(normal.CreationUtc == job.RootCreationUtc && normal.ExecutablePathErrorCode == null && normal.ExecutablePathDiagnostic == null, "normal metadata unchanged");
        ReaJobMembers members = job.QueryMembers();
        Check(members.Members.Length == 1 && members.Members[0].ProcessId == owned.Id && members.ActiveProcesses == 1, "normal Job enumeration unchanged");
        Check(TerminalFault.TimedCalls == 0, "normal path has no grace wait");
        groups++;

        TerminalFault.Reset(5);
        Win32Exception timeout = Failure(delegate { job.Probe(owned.Id); }, 5, "live timeout rethrows original image5");
        Check(Data(timeout, "ReaProbeImageErrorCode", 5) && Data(timeout, "ReaProbeExitAfterImageFailure", false) && Data(timeout, "ReaProbeInJob", true), "timeout preserves observed facts");
        Check(Data(timeout, "ReaProbeStep", "QueryFullProcessImageName") && Data(timeout, "ReaProbeProcessId", owned.Id) && Data(timeout, "ReaProbeCreationUtc", job.RootCreationUtc), "timeout retains identity and API stage");
        Check(TerminalFault.TimedCalls == 1 && TerminalFault.RequestedMilliseconds == 100 && TerminalFault.ImageCalls == 1 && TerminalFault.ImageHandle == TerminalFault.TimedHandle, "one image query and bounded same-handle wait");
        Check(!owned.HasExited, "timeout never terminates or accepts live process");
        groups++;

        foreach (int code in new int[] { 87, 123456 })
        {
            TerminalFault.Reset(code);
            Win32Exception other = Failure(delegate { job.Probe(owned.Id); }, code, "other image error remains fail closed " + code);
            Check(Data(other, "ReaProbeImageErrorCode", code) && Data(other, "ReaProbeExitAfterImageFailure", false) && !other.Data.Contains("OpenProcess"), "image error is not OpenProcess gone " + code);
            Check(TerminalFault.TimedCalls == 0 && !owned.HasExited, "no grace for other error " + code);
        }
        groups++;

        TerminalFault.Reset(5);
        TerminalFault.FailTimedWait = true;
        Win32Exception wait = Failure(delegate { job.Probe(owned.Id); }, 6, "timed wait failure propagates wait6");
        Check(Data(wait, "ReaProbeImageErrorCode", 5) && Data(wait, "ReaProbeExitCheckErrorCode", 6), "wait failure preserves both error codes");
        Check(Data(wait, "ReaProbeStep", "WaitForSingleObject(after image failure)") && Data(wait, "ReaProbeExitAfterImageFailure", false), "wait failure retains earlier zero-wait fact");
        Check(TerminalFault.TimedCalls == 1 && !owned.HasExited, "failed wait cannot accept or terminate process");
        groups++;

        Process outside = Start();
        TerminalFault.Reset(5);
        Win32Exception nonmember = Failure(delegate { job.Probe(outside.Id); }, 5, "nonmember image5 remains fail closed");
        Check(Data(nonmember, "ReaProbeInJob", false) && Data(nonmember, "ReaProbeExitAfterImageFailure", false), "nonmember observations retained");
        Check(TerminalFault.TimedCalls == 0 && !outside.HasExited, "nonmember never receives grace or termination");
        outside.StandardInput.WriteLine("exit"); outside.StandardInput.Flush();
        Check(outside.WaitForExit(2000) && Data(nonmember, "ReaProbeExitAfterImageFailure", false), "later external exit cannot rewrite rejected observation");
        groups++;

        TerminalFault.Reset(5);
        TerminalFault.EndFirstTimedProcess = true;
        ReaProcessProbe terminal = job.Probe(owned.Id);
        Check(terminal.HasExited && terminal.InJob && terminal.CreationUtc == job.RootCreationUtc, "genuine same-handle terminal proof accepted");
        Check(terminal.ExecutablePath == null && terminal.ExecutablePathErrorCode == 5 && !String.IsNullOrEmpty(terminal.ExecutablePathDiagnostic), "terminal observation has no fabricated image");
        Check(TerminalFault.ImageCalls == 1 && TerminalFault.TimedCalls == 1 && TerminalFault.RequestedMilliseconds == 100 && TerminalFault.ImageHandle == TerminalFault.TimedHandle, "terminal transition uses one native wait and no image retry");
        Check(owned.HasExited && TerminalFault.FirstEndedPid == owned.Id, "controlled positive fixture really exited");
        TerminalFault.Reset(5);
        ReaProcessProbe already = job.Probe(owned.Id);
        Check(already.HasExited && already.ExecutablePath == null && TerminalFault.TimedCalls == 0, "already signaled branch stays immediate");
        groups++;

        Process root = Start();
        ReaOwnedJob pair = Assign(root);
        root.StandardInput.WriteLine("spawn"); root.StandardInput.Flush();
        string childLine = root.StandardOutput.ReadLine();
        if (childLine == null || !childLine.StartsWith("child:", StringComparison.Ordinal)) throw new InvalidOperationException("Missing own descendant identity.");
        int childId = Int32.Parse(childLine.Substring(6), System.Globalization.CultureInfo.InvariantCulture);
        Process child = Process.GetProcessById(childId);
        bool childVerified = false;
        try
        {
            IntPtr childRetained = child.Handle;
            TerminalFault.Reset(0);
            ReaProcessProbe childProof = pair.Probe(childId);
            Check(childProof.InJob && !childProof.HasExited && childProof.CreationUtc == child.StartTime.ToUniversalTime(), "descendant independently verified before fault injection");
            // A failed proof must never put a reopened PID on the kill list.
            // The retained handle pins this verified creation identity thereafter.
            Processes.Add(child);
            TerminalFault.Owned.Add(childId, child);
            childVerified = true;
        }
        finally { if (!childVerified) child.Dispose(); }
        Check(pair.QueryMembers().Members.Length == 2, "aggregate fixture has two actual Job members");
        TerminalFault.Reset(5);
        TerminalFault.EndFirstTimedProcess = true;
        Win32Exception aggregate = Failure(delegate {
            ReaJobMembers returned = null;
            try { returned = pair.QueryMembers(); }
            finally
            {
                Console.WriteLine("AGGREGATE returned=" + (returned != null) + " first_ended_pid=" + TerminalFault.FirstEndedPid);
                LogRetainedState("root", root);
                LogRetainedState("child", child);
                if (returned != null)
                {
                    Console.WriteLine("AGGREGATE active=" + returned.ActiveProcesses + " members=" + returned.Members.Length + " gone=" + returned.GoneProcessIds.Length);
                    foreach (ReaProcessProbe member in returned.Members)
                        Console.WriteLine("AGGREGATE member_pid=" + member.ProcessId + " has_exited=" + member.HasExited);
                }
            }
        }, 5, "second unexited member fails when shared grace exhausted");
        Check(Data(aggregate, "ReaProbeFromQueryMembers", true) && Data(aggregate, "ReaProbeMemberIndex", 1) && Data(aggregate, "ReaProbeMemberCount", 2), "enumeration failure annotations retained");
        Check(TerminalFault.TimedCalls == 1 && TerminalFault.RequestedMilliseconds == 100 && TerminalFault.ImageCalls == 2, "whole enumeration grants at most one 100ms wait");
        int survivor = TerminalFault.FirstEndedPid == root.Id ? child.Id : root.Id;
        Check(!TerminalFault.Owned[survivor].HasExited && Data(aggregate, "ReaProbeProcessId", survivor) && Data(aggregate, "ReaProbeExitAfterImageFailure", false), "second live member is neither adopted nor terminated");
        TerminalFault.Reset(0);
        ReaJobMembers remaining = pair.QueryMembers();
        Check(remaining.ActiveProcesses == 1 && remaining.Members.Length == 1 && remaining.Members[0].ProcessId == survivor, "root/member exit never implies empty Job");
        groups++;
    }

    private static void LogRetainedState(string role, Process process)
    {
        // Diagnostic-only observation of an already retained fixture identity.
        // A diagnostic failure must not replace the original QueryMembers error.
        try { Console.WriteLine("AGGREGATE " + role + "_pid=" + process.Id + " has_exited=" + process.HasExited); }
        catch (Exception error) { Console.WriteLine("AGGREGATE " + role + "_state_error=" + error.GetType().FullName); }
    }

    public static int Main(string[] args)
    {
        Exception failure = null;
        bool outputValidated = false;
        try
        {
            if (args.Length != 0 || IntPtr.Size != 8 || Environment.OSVersion.Platform != PlatformID.Win32NT) throw new InvalidOperationException("Fixed Windows x64 no-argument fixture required.");
            if (!String.Equals(Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory).TrimEnd('\\'), Output, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Fixed output directory required.");
            Normal(Output, true); outputValidated = true;
            Run();
        }
        catch (Exception error) { failure = error; }
        finally
        {
            TerminalFault.Reset(0);
            foreach (ReaOwnedJob job in Jobs) { try { job.Dispose(); } catch (Exception error) { if (failure == null) failure = error; } }
            foreach (Process process in Processes)
            {
                try
                {
                    // Process.Handle was retained at creation/adoption. No PID
                    // lookup is performed for cleanup or monitoring recovery.
                    if (!process.HasExited) process.Kill();
                    if (!process.WaitForExit(2000)) throw new InvalidOperationException("Own process exit not confirmed.");
                    exited++;
                }
                catch (Exception error) { if (failure == null) failure = error; }
                finally { process.Dispose(); }
            }
        }
        if (failure == null)
        {
            Check(Processes.Count == 4 && exited == 4, "all four own process identities confirmed exited");
            groups++;
        }
        string status = failure == null ? "passed-isolated-contract-tests" : "failed";
        if (outputValidated)
        {
            Normal(Output, true);
            string json = "{\"status\":\"" + status + "\",\"assertions\":" + assertions + ",\"groups\":" + groups + ",\"own_processes\":" + Processes.Count + ",\"exits_confirmed\":" + exited + ",\"failure_type\":" + (failure == null ? "null" : "\"" + failure.GetType().FullName + "\"") + ",\"fault_injection\":true,\"positive_wait_is_native\":true,\"production_modified\":false,\"r0_reproduced\":false,\"root_cause_proven\":false}\n";
            using (FileStream stream = new FileStream(Path.Combine(Output, "test-result.json"), FileMode.CreateNew, FileAccess.Write, FileShare.Read))
            { byte[] bytes = new UTF8Encoding(false).GetBytes(json); stream.Write(bytes, 0, bytes.Length); }
        }
        if (failure != null) { Console.Error.WriteLine(failure.GetType().FullName + ": " + failure.Message); return 1; }
        Console.WriteLine("PASS: " + assertions + " assertions, " + groups + " scenario groups");
        return 0;
    }
}
