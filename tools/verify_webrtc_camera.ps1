param(
    [string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
)

$ErrorActionPreference = 'Stop'

$checks = @(
    @{
        File = 'Form1.Camera.cs'
        Pattern = 'EnableMqttCameraFrameFallback'
        Message = 'MQTT JPEG camera frame fallback must be explicit and disabled by default.'
    },
    @{
        File = 'Form1.Camera.cs'
        Pattern = 'webRtcFrameInFlight'
        Message = 'WebRTC frame submission must be throttled so frames cannot pile up.'
    },
    @{
        File = 'index.html'
        Pattern = 'pendingRemoteCandidates'
        Message = 'Browser must buffer ICE candidates that arrive before SDP answer is applied.'
    },
    @{
        File = 'index.html'
        Pattern = 'requestStartCameraStream'
        Message = 'Web UI must explicitly request camera START before opening WebRTC.'
    },
    @{
        File = 'WebRtcCameraServer.cs'
        Pattern = 'MaxWebRtcStreamWidth'
        Message = 'WebRTC stream dimensions must be centralized for predictable browser streaming.'
    },
    @{
        File = 'index.html'
        Pattern = "signalingState !== 'have-local-offer'"
        Message = 'Browser must ignore duplicate or stale SDP answers after signaling reaches stable.'
    },
    @{
        File = 'index.html'
        Pattern = 'appliedRemoteCandidateKeys'
        Message = 'Browser must de-duplicate repeated ICE candidates from MQTT signaling.'
    },
    @{
        File = 'index.html'
        Pattern = 'MQTT_TOPIC_WEBRTC_STATUS'
        Message = 'Browser must subscribe to WebRTC status diagnostics separately from video.'
    },
    @{
        File = 'index.html'
        Pattern = 'handleWebRtcStatus'
        Message = 'Browser must show what WebRTC is waiting for when frames are missing.'
    },
    @{
        File = 'index.html'
        Pattern = 'onloadeddata'
        Message = 'Browser must mark video active only after the first real video frame arrives.'
    },
    @{
        File = 'WebRtcCameraServer.cs'
        Pattern = 'DACDT/camera/webrtc/status'
        Message = 'Control app must publish WebRTC encoder/frame diagnostics.'
    },
    @{
        File = 'DACDT_2026.csproj'
        Pattern = '<PlatformTarget>x86</PlatformTarget>'
        Message = 'Default build must use x86 so it matches ActUtlType PLC architecture.'
    }
)

$failed = $false
foreach ($check in $checks) {
    $path = Join-Path $ProjectRoot $check.File
    if (-not (Test-Path -LiteralPath $path)) {
        Write-Host "FAIL $($check.File): file not found"
        $failed = $true
        continue
    }

    $content = Get-Content -LiteralPath $path -Raw
    if ($content -notmatch [regex]::Escape($check.Pattern)) {
        Write-Host "FAIL $($check.File): $($check.Message)"
        $failed = $true
    } else {
        Write-Host "PASS $($check.File): $($check.Pattern)"
    }
}

if ($failed) {
    exit 1
}

$indexPath = Join-Path $ProjectRoot 'index.html'
$indexContent = Get-Content -LiteralPath $indexPath -Raw
if ($indexContent -match 'return;\s*const candidate = new RTCIceCandidate') {
    Write-Host 'FAIL index.html: unreachable legacy ICE candidate block must be removed.'
    exit 1
}

Write-Host 'PASS index.html: no unreachable legacy ICE candidate block'
