param(
    [Parameter(Mandatory = $true)][string]$ClientRoot,
    [Parameter(Mandatory = $true)][string]$ServerRoot,
    [Parameter(Mandatory = $true)][string]$ClientDll,
    [Parameter(Mandatory = $true)][string]$ServerDll,
    [Parameter(Mandatory = $true)][string]$DedicatedServerExe,
    [string]$Python = "python"
)

$ErrorActionPreference = "Stop"
$manifest = Join-Path $ClientRoot "mirror_hash_manifest.py"
& $Python $manifest --diff $ClientDll $ServerDll --filter Twelve
if ($LASTEXITCODE -ne 0) { throw "Mirror remote-call verification failed." }

$sharedFiles = @(
    "Assets/_Games/12 Beads/Scripts & UI/Scripts/TwelveBoardTopology.cs",
    "Assets/_Games/12 Beads/Scripts & UI/Scripts/TwelveState.cs",
    "Assets/_Games/12 Beads/Scripts & UI/Scripts/TwelveRulesEngine.cs",
    "Assets/_Games/12 Beads/Tests/Editor/TwelveRulesTests.cs",
    "Assets/_Games/12 Beads/Tests/TwelveDedicatedServerFuzzHarness.cs"
)
foreach ($relativePath in $sharedFiles) {
    $clientHash = (Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $ClientRoot $relativePath)).Hash
    $serverHash = (Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $ServerRoot $relativePath)).Hash
    if ($clientHash -ne $serverHash) { throw "Shared Twelve source differs: $relativePath" }
}
Write-Output "Shared Twelve rules/test sources match."

$dedicatedLog = Join-Path ([System.IO.Path]::GetTempPath()) "twelve-dedicated-fuzz.log"
$process = Start-Process -FilePath $DedicatedServerExe -ArgumentList @(
    "-batchmode", "-nographics", "--twelve-dedicated-fuzz", "-logFile", $dedicatedLog
) -PassThru -WindowStyle Hidden
if (-not $process.WaitForExit(60000)) {
    Stop-Process -Id $process.Id -Force
    throw "Dedicated-server fuzz test timed out."
}
$dedicatedOutput = Get-Content -LiteralPath $dedicatedLog -Raw
if ($process.ExitCode -ne 0 -or $dedicatedOutput -notmatch "TWELVE_DEDICATED_FUZZ: PASS") {
    throw "Dedicated-server fuzz test failed. See $dedicatedLog"
}
Write-Output "Dedicated-server Twelve fuzz passed."
