$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')
$hostDll = Join-Path (Get-Location) 'src/CiCdInvestigator.McpServer/bin/Release/net10.0/CiCdInvestigator.McpServer.dll'
if (-not (Test-Path $hostDll)) { throw 'Build the solution first.' }
$container = @(& docker ps -q --filter volume=cicd-investigator-data)
if ($container.Count -ne 1) { throw 'Start the Aspire SQL container first.' }
$port = (& docker port $container[0] 1433/tcp).Split(':')[-1]
$secrets = Get-Content "$env:APPDATA/Microsoft/UserSecrets/cicd-investigator-apphost/secrets.json" -Raw | ConvertFrom-Json
$connection = "Server=127.0.0.1,$port;Database=investigator-report-test;User ID=sa;Password=$($secrets.'Parameters:sql-password');TrustServerCertificate=true"
$listener = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, 0)
$listener.Start()
$httpPort = $listener.LocalEndpoint.Port
$listener.Stop()
$url = "http://127.0.0.1:$httpPort"
New-Item -ItemType Directory -Force .scratch/report-integration | Out-Null
$previous = $env:ConnectionStrings__investigator
$process = $null
try {
    $env:ConnectionStrings__investigator = $connection
    $process = Start-Process dotnet -ArgumentList @($hostDll, '--urls', $url) -WindowStyle Hidden -PassThru -RedirectStandardOutput .scratch/report-integration/host.log -RedirectStandardError .scratch/report-integration/error.log
    $deadline = (Get-Date).AddSeconds(45)
    $ready = $false
    do {
        if ($process.HasExited) { throw 'Isolated MCP host exited; inspect ignored .scratch/report-integration logs.' }
        try { $ready = (Invoke-WebRequest "$url/health" -TimeoutSec 2).StatusCode -eq 200 } catch { Start-Sleep -Milliseconds 250 }
    } until ($ready -or (Get-Date) -gt $deadline)
    if (-not $ready) { throw 'Isolated MCP host did not become ready.' }
    $env:INVESTIGATOR_REPORT_TEST_SQL = $connection
    $cli = (Get-Command copilot -CommandType Application).Source
    & dotnet run --no-build -c Release --project tests/CiCdInvestigator.Agent.Smoke -- "$url/mcp" $cli
    if ($LASTEXITCODE -ne 0) { throw 'Real report integration smoke failed.' }
}
finally {
    if ($null -ne $process -and -not $process.HasExited) { Stop-Process -Id $process.Id; $process.WaitForExit() }
    $env:ConnectionStrings__investigator = $previous
    Remove-Item Env:INVESTIGATOR_REPORT_TEST_SQL -ErrorAction SilentlyContinue
}
