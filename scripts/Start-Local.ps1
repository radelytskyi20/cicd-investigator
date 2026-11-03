param([string]$NodePath)
$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')
$expected = (Get-Content .node-version -Raw).Trim()
if ($NodePath) {
    $resolved = (Resolve-Path -LiteralPath $NodePath).Path
    $env:PATH = "$(Split-Path $resolved);$env:PATH"
}
$actual = (& node --version).TrimStart('v')
if ($actual -ne $expected) { throw "Node $expected is required; found $actual. Pass -NodePath with the selected node.exe path." }
& aspire run
if ($LASTEXITCODE -ne 0) { throw "Aspire exited with code $LASTEXITCODE." }
