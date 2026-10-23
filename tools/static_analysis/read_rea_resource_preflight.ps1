$ErrorActionPreference = 'Stop'
$started = [DateTime]::UtcNow
$external = @(Get-CimInstance Win32_Process | Where-Object {
    $_.Name -match '^(javaw?|UnrealEditor(-Cmd)?|UE4Editor(-Cmd)?|UnrealBuildTool|AutomationTool|ShaderCompileWorker|cl|link|csc|dotnet|MSBuild|UbaHost|UbaAgent|UbaSessionServer)\.exe$'
} | Select-Object ProcessId,ParentProcessId,Name)
$memory = Get-CimInstance Win32_PerfFormattedData_PerfOS_Memory
[pscustomobject]@{
    utc=$started.ToString('o')
    available_physical_bytes=[long]$memory.AvailableBytes
    system_commit_bytes=[long]$memory.CommittedBytes
    system_commit_limit_bytes=[long]$memory.CommitLimit
    external_build_or_java_processes=$external
} | ConvertTo-Json -Depth 4 -Compress
