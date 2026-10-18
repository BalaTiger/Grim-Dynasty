$ErrorActionPreference='Stop'
$checks=@()
function Inspect([string]$name,[scriptblock]$code) {
 try {$script:checks+=@{name=$name;status='ok';value=(& $code)}}
 catch {$script:checks+=@{name=$name;status='failed';error=$_.Exception.Message;error_id=$_.FullyQualifiedErrorId}}
}
Inspect 'AdministratorToken' {([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)}
Inspect 'OS_CIM' {Get-CimInstance Win32_OperatingSystem | Select-Object Caption,Version,BuildNumber,OSArchitecture,TotalVisibleMemorySize,FreePhysicalMemory}
Inspect 'OS_Registry' {Get-ItemProperty -LiteralPath 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion' | Select-Object ProductName,EditionID,DisplayVersion,CurrentBuild,UBR}
Inspect 'System' {Get-CimInstance Win32_ComputerSystem | Select-Object Manufacturer,Model,HypervisorPresent,TotalPhysicalMemory}
Inspect 'CPU' {Get-CimInstance Win32_Processor | Select-Object Name,NumberOfCores,NumberOfLogicalProcessors,VirtualizationFirmwareEnabled,VMMonitorModeExtensions,SecondLevelAddressTranslationExtensions}
Inspect 'FixedDiskFreeSpace' {Get-CimInstance Win32_LogicalDisk -Filter 'DriveType=3' | Select-Object DeviceID,Size,FreeSpace}
Inspect 'SandboxFeatureAndParents' {
 $f=Get-WindowsOptionalFeature -Online -FeatureName Containers-DisposableClientVM -LogPath "$PSScriptRoot\isolation-readiness-dism.log"
 $f | Select-Object FeatureName,@{n='StateName';e={$_.State.ToString()}},RestartRequired,CustomProperties
}
Inspect 'RelatedFeatureStates' {
 Get-WindowsOptionalFeature -Online -LogPath "$PSScriptRoot\isolation-readiness-dism.log" | Where-Object {$_.FeatureName -in @('Microsoft-Hyper-V-All','Microsoft-Hyper-V-Hypervisor','VirtualMachinePlatform','HypervisorPlatform','Containers','Containers-DisposableClientVM')} | Select-Object FeatureName,@{n='StateName';e={$_.State.ToString()}},RestartRequired
}
Inspect 'PendingComponentRebootMarker' {Test-Path -LiteralPath 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending'}
Inspect 'PendingWindowsUpdateRebootMarker' {Test-Path -LiteralPath 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired'}
Inspect 'AvailableIsolationLaunchers' {Get-Command WindowsSandbox.exe,vmconnect.exe,docker,VBoxManage,qemu-system-x86_64,vmrun -ErrorAction SilentlyContinue | Select-Object Name,Source}
$report=[ordered]@{time_utc=[DateTime]::UtcNow.ToString('o');operation='Read-only checks only. No enable/install/restart/Steam action; DISM log restricted to workspace';checks=$checks}
$json=$report|ConvertTo-Json -Depth 12
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'isolation-readiness.json'),$json)
$json
