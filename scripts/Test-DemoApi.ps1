param([Guid]$ResultId = [Guid]::Empty)
$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')
$description = & aspire describe --apphost src/CiCdInvestigator.AppHost/CiCdInvestigator.AppHost.csproj --format Json | ConvertFrom-Json
$api = @($description.resources | Where-Object { $_.name -like 'api-*' })
if ($api.Count -ne 1 -or $api[0].healthStatus -ne 'Healthy') { throw 'Expected one healthy API.' }
$url = ($api[0].urls | Where-Object name -eq 'http').url
$runs = (Invoke-RestMethod "$url/api/runs").data
if ($runs.Count -lt 2 -or 'run-102' -notin $runs.id -or 'run-101' -notin $runs.id) { throw 'Expected the S01 runs in the catalog.' }
$run = (Invoke-RestMethod "$url/api/runs/run-102").data
if ($run.jobs[0].id -ne 'job-102-test-1') { throw 'Run/job identity mismatch.' }
$baseline = (Invoke-RestMethod "$url/api/runs/run-102/baseline").data
if ($baseline.candidate.id -ne 'run-101') { throw 'Expected the successful baseline.' }
$log = (Invoke-RestMethod "$url/api/runs/run-102/jobs/job-102-test-1/logs?attempt=1&start=3&count=1&datasetVersion=mock-v1").data
if ($log.lines.Count -ne 1 -or $log.lines[0].position -ne 3 -or $log.lines[0].text -notlike 'FAIL*') { throw 'Expected exact failure log line 3.' }
$tests = (Invoke-RestMethod "$url/api/runs/run-102/jobs/job-102-test-1/tests?attempt=1").data
if ($tests.tests[0].outcome -ne 'Failed') { throw 'Expected failed test evidence.' }
$changes = (Invoke-RestMethod "$url/api/runs/run-102/changes").data
if ($changes.patch -notlike '*AwayFromZero*') { throw 'Expected the stored rounding change.' }
$unavailable = Invoke-WebRequest "$url/api/runs/run-102?datasetVersion=unavailable-version" -SkipHttpErrorCheck
if ([int]$unavailable.StatusCode -ne 410) { throw 'Unavailable source version must not return replacement data.' }
$invalid = Invoke-WebRequest "$url/api/reports" -Method Post -ContentType 'application/json' -Body '{"resultId":"00000000-0000-0000-0000-000000000000"}' -SkipHttpErrorCheck
if ([int]$invalid.StatusCode -ne 409) { throw 'Invalid ready-result ID must be rejected.' }
Write-Output 'PASS: SQL-backed catalog, run/jobs, baseline, exact log line, tests and changes; unavailable version and invalid Save rejected.'
if ($ResultId -ne [Guid]::Empty) {
    # Supply the current validated result ID while its browser session is open and idle.
    $body = @{ resultId = $ResultId.ToString() } | ConvertTo-Json
    $first = Invoke-RestMethod "$url/api/reports" -Method Post -ContentType 'application/json' -Body $body
    $second = Invoke-RestMethod "$url/api/reports" -Method Post -ContentType 'application/json' -Body $body
    $history = Invoke-RestMethod "$url/api/reports"
    $opened = Invoke-RestMethod "$url/api/reports/$ResultId"
    if ($first.id -ne $second.id -or @($history | Where-Object id -eq $ResultId.ToString()).Count -ne 1 -or $opened.result.resultId -ne $ResultId.ToString()) { throw 'Save idempotency or reopening failed.' }
    Write-Output 'PASS: repeated explicit Save produces one report; saved content reopens with the same ready-result ID.'
}
