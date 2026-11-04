$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')
$hostDll = Join-Path (Get-Location) 'src/CiCdInvestigator.McpServer/bin/Debug/net10.0/CiCdInvestigator.McpServer.dll'
if (-not (Test-Path $hostDll)) { throw 'Build the solution first.' }
$container = @(& docker ps -q --filter volume=cicd-investigator-data)
if ($container.Count -ne 1) { throw 'Start the Aspire SQL container first.' }
$port = (& docker port $container[0] 1433/tcp).Split(':')[-1]
$secrets = Get-Content "$env:APPDATA/Microsoft/UserSecrets/cicd-investigator-apphost/secrets.json" -Raw | ConvertFrom-Json
$connection = "Server=127.0.0.1,$port;Database=investigator-mcp-test;User ID=sa;Password=$($secrets.'Parameters:sql-password');TrustServerCertificate=true"
$listener = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, 0)
$listener.Start()
$httpPort = $listener.LocalEndpoint.Port
$listener.Stop()
$url = "http://127.0.0.1:$httpPort"
New-Item -ItemType Directory -Force .scratch/mcp-integration | Out-Null
$previous = $env:ConnectionStrings__investigator
$process = $null
try {
    $env:ConnectionStrings__investigator = $connection
    $process = Start-Process dotnet -ArgumentList @($hostDll, '--urls', $url) -WindowStyle Hidden -PassThru -RedirectStandardOutput .scratch/mcp-integration/host.log -RedirectStandardError .scratch/mcp-integration/error.log
    $deadline = (Get-Date).AddSeconds(45)
    $ready = $false
    do {
        if ($process.HasExited) { throw 'Isolated MCP host exited; inspect ignored .scratch/mcp-integration logs.' }
        try { $ready = (Invoke-WebRequest "$url/health" -TimeoutSec 2).StatusCode -eq 200 } catch { Start-Sleep -Milliseconds 250 }
    } until ($ready -or (Get-Date) -gt $deadline)
    if (-not $ready) { throw 'Isolated MCP host did not become ready.' }
    & dotnet run --project tests/CiCdInvestigator.Mcp.Smoke -- "$url/mcp"
    if ($LASTEXITCODE -ne 0) { throw 'Isolated six-tool smoke failed.' }
    $env:INVESTIGATOR_MCP_TEST_SQL = $connection
    $env:INVESTIGATOR_MCP_TEST_URL = "$url/mcp"
    & dotnet test tests/CiCdInvestigator.Data.Tests --filter 'Category=Mcp' --verbosity minimal
    if ($LASTEXITCODE -ne 0) { throw 'MCP evidence test failed.' }
}
finally {
    if ($null -ne $process -and -not $process.HasExited) { Stop-Process -Id $process.Id; $process.WaitForExit() }
    $env:ConnectionStrings__investigator = $previous
    Remove-Item Env:INVESTIGATOR_MCP_TEST_SQL, Env:INVESTIGATOR_MCP_TEST_URL -ErrorAction SilentlyContinue
}
