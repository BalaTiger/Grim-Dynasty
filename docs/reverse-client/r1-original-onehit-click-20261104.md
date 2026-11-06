# Original click observation, 2026-11-04

The renewed original-game observation did **not capture the input entry**. After one ground click, the original process exited with `0x80000003`. This is an observed exit code, not a proven root cause. Do not repeat the same original-game software-breakpoint attempt unchanged.

## Actual sequence

- Backup receipt `r1-click-resume-preflight-20261104T1612Z.json`: both backup copies 19/19 files match, three original image hashes match. No original save contents were read or restored.
- Steam UI was visibly offline; Grim Dawn's per-game Cloud switch was off. No settings changed. Normal Steam launch loaded the already selected level-75 character and existing town. UI receipt: `r1-original-relaunch-ui-20261104.json`.
- Fresh original identity: PID `52980`, creation FILETIME `134382828029856734`, exact x64 original image. The original remained outside the ordinary tools Job (flags `0`).
- Frozen plan `.tooling/gd-click-onehit-20261104/manifest.json` SHA256 `1adea3e825023c5635fbef849e8e4ae2cb7e4a670e97e06d6ec02af7508e960d`; 17/17 pins matched. Formal owner session `72623` started once.
- 16:28:59.509940 UTC: armed asynchronous observation; state Running, StopID 1, breakpoint hit count 0. Game entry address `0x7ffac965e1f0`, RVA `0x15e1f0`.
- CUA issued one left click on visible empty town ground at window-relative pixel `(1516,864)` in HWND `10617210`, 2560x1440 image. Call began `16:29:22.767Z`, returned `16:29:22.867Z`. The subsequent AX read reported that the window was unavailable.
- 16:29:22.989561 UTC: SDK state Exited; same native handle still unsignaled, exit code already `0x80000003`. At 16:29:23.343354 UTC the **same handle** signaled exit. No owned breakpoint stop or capture was reported.
- Initial attach Continue: 1. Subsequent Stop/Continue/Detach/Kill: all 0. No breakpoint-deletion attempt. Original disk image hashes still match.
- Owner finished at `16:29:23.4627908Z`, tool exit 0, tool Job empty/disposed, no forced termination. This is successful accounting of an unsuccessful capture, not successful game tracing.

## Evidence and limitations

Raw current-run evidence is under `.tooling/gd-click-onehit-20261104/`: `trace-result.json`, `trace-events.ndjson`, `lldb.stdout.log`, `worker-result.json`, `owner-result.json`, `owner-samples.ndjson`, and the frozen source/config/manifest. The old seventh attempt remains unchanged under `.tooling/gd-click-observation-20261104/`.

The current run establishes temporal proximity between the click and exit, without proving the exception address, first/second-chance disposition, or why LLDB did not deliver an owned breakpoint stop. No observed owned stop is not proof that the breakpoint instruction never executed. The revised exit-first cleanup successfully avoided the earlier indefinite stopped-state assumption. Neither an input parameter nor a UI caller was recovered, and no behavior-equivalent movement implementation is newly validated by this run.

The existing independent-release receipt finished at `16:32:50.9695513Z`: all four recorded PIDs were absent, no current LLDB or original-game process was listed, and both backup copies still matched 19/19 file hashes. These backup checks do not establish the condition of the current original saves or remote Steam Cloud. No restore or overwrite occurred. Structured results and evidence hashes are in [r1-original-onehit-click-20261104.json](r1-original-onehit-click-20261104.json).

Next finite discriminator: prepare two bounded cases using a harmless self-authored DLL and host to test the same installed LLDB's address breakpoint in an already loaded DLL, with main-thread versus worker-thread calls as the proposed comparison. Prior positive fixture evidence used executable functions. The original remains closed, not paused. No original relaunch is scheduled; do not infer a DRM mechanism, install another debugger, or repeat original crashes without new relevant evidence.
