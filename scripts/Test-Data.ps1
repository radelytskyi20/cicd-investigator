$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')
# The dedicated smoke database contains only synthetic test data and is retained for inspection.
$secrets = Get-Content "$env:APPDATA/Microsoft/UserSecrets/cicd-investigator-apphost/secrets.json" -Raw | ConvertFrom-Json
$container = @(& docker ps -q --filter volume=cicd-investigator-data)
if ($container.Count -ne 1) { throw 'Start the local Aspire stack first; expected one project SQL container.' }
$port = (& docker port $container[0] 1433/tcp).Split(':')[-1]
$env:INVESTIGATOR_TEST_SQL = "Server=127.0.0.1,$port;Database=investigator-smoke-t0201;User ID=sa;Password=$($secrets.'Parameters:sql-password');TrustServerCertificate=true"
try {
    & dotnet test tests/CiCdInvestigator.Data.Tests --verbosity minimal
    if ($LASTEXITCODE -ne 0) { throw 'SQL dataset smoke failed.' }
}
finally { Remove-Item Env:INVESTIGATOR_TEST_SQL }
