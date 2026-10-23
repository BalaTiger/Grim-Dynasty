# Preparation only: dot-sourcing defines one function and performs no file I/O.
# The separate reviewed runner calls it explicitly. Reuses the compiled format
# parser; it neither implements a second parser nor constructs/publishes a mesh.
function Invoke-GrimDynastyMainmenuPathingProbe {
    [CmdletBinding()]
    param()
    $ErrorActionPreference = 'Stop'
    $taskRoot = 'C:\Users\zhuzi\Documents\Grim-Dynasty'
    $output = Join-Path $taskRoot 'docs\reverse-client\r1-mainmenu-pathing-data-probe'
    $chunkOffset = 422373
    $chunkLength = 442996
    $chunkHash = '0a0a38176e69847c02d65e90805da18c8b849fe1806e16bfc03bcebabc07ccee'
    $pins = @(
        @{ key='library'; relative='.tooling\r1-pathing-data-validation\GrimDynasty.PathingData.dll'; bytes=6656; sha256='4f105f145ccdfb4b17b33b1167f8b700f06f38a4a07f8dc36cdf50f3d12aac4f' },
        @{ key='member'; relative='docs\reverse-client\r1-mainmenu-complete-member\mainmenu.map.bin'; bytes=1091357; sha256='a0e4559c417802c284b19038bfa9e23f3419996457dd78b4d2669d292313b289' },
        @{ key='observation'; relative='docs\reverse-client\r1-mainmenu-complete-member\level-chunk-observation.json'; bytes=5079; sha256='558a9ac7959da4e64394c4195623e4576b6b081c8978f0869d18744d4d52e6d3' }
    )
    function Require-Probe([bool]$condition,[string]$message) { if (-not $condition) { throw $message } }
    function Get-BytesDigest([byte[]]$bytes,[int]$offset,[int]$length) {
        $sha = [Security.Cryptography.SHA256]::Create()
        try { return ([BitConverter]::ToString($sha.ComputeHash($bytes,$offset,$length))).Replace('-','').ToLowerInvariant() }
        finally { $sha.Dispose() }
    }
    function Read-PinnedProbeInput($pin) {
        $filename = Join-Path $taskRoot $pin.relative
        $info = Get-Item -LiteralPath $filename
        Require-Probe (-not $info.PSIsContainer -and (($info.Attributes -band [IO.FileAttributes]::ReparsePoint) -eq 0)) 'Pinned input must be an ordinary file'
        # FileShare.Read prevents a writer while this bounded snapshot is read.
        $stream = [IO.File]::Open($filename,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
        try {
            Require-Probe ($stream.Length -eq $pin.bytes) ('Pinned input size mismatch: '+$pin.relative)
            $bytes = New-Object byte[] ([int]$pin.bytes)
            $at = 0
            while ($at -lt $bytes.Length) {
                $n = $stream.Read($bytes,$at,$bytes.Length-$at)
                Require-Probe ($n -gt 0) 'Unexpected EOF in pinned workspace input'
                $at += $n
            }
            Require-Probe ((Get-BytesDigest $bytes 0 $bytes.Length) -eq $pin.sha256) ('Pinned input SHA256 mismatch: '+$pin.relative)
            return ,$bytes
        } finally { $stream.Dispose() }
    }
    function Write-NewProbeJson($report) {
        $text = ($report | ConvertTo-Json -Depth 12) + [Environment]::NewLine
        $utf8 = New-Object Text.UTF8Encoding($false)
        $bytes = $utf8.GetBytes($text)
        Require-Probe ($bytes.Length -le 16MB) 'Own report budget exceeded; no truncated success report'
        $stream = [IO.File]::Open((Join-Path $output 'inspection.json'),[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
        try { $stream.Write($bytes,0,$bytes.Length) } finally { $stream.Dispose() }
    }
    function Observe-LayerHeader([byte[]]$buffer,[int]$offset,[int]$length) {
        $prefixLength = [Math]::Min(32,$length)
        $view = [ordered]@{
            observation_only=$true
            interpretation='Raw byte/LE-word observations only; no RLTD version, coordinate or layer-build acceptance is claimed'
            prefix_bytes=$prefixLength
            prefix_hex=([BitConverter]::ToString($buffer,$offset,$prefixLength)).Replace('-','').ToLowerInvariant()
            ascii_00_03=$null
            magic_u32_at00=$null
            version_word_u32_at04=$null
            coordinate_word_i32_at08=$null
            coordinate_word_i32_at0c=$null
            word_i32_at10=$null
        }
        if ($length -ge 4) {
            $view.ascii_00_03=[Text.Encoding]::ASCII.GetString($buffer,$offset,4)
            $view.magic_u32_at00=[BitConverter]::ToUInt32($buffer,$offset)
        }
        if ($length -ge 8) { $view.version_word_u32_at04=[BitConverter]::ToUInt32($buffer,$offset+4) }
        if ($length -ge 12) { $view.coordinate_word_i32_at08=[BitConverter]::ToInt32($buffer,$offset+8) }
        if ($length -ge 16) { $view.coordinate_word_i32_at0c=[BitConverter]::ToInt32($buffer,$offset+12) }
        if ($length -ge 20) { $view.word_i32_at10=[BitConverter]::ToInt32($buffer,$offset+16) }
        return $view
    }

    Require-Probe ((Get-Location).Path -eq $taskRoot) 'Fixed project working directory required'
    Require-Probe ([BitConverter]::IsLittleEndian) 'This fixed Windows byte-observation probe requires little endian'
    Require-Probe (-not (Test-Path -LiteralPath $output)) 'Existing output preserved; this probe never overwrites or retries into it'
    New-Item -ItemType Directory -Path $output -ErrorAction Stop | Out-Null
    $report = [ordered]@{
        schema='grim-dynasty-real-mainmenu-pathing-framing-v1'
        started_utc=[DateTime]::UtcNow.ToString('o')
        status='running'
        phase='input-pins'
        pinned_inputs=$pins
        parser='GrimDynasty.ReverseClient.Actions.NavigationPathingData.Parse(byte[],int,int)'
        chunk=[ordered]@{ member_offset=$chunkOffset; bytes=$chunkLength; expected_sha256=$chunkHash; actual_sha256=$null }
        boundaries=[ordered]@{
            original_archive_or_executable_read=$false; original_game_or_backend_started=$false
            compiled_library_reused=$true; compile_performed=$false; old_tests_rerun=$false
            parser_reimplemented=$false; whole_member_copied_to_output=$false; payloads_copied_to_output=$false
            layer_decoded_or_added=$false; mesh_built_or_published=$false; gameplay_accepted=$false
            native_1099b0_completion_or_callback_behavior_verified=$false
            format_bounds_are_own_safety_adapter=$true; rltd_header_bytes_are_observations_only=$true
        }
        grid=$null; group_count=$null; layer_count=$null; groups=@()
        borrowed_original_member_identity=$false; member_memory_unchanged=$false
        input_files_unchanged=$false; input_sha256_after=@(); failure=$null
    }
    try {
        $inputs = @{}
        foreach ($pin in $pins) { $inputs[$pin.key] = Read-PinnedProbeInput $pin }
        [byte[]]$member = $inputs.member
        $observation = [Text.Encoding]::UTF8.GetString($inputs.observation) | ConvertFrom-Json
        Require-Probe ($observation.source.sha256Before -eq $pins[1].sha256 -and $observation.source.sha256After -eq $pins[1].sha256) 'Pinned observation source differs'
        $declared = @($observation.chunks | Where-Object { $_.id -eq 28 })
        $observed = @($observation.pathingChunkObservations | Where-Object { $_.id -eq 28 })
        Require-Probe ($declared.Count -eq 1 -and $observed.Count -eq 1) 'Pinned evidence does not identify one fixed chunk 1C'
        Require-Probe ($declared[0].payloadMemberOffset -eq $chunkOffset -and $declared[0].payloadBytes -eq $chunkLength) 'Declared chunk extent differs'
        Require-Probe ($observed[0].payloadMemberOffset -eq $chunkOffset -and $observed[0].bytes -eq $chunkLength -and $observed[0].sha256 -eq $chunkHash) 'Observed chunk fingerprint differs'
        $report.chunk.actual_sha256 = Get-BytesDigest $member $chunkOffset $chunkLength
        Require-Probe ($report.chunk.actual_sha256 -eq $chunkHash) 'Actual member subrange SHA256 differs'

        $report.phase='reuse-compiled-parser'
        # Load the verified bytes, so no path-race or second DLL selection can
        # substitute code. Reflection binds the method from this assembly itself.
        $assembly = [Reflection.Assembly]::Load([byte[]]$inputs.library)
        $type = $assembly.GetType('GrimDynasty.ReverseClient.Actions.NavigationPathingData',$true)
        $method = $type.GetMethod('Parse',[Type[]]@([byte[]],[int],[int]))
        Require-Probe ($null -ne $method) 'Pinned DLL has no expected Parse overload'
        $arguments = New-Object object[] 3
        $arguments[0]=$member; $arguments[1]=[int]$chunkOffset; $arguments[2]=[int]$chunkLength
        $data = $method.Invoke($null,$arguments)
        Require-Probe ([Object]::ReferenceEquals($data.Source.Buffer,$member)) 'Parser source is not the original member byte array'
        Require-Probe ($data.Source.Offset -eq $chunkOffset -and $data.Source.Length -eq $chunkLength) 'Parser source extent differs'
        $report.grid=[ordered]@{ minimum_x=$data.MinimumGridX; minimum_z=$data.MinimumGridZ; maximum_x=$data.MaximumGridX; maximum_z=$data.MaximumGridZ }
        $groups = New-Object 'System.Collections.Generic.List[object]'
        $layerCount=0
        for ($g=0; $g -lt $data.Groups.Count; $g++) {
            $group=$data.Groups[$g]
            Require-Probe ([Object]::ReferenceEquals($group.EncodedLayers.Buffer,$member)) 'Group lost borrowed member identity'
            $layers=New-Object 'System.Collections.Generic.List[object]'
            for ($l=0; $l -lt $group.Layers.Count; $l++) {
                $layer=$group.Layers[$l]; $payload=$layer.Payload
                Require-Probe ([Object]::ReferenceEquals($payload.Buffer,$member)) 'Layer lost borrowed member identity'
                $layers.Add([ordered]@{
                    ordinal_in_group=$l; ordinal_total=$layerCount
                    length_prefix_member_offset=$layer.LengthPrefixOffset; declared_bytes=$layer.DeclaredLength
                    payload_member_offset=$payload.Offset; payload_chunk_offset=$payload.Offset-$chunkOffset
                    payload_bytes=$payload.Length; payload_sha256=(Get-BytesDigest $member $payload.Offset $payload.Length)
                    borrowed_original_member_identity=$true
                    header_observation=(Observe-LayerHeader $member $payload.Offset $payload.Length)
                })
                $layerCount++
            }
            $groups.Add([ordered]@{
                ordinal=$g; length_prefix_member_offset=$group.LengthPrefixOffset; declared_bytes=$group.DeclaredLength
                encoded_layers_member_offset=$group.EncodedLayers.Offset; encoded_layers_bytes=$group.EncodedLayers.Length
                borrowed_original_member_identity=$true; layer_count=$group.Layers.Count; layers=$layers.ToArray()
            })
        }
        $report.group_count=$data.Groups.Count; $report.layer_count=$layerCount; $report.groups=$groups.ToArray()
        $report.borrowed_original_member_identity=$true
        $report.member_memory_unchanged=((Get-BytesDigest $member 0 $member.Length) -eq $pins[1].sha256)
        Require-Probe $report.member_memory_unchanged 'Parser changed the borrowed member bytes'
        $report.phase='input-fingerprints-after'
        foreach ($pin in $pins) {
            $after=(Get-FileHash -LiteralPath (Join-Path $taskRoot $pin.relative) -Algorithm SHA256).Hash.ToLowerInvariant()
            $report.input_sha256_after += [ordered]@{ relative=$pin.relative; sha256=$after }
            Require-Probe ($after -eq $pin.sha256) ('Input changed during probe: '+$pin.relative)
        }
        $report.input_files_unchanged=$true
        $report.status='passed-real-sample-pathing-framing-only'; $report.phase='complete'
    } catch {
        $report.status='failed'
        $errorObject=$_.Exception.GetBaseException()
        $report.failure=[ordered]@{ type=$errorObject.GetType().FullName; message=$errorObject.Message }
        $codeField=$errorObject.GetType().GetField('Code')
        $offsetField=$errorObject.GetType().GetField('SourceOffset')
        if ($null -ne $codeField) { $report.failure.format_code=$codeField.GetValue($errorObject) }
        if ($null -ne $offsetField) { $report.failure.format_source_offset=$offsetField.GetValue($errorObject) }
    }
    $report.finished_utc=[DateTime]::UtcNow.ToString('o')
    Write-NewProbeJson $report
    if ($report.status -ne 'passed-real-sample-pathing-framing-only') { throw ('Probe failed; preserved inspection.json: '+$report.failure.message) }
    Write-Output ('PASS real saved mainmenu PTHD framing: '+$report.group_count+' groups, '+$report.layer_count+' layers; output '+$output+'; no layer construction or mesh publication')
}
