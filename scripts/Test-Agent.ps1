param([switch]$FollowUpOnly)
$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')
$description = & aspire describe --apphost src/CiCdInvestigator.AppHost/CiCdInvestigator.AppHost.csproj --format Json | ConvertFrom-Json
$mcp = @($description.resources | Where-Object { $_.name -like 'mcp-*' })
if ($mcp.Count -ne 1 -or $mcp[0].healthStatus -ne 'Healthy') { throw 'Expected one healthy MCP host.' }
$endpoint = ($mcp[0].urls | Where-Object name -eq 'http').url + '/mcp'
$cli = (Get-Command copilot -CommandType Application).Source
$smokeArguments = @($endpoint, $cli)
if ($FollowUpOnly) { $smokeArguments += '--follow-up-only' }
& dotnet run --no-build -c Release --project tests/CiCdInvestigator.Agent.Smoke -- @smokeArguments
if ($LASTEXITCODE -ne 0) { throw 'Agent SignalR smoke failed.' }
