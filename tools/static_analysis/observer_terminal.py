"""Bounded observer cleanup over an already identity-verified native handle.

The caller owns attachment, strict stop inspection and its own breakpoint.
This module never infers an exception cause from an exit code.
"""
import time


class TerminalUnknown(RuntimeError):
    def __init__(self, message, evidence):
        super().__init__(message)
        self.evidence = evidence


def finish_observation(process, native, expected_pid, expected_creation, lldb,
                       inspect_stop, remove_breakpoint, record, foreign_stop=False,
                       timeout=2.0, stop_timeout=15.0,
                       clock=time.monotonic, sleep=time.sleep):
    """Return safe detached/exited evidence or raise TerminalUnknown.

    native must retain the handle originally verified for expected_pid. Its
    snapshot() returns creation_filetime, wait (0/258), and uint32 exit_code.
    Callbacks must raise on failed inspection/removal; an unknown stop is never
    resumed. The original capture error remains the caller's separate evidence.
    """
    evidence = {"outcome": None, "safe_to_close_debugger": False,
                "native_terminal": False, "detach": None, "stop": None,
                "terminal": None, "native_last": None,
                "foreign_stop": bool(foreign_stop)}

    def unknown(message):
        evidence["safe_to_close_debugger"] = False
        raise TerminalUnknown(message, evidence)

    def sample(phase):
        value = dict(native.snapshot())
        evidence["native_last"] = value
        if (value.get("creation_filetime") != expected_creation or
                type(value.get("wait")) is not int or value["wait"] not in (0, 258) or
                type(value.get("exit_code")) is not int or
                not 0 <= value["exit_code"] <= 0xffffffff):
            unknown("Native identity or terminal sample differs")
        record("terminal-native", phase=phase, native=value)
        return value

    def state():
        if process is None or not process.IsValid() or process.GetProcessID() != expected_pid:
            unknown("Debugger process identity unavailable or changed")
        value = process.GetState()
        evidence["lldb_state"] = value
        return value

    def finish(outcome, phase, value):
        evidence.update(outcome=outcome, safe_to_close_debugger=True,
                        native_terminal=value["wait"] == 0, phase=phase)
        record("terminal-resolved", outcome=outcome, phase=phase, native=value,
               detach=evidence["detach"], stop=evidence["stop"])
        return evidence

    def wait_exit(phase, first=None):
        started = clock()
        value = sample(phase) if first is None else first
        observation = {"phase": phase, "first": value, "final": value, "elapsed_seconds": 0.0}
        evidence["terminal"] = observation
        while value["wait"] != 0:
            elapsed = clock() - started
            if elapsed >= timeout:
                observation["elapsed_seconds"] = elapsed
                unknown("LLDB exit observed but same-handle terminal state remains unknown")
            sleep(min(0.05, timeout - elapsed))
            value = sample(phase)
            observation["final"] = value
        observation["elapsed_seconds"] = clock() - started
        return finish("exited", phase, value)

    def classify(phase):
        value = sample(phase)
        if value["wait"] == 0:
            return finish("exited", phase, value), None
        current = state()
        if current == lldb.eStateExited:
            return wait_exit(phase, value), None
        if current == lldb.eStateDetached:
            return finish("detached", phase, value), None
        return None, current

    try:
        if (type(expected_pid) is not int or expected_pid <= 0 or
                not isinstance(expected_creation, str) or not expected_creation or
                not 0 < timeout <= 2.0 or not 0 < stop_timeout <= 15.0):
            unknown("Invalid fixed identity or bounded wait parameters")
        result, current = classify("entry")
        if result is not None:
            return result
        if foreign_stop:
            unknown("Foreign or incompletely inspected live stop retained")

        stop_succeeded = False
        if current in (lldb.eStateRunning, lldb.eStateStepping):
            error = process.Stop()
            evidence["stop"] = {"success": error.Success(), "error": error.GetCString()}
            record("terminal-stop-result", **evidence["stop"])
            stop_succeeded = error.Success()
            result, current = classify("after-stop")
            if result is not None:
                return result
            if not stop_succeeded:
                unknown("Stop failed without proven native termination")
            deadline = clock() + stop_timeout
            while current in (lldb.eStateRunning, lldb.eStateStepping):
                remaining = deadline - clock()
                if remaining <= 0:
                    unknown("Stop transition deadline exceeded")
                sleep(min(0.05, remaining))
                result, current = classify("waiting-after-stop")
                if result is not None:
                    return result
        if current != lldb.eStateStopped:
            unknown("No inspected stop is available for detach")

        inspected_stop_id = process.GetStopID()
        if type(inspected_stop_id) is not int or inspected_stop_id < 0:
            unknown("Stop identity unavailable before inspection")
        evidence["inspected_stop_id"] = inspected_stop_id
        inspect_stop("pre-detach", allow_interrupt=stop_succeeded)
        result, current = classify("after-stop-inspection")
        if result is not None:
            return result
        if current != lldb.eStateStopped:
            unknown("Target state changed after stop inspection")
        evidence["observed_stop_id"] = process.GetStopID()
        if evidence["observed_stop_id"] != inspected_stop_id:
            unknown("Stop identity changed during inspection; no breakpoint removal or detach")
        evidence["breakpoint_removal_attempted"] = True
        remove_breakpoint()
        evidence["breakpoint_removed"] = True
        result, current = classify("after-breakpoint-removal")
        if result is not None:
            return result
        if current != lldb.eStateStopped:
            unknown("Target state changed before detach")
        evidence["observed_stop_id"] = process.GetStopID()
        if evidence["observed_stop_id"] != inspected_stop_id:
            unknown("Stop identity changed after breakpoint removal; no detach")

        error = process.Detach()
        evidence["detach"] = {"success": error.Success(), "error": error.GetCString()}
        record("terminal-detach-result", **evidence["detach"])
        # A successful API return is not a detached-state or native-exit proof.
        started = clock()
        while True:
            value = sample("after-detach")
            if value["wait"] == 0:
                return finish("exited", "after-detach", value)
            current = state()
            evidence["detach"]["state"] = current
            if current == lldb.eStateExited:
                return wait_exit("after-detach-exited", value)
            if not error.Success():
                unknown("Detach failed without proven native termination")
            if current == lldb.eStateDetached:
                return finish("detached", "after-detach", value)
            elapsed = clock() - started
            if elapsed >= timeout:
                unknown("Detach returned but detached/native-terminal state remains unknown")
            sleep(min(0.05, timeout - elapsed))
    except TerminalUnknown:
        raise
    except Exception as error:
        evidence["safe_to_close_debugger"] = False
        evidence["check_error"] = {"type": type(error).__name__, "message": str(error)}
        raise TerminalUnknown("Terminal verification failed: " + str(error), evidence) from error
