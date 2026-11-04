param([string]$NodePath, [string]$CopilotPath)
$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')
$expected = (Get-Content .node-version -Raw).Trim()
if ($NodePath) {
    $resolved = (Resolve-Path -LiteralPath $NodePath).Path
    $env:PATH = "$(Split-Path $resolved);$env:PATH"
}
$actual = (& node --version).TrimStart('v')
if ($actual -ne $expected) { throw "Node $expected is required; found $actual. Pass -NodePath with the selected node.exe path." }
if (-not $CopilotPath) { $CopilotPath = $env:Copilot__CliPath }
if (-not $CopilotPath) {
    $command = Get-Command copilot -CommandType Application -ErrorAction SilentlyContinue
    if ($command) { $CopilotPath = $command.Source }
}
if (-not $CopilotPath -and $env:LOCALAPPDATA) {
    $installed = Join-Path $env:LOCALAPPDATA 'GitHubCopilotCLI/copilot.exe'
    if (Test-Path -LiteralPath $installed -PathType Leaf) { $CopilotPath = $installed }
}
if (-not $CopilotPath -or -not (Test-Path -LiteralPath $CopilotPath -PathType Leaf)) {
    throw 'Copilot CLI was not found. Pass -CopilotPath with the installed executable path.'
}
$resolvedCopilot = (Resolve-Path -LiteralPath $CopilotPath).Path
$previousCopilot = $env:Copilot__CliPath
try {
    $env:Copilot__CliPath = $resolvedCopilot
    & aspire run
    if ($LASTEXITCODE -ne 0) { throw "Aspire exited with code $LASTEXITCODE." }
}
finally { $env:Copilot__CliPath = $previousCopilot }
