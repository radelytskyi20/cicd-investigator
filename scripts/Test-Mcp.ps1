$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')
$description = & aspire describe --apphost src/CiCdInvestigator.AppHost/CiCdInvestigator.AppHost.csproj --format Json | ConvertFrom-Json
$mcp = @($description.resources | Where-Object { $_.name -like 'mcp-*' })
if ($mcp.Count -ne 1 -or $mcp[0].healthStatus -ne 'Healthy') { throw 'Expected one healthy MCP host. Start Aspire and wait for readiness.' }
$endpoint = ($mcp[0].urls | Where-Object name -eq 'http').url + '/mcp'
& dotnet run --project tests/CiCdInvestigator.Mcp.Smoke -- $endpoint
if ($LASTEXITCODE -ne 0) { throw 'MCP smoke failed.' }
