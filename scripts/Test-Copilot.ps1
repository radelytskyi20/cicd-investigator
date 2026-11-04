param([string]$Model = 'gpt-5.4', [switch]$Lifecycle)
$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')
$description = & aspire describe --apphost src/CiCdInvestigator.AppHost/CiCdInvestigator.AppHost.csproj --format Json | ConvertFrom-Json
$mcp = @($description.resources | Where-Object { $_.name -like 'mcp-*' })
if ($mcp.Count -ne 1 -or $mcp[0].healthStatus -ne 'Healthy') { throw 'Expected one healthy MCP host. Start Aspire and wait for readiness.' }
$endpoint = ($mcp[0].urls | Where-Object name -eq 'http').url + '/mcp'
$cli = (Get-Command copilot -CommandType Application).Source
$mode = if ($Lifecycle) { '--copilot-lifecycle-smoke' } else { '--copilot-smoke' }
& dotnet run --no-build -c Release --project src/CiCdInvestigator.Api -- $mode $endpoint $cli $Model
if ($LASTEXITCODE -ne 0) { throw 'Copilot smoke failed.' }
