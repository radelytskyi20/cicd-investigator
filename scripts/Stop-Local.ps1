$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')
& aspire stop --apphost src/CiCdInvestigator.AppHost/CiCdInvestigator.AppHost.csproj --non-interactive
if ($LASTEXITCODE -ne 0) { throw 'Aspire stop failed.' }
$deadline = (Get-Date).AddSeconds(45)
do {
    $containers = & docker ps -q --filter volume=cicd-investigator-data
    if ($LASTEXITCODE -ne 0) { throw 'Could not verify SQL container shutdown.' }
    if (-not $containers) { return }
    Start-Sleep -Milliseconds 500
} while ((Get-Date) -lt $deadline)
throw 'SQL container is still stopping. Do not restart until shutdown completes.'
